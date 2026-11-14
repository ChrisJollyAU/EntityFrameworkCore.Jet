using LibRed.Formats;
using Microsoft.Win32.SafeHandles;

namespace LibRed.IO;

/// <summary>
/// A bounded, write-through buffer pool of raw page bytes for one physical database file, <b>shared by every
/// <see cref="PageChannel"/> open on that file</b> (see <see cref="Acquire"/>). A single cache per file is what
/// keeps coexisting handles coherent: because they all read and write through the same pool, one connection's
/// committed writes are immediately visible to the others, while transaction-local overlay pages remain private,
/// exactly as the old straight-to-disk committed reads were, only now served from memory instead of a
/// <c>Seek()+Read()</c> per page.
///
/// <para>The pool owns its own copies: reads copy <i>out</i> to the caller and writes copy <i>in</i> from the
/// caller, so a caller mutating its buffer can never corrupt a cached page. Eviction is LRU, capped at
/// <see cref="CapacityPages"/>. All operations are guarded by a per-cache lock — same-file access from multiple
/// threads is serialised at the page level (concurrent unsynchronised writes to the same page were never
/// defined behaviour anyway).</para>
/// </summary>
internal sealed class PageCache : IDisposable
{
    /// <summary>Max resident pages per file. At a 4 KB page this is ~32 MB, comfortably covering the whole of a
    /// typical Access database (and the benchmark tables); larger databases simply evict LRU.</summary>
    private const int CapacityPages = 8192;

    private sealed class Entry(int page, byte[] bytes)
    {
        public readonly int Page = page;
        public byte[] Bytes = bytes;

        // An optional higher-layer parse of this page (e.g. an index page's decoded entries), cached alongside
        // the raw bytes so it is invalidated by the same write-through/eviction path — whenever the bytes change
        // the parse is stale, so Store() clears it. Opaque to the cache (boxed as object).
        public object? Parsed;
    }

    private readonly object _gate = new();
    // Shared for readers, exclusive for publication. Recursive so a writing statement can hold the exclusive
    // scope while each page write re-enters it; an attempted read→write upgrade is rejected explicitly below.
    private readonly ReaderWriterLockSlim _publishGate = new(LockRecursionPolicy.SupportsRecursion);
    private long _schemaGeneration;
    private long _imageGeneration;

    // A decode whose dependencies were checked at this generation needs no repeated page lookups until
    // the pool replaces an image. This is independent of schema: ordinary data writes change counts too.
    public long ImageGeneration => Volatile.Read(ref _imageGeneration);
    // The file's committed length in bytes, shared like the pages: every change to it is made by a channel on this
    // file, which records it here, so measuring the stream again only repeats this number - at the cost of a
    // file-information syscall per measurement, which on Windows made every pointer range check on a read path
    // expensive. Seeded by the first channel to open the file; -1 until then. Read and written without _gate:
    // every range check reads it, and taking the lock every cache hit already takes would add contention
    // between all the connections on the file for a single value.
    private long _fileLength = -1;
    private readonly Dictionary<int, LinkedListNode<Entry>> _map = [];
    private readonly LinkedList<Entry> _lru = new(); // first = most-recently-used

    // The images a refresh at a newly taken lock dropped, set aside for the rest of the writing scope: what this process
    // may have read of each page before another user's commit, which the page is compared with when it is locked (see
    // ImageBeforeOthersCommitted). `_evictions` counts pages evicted, and `_scopeEvictions` the count when the scope
    // began: a page evicted since may have been read and is in neither set, so the comparison cannot be trusted then.
    // All under _gate, but for the reset at a scope's start, made under the exclusive publish gate.
    private Dictionary<int, byte[]>? _setAside;
    private long _evictions;
    private long _scopeEvictions;

    // This process's registration as a Jet user of the file (null when it has none: an exclusive open, off Windows,
    // or a lock file that could not be opened). It lives here because a Jet user is per process per file, exactly
    // the scope and lifetime of this pool: registered by the first channel to open the file, deregistered when the
    // last one closes. The lock manager has the same scope but a test can inject its own and skip it.
    private readonly JetUserRegistration? _user;

    // How a registered user notices another user's commits (docs/format/locking.md §6): every user ends each batch of
    // page writes by changing its own commit slot on page 0, so a slot other than ours that differs from what this
    // process last saw means pages here may be stale. `_database` is a read handle of our own on the file — the
    // channels' streams buffer, and a positional read needs none of that — and `_seenCommitTable` the table as it
    // stood at the last check. Both null when the process is not registered: an exclusive holder is alone, and an
    // unregistered process is not a user anyone's slot speaks to.
    private readonly SafeFileHandle? _database;
    private readonly byte[]? _seenCommitTable;
    private readonly long _commitTableOffset;
    private readonly int _ownSlotOffset;
    private readonly int _slotSize;

    private PageCache(JetUserRegistration? user, string? path, JetFormatBase? format)
    {
        _user = user;
        if (user is null || path is null || format is null) return;

        _commitTableOffset = format.CommitByteTableOffset;
        _slotSize = format.CommitByteSlotSize;
        _ownSlotOffset = user.User * _slotSize;
        try
        {
            _database = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var table = new byte[format.CommitByteTableUsers * _slotSize];
            RandomAccess.Read(_database, table, _commitTableOffset);
            _seenCommitTable = table;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Without the read there is nothing to compare with; the process stays a user that does not look.
            _database?.Dispose();
            _database = null;
        }
    }

    /// <summary>This process's registration as a Jet user of the file — its user number, and the page write locks it
    /// holds for every channel on the file — or null when it is not registered.</summary>
    public JetUserRegistration? User => _user;

    /// <summary>Deregisters the Jet user and releases the publication lock. Called when the last channel on the
    /// file lets the cache go, so an open/close cycle leaves neither behind.</summary>
    public void Dispose()
    {
        _database?.Dispose();
        _user?.Dispose();
        _publishGate.Dispose();
    }

    // --- shared registry: one cache per canonical file path, refcounted by the channels using it ---

    private static readonly Dictionary<string, (PageCache Cache, int RefCount)> Registry = [with(StringComparer.Ordinal)];
    private static readonly Lock RegistryGate = new();

    /// <summary>Returns the shared cache for <paramref name="path"/>, creating it on first use; each call must be
    /// paired with a <see cref="Release"/>. The key is <see cref="FileIdentity"/>'s, so relative and absolute opens
    /// of one file share a pool (two pools for one file would reintroduce cross-handle staleness) while two files
    /// whose paths differ only in case, on a platform where that makes them two files, do not.</summary>
    public static PageCache Acquire(string path) => AcquireKey(FileIdentity.Key(path), registerAs: null);

    /// <summary><see cref="Acquire"/> by a key <see cref="FileIdentity"/> has already given — a channel works its
    /// file's key out once and uses it for every registry it joins and leaves. With <paramref name="registerAs"/>,
    /// the database path a shared open was given and the file's format, the call that creates the cache also
    /// registers this process as a Jet user of the file (<see cref="JetUserRegistration"/>) and starts watching the
    /// other users' commit slots; a later call joins that registration.</summary>
    /// <remarks>An exclusive open passes null and is never refused the registration it skips: it holds the file
    /// with <c>FileShare.None</c>, so no other channel can be on the file before it or join it after.</remarks>
    internal static PageCache AcquireKey(string key, (string Path, JetFormatBase Format)? registerAs)
    {
        lock (RegistryGate)
        {
            if (Registry.TryGetValue(key, out var slot))
            {
                Registry[key] = (slot.Cache, slot.RefCount + 1);
                return slot.Cache;
            }
            var cache = registerAs is { } open
                ? new PageCache(JetUserRegistration.Connect(open.Path), open.Path, open.Format)
                : new PageCache(null, null, null);
            Registry[key] = (cache, 1);
            return cache;
        }
    }

    /// <summary>Drops one reference to the cache for <paramref name="path"/>; when the last channel closes, the
    /// pool is discarded so a later re-open re-reads from disk (picking up any external change).</summary>
    public static void Release(string path) => ReleaseKey(FileIdentity.Key(path));

    /// <summary><see cref="Release"/> by a key <see cref="FileIdentity"/> has already given.</summary>
    internal static void ReleaseKey(string key)
    {
        lock (RegistryGate)
        {
            if (!Registry.TryGetValue(key, out var slot)) return;
            if (slot.RefCount <= 1)
            {
                Registry.Remove(key);
                slot.Cache.Dispose();
            }
            else Registry[key] = (slot.Cache, slot.RefCount - 1);
        }
    }

    /// <summary>Copies the cached bytes of <paramref name="page"/> into <paramref name="destination"/> and
    /// returns true on a hit; false (leaving the buffer untouched) on a miss.</summary>
    public bool TryRead(int page, Span<byte> destination)
    {
        lock (_gate)
        {
            if (!_map.TryGetValue(page, out var node)) return false;
            _lru.Remove(node);
            _lru.AddFirst(node);
            node.Value.Bytes.CopyTo(destination);
            return true;
        }
    }

    /// <summary>Returns the cache's own byte array for a resident <paramref name="page"/> without copying it out
    /// — for a read that consumes the bytes immediately (e.g. decoding one row) and never mutates or retains
    /// them. False on a miss. The array is live cache state: treat it read-only and use it before any further
    /// cache operation on the same thread could evict or overwrite it.</summary>
    public bool TryGetArray(int page, out byte[] array)
    {
        lock (_gate)
        {
            if (_map.TryGetValue(page, out var node))
            {
                _lru.Remove(node);
                _lru.AddFirst(node);
                array = node.Value.Bytes;
                return true;
            }
            array = [];
            return false;
        }
    }

    /// <summary>Stores <paramref name="source"/> as the cached image of <paramref name="page"/> (copying it),
    /// used both to fill a read miss and to write through a page write.</summary>
    public void Store(int page, ReadOnlySpan<byte> source) => Adopt(page, source.ToArray());

    /// <summary>Stores <paramref name="bytes"/> as the cached image of <paramref name="page"/> without copying
    /// it. The caller hands the array over: nothing may write into it afterwards, since zero-copy readers are
    /// given it as it stands (see <see cref="TryGetArray"/>). For an array already private to the write — a page
    /// write's own copy, or a transaction's overlay page as it publishes — where copying it again bought nothing.</summary>
    public void Adopt(int page, byte[] bytes)
    {
        lock (_gate)
        {
            if (_map.TryGetValue(page, out var node))
            {
                // Copy-on-write: replace the array rather than overwriting it in place, so a zero-copy reader
                // that already took the old array via TryGetArray keeps a stable (if now slightly stale) image
                // instead of seeing bytes change under it. Reads on a cache hit therefore need no page lock —
                // only the pre-existing _gate — which keeps the hot read path free of coordination overhead.
                node.Value.Bytes = bytes;
                node.Value.Parsed = null; // the bytes changed, so any cached parse of them is stale
                Interlocked.Increment(ref _imageGeneration);
                _lru.Remove(node);
                _lru.AddFirst(node);
                return;
            }

            var entry = new Entry(page, bytes);
            var added = _lru.AddFirst(entry);
            _map[page] = added;
            Interlocked.Increment(ref _imageGeneration);

            if (_map.Count > CapacityPages)
            {
                LinkedListNode<Entry> lruNode = _lru.Last!;
                _lru.RemoveLast();
                _map.Remove(lruNode.Value.Page);
                _evictions++;
            }
        }
    }

    /// <summary>Removes a page which was appended during a failed commit and subsequently truncated.</summary>
    public void Remove(int page)
    {
        lock (_gate)
        {
            if (!_map.Remove(page, out LinkedListNode<Entry>? node)) return;
            _lru.Remove(node);
            Interlocked.Increment(ref _imageGeneration);
        }
    }

    /// <summary>Serializes committed-page validation and publication for every channel on this file, and
    /// excludes readers for the duration. A transaction validates all pages it derived from and publishes its
    /// overlay while holding this gate; ordinary write-through operations use the same gate, preventing a write
    /// between validation and publish. Writing statements hold it for their whole duration, so a reader never
    /// sees one statement's pages half-written. The gate is recursive: a statement-scoped acquisition nests the
    /// per-page publications inside it.</summary>
    public void PublishLocked(Action action) => PublishLocked<object?>(() => { action(); return null; });

    /// <inheritdoc cref="PublishLocked(Action)"/>
    public T PublishLocked<T>(Func<T> action)
    {
        // A reader that turns out to write is a statement-classification bug (see QueryEngine): the shared
        // gate cannot be upgraded, so say so rather than surfacing a bare LockRecursionException.
        if (_publishGate.IsReadLockHeld)
            throw new InvalidOperationException(
                "A page write was attempted inside a shared (read-consistent) scope; the statement should have taken the exclusive scope.");

        _publishGate.EnterWriteLock();
        try
        {
            // The outermost scope looks at the other users' slots first, so a commit validates and a statement
            // reads against what they have written since (see RefreshIfOthersCommitted). What an earlier scope set
            // aside is nothing this one has read.
            if (_publishGate.RecursiveWriteCount == 1)
            {
                lock (_gate)
                {
                    _setAside = null;
                    _scopeEvictions = _evictions;
                }
                RefreshIfOthersCommitted();
            }
            return action();
        }
        finally { _publishGate.ExitWriteLock(); }
    }

    /// <summary>Runs one logical read while excluding a multi-page commit publication, so the caller sees
    /// either the complete pre-commit or complete post-commit page set. Shared: concurrent readers on this file
    /// run together, and only a publication (see <see cref="PublishLocked(Action)"/>) excludes them.</summary>
    /// <remarks>The outermost scope of a registered user first looks for another user's commit. Nothing found — the
    /// ordinary case — it carries on under the shared lock it looked under; found, it lets that go and drops the
    /// cache under the exclusive one, since readers may be holding images from it, then reads.</remarks>
    public T ReadConsistent<T>(Func<T> action)
    {
        if (_database is not null && !_publishGate.IsReadLockHeld && !_publishGate.IsWriteLockHeld)
        {
            _publishGate.EnterReadLock();
            bool changed;
            try { changed = OthersCommitted(stackalloc byte[_seenCommitTable!.Length]); }
            catch
            {
                _publishGate.ExitReadLock();
                throw;
            }

            if (!changed)
            {
                try { return action(); }
                finally { _publishGate.ExitReadLock(); }
            }

            _publishGate.ExitReadLock();
            RefreshIfOthersCommitted();
        }

        _publishGate.EnterReadLock();
        try { return action(); }
        finally { _publishGate.ExitReadLock(); }
    }

    /// <summary>Reads the commit table from the file into <paramref name="table"/> and says whether a slot other
    /// than this process's own differs from the table last seen. Only this process writes its own slot, and its
    /// channels publish through this cache, so a change there is nothing to refresh for. Callers hold the publish
    /// gate, shared or exclusive, so the table last seen cannot change under the comparison.</summary>
    private bool OthersCommitted(Span<byte> table)
    {
        RandomAccess.Read(_database!, table, _commitTableOffset);
        ReadOnlySpan<byte> seen = _seenCommitTable;
        int after = _ownSlotOffset + _slotSize;
        return !table[.._ownSlotOffset].SequenceEqual(seen[.._ownSlotOffset])
            || !table[after..].SequenceEqual(seen[after..]);
    }

    /// <summary>Under the exclusive publish gate — taken here, recursively, so a writing statement already holding it
    /// can look again part-way — when another user's slot has changed since the last look: drops every committed
    /// page image and its parse, measures the file again — another user may have grown it — and marks the schema
    /// changed so the catalog and the format are re-read. Coarse on purpose: the slot says that user wrote, not what.
    /// A transaction's own overlay is untouched; its commit then finds the other user's pages through the baseline
    /// check, as it finds another connection's in this process. A registered user only; does nothing otherwise.</summary>
    /// <remarks>Inside a shared scope it only looks: the shared gate cannot be upgraded, and the other readers hold
    /// images the drop would pull from under them. The caller then gives up the scope, and the next one's look drops
    /// the cache (<see cref="ReadConsistent{T}"/>).</remarks>
    /// <returns>Whether another user had committed — and, outside a shared scope, the cache was dropped.</returns>
    public bool RefreshIfOthersCommitted() => Refresh(setAside: false) is not Freshness.Unchanged;

    /// <summary>At a lock this process has just taken on <paramref name="page"/>, which no other user can now change
    /// until it is let go: refreshes as <see cref="RefreshIfOthersCommitted"/> does, but sets the dropped images aside
    /// for the rest of the writing scope rather than discarding them, and hands back in <paramref name="before"/> the
    /// image of the page this process held before any such drop in the scope — what the caller may have read of it,
    /// to compare with the page as the file now has it (null: it held none, so nothing it read of the page can be
    /// stale). That is ACE's discipline (locking.md §4): lock the page, read it again, then change it.</summary>
    /// <returns>False when the comparison cannot be trusted and the caller must take what it read as stale: inside a
    /// shared scope after another user committed (nothing can be dropped there), or when a page has been evicted since
    /// the scope began, so may have been read and is in neither set.</returns>
    public bool RefreshAtLock(int page, out byte[]? before)
    {
        before = null;
        Freshness freshness = Refresh(setAside: true);
        if (freshness is Freshness.Unknown) return false;
        lock (_gate)
        {
            if (_setAside is not null && _setAside.Remove(page, out byte[]? image)) before = image;
        }
        return true;
    }

    private enum Freshness { Unchanged, SetAside, Unknown }

    private Freshness Refresh(bool setAside)
    {
        if (_database is null) return Freshness.Unchanged;
        if (_publishGate.IsReadLockHeld && !_publishGate.IsWriteLockHeld)
            return OthersCommitted(stackalloc byte[_seenCommitTable!.Length]) ? Freshness.Unknown : Freshness.Unchanged;
        _publishGate.EnterWriteLock();
        try
        {
            Span<byte> table = stackalloc byte[_seenCommitTable!.Length];
            if (!OthersCommitted(table)) return Freshness.Unchanged;

            bool trusted;
            lock (_gate)
            {
                trusted = setAside && _evictions == _scopeEvictions;
                if (trusted)
                {
                    // The oldest image of a page wins: it is the one the scope may have read before the first drop.
                    _setAside ??= [];
                    foreach (Entry entry in _lru) _setAside.TryAdd(entry.Page, entry.Bytes);
                }
                else _setAside = null;
                _map.Clear();
                _lru.Clear();
            }
            SetFileLength(RandomAccess.GetLength(_database));
            MarkSchemaChanged();
            table.CopyTo(_seenCommitTable);
            return trusted ? Freshness.SetAside : Freshness.Unknown;
        }
        finally { _publishGate.ExitWriteLock(); }
    }

    // Format checks read this on hot row/page paths. The generation is one independently published value;
    // an atomic acquire read is sufficient, without taking the buffer-pool lock for every format access.
    public long SchemaGeneration => Volatile.Read(ref _schemaGeneration);

    public void MarkSchemaChanged()
    {
        Interlocked.Increment(ref _schemaGeneration);
        // Publish after the schema generation: seeing this image generation also requires rechecking
        // schema state, even when the invalidation itself did not replace a resident page.
        Interlocked.Increment(ref _imageGeneration);
    }

    /// <summary>The file's committed length in bytes (see <see cref="SetFileLength"/>).</summary>
    public long FileLength => Interlocked.Read(ref _fileLength);

    /// <summary>Seeds the committed length from the first channel to open the file. A later open keeps the value
    /// already held, which the writes of the channels before it have kept current.</summary>
    public void InitFileLength(long measured) => Interlocked.CompareExchange(ref _fileLength, measured, -1);

    /// <summary>Records a change a channel has just made to the file's length.</summary>
    public void SetFileLength(long length) => Interlocked.Exchange(ref _fileLength, length);

    /// <summary>Returns a previously cached higher-layer parse of <paramref name="page"/> (see
    /// <see cref="SetParsed"/>), or false if the page is not resident or has no parse cached.</summary>
    public bool TryGetParsed(int page, out object? parsed)
    {
        lock (_gate)
        {
            if (_map.TryGetValue(page, out var node) && node.Value.Parsed is { } p)
            {
                _lru.Remove(node);
                _lru.AddFirst(node);
                parsed = p;
                return true;
            }
            parsed = null;
            return false;
        }
    }

    /// <summary>Attaches a higher-layer parse to a resident page so later reads can skip re-parsing; a no-op if
    /// the page is not resident (it will simply be re-parsed next time). Cleared automatically when the page's
    /// bytes change (see <see cref="Store"/>) or it is evicted.</summary>
    public void SetParsed(int page, object parsed)
    {
        lock (_gate)
            if (_map.TryGetValue(page, out var node))
                node.Value.Parsed = parsed;
    }
}
