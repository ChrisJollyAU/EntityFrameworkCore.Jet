using System.Buffers.Binary;
using LibRed.Crypto;
using LibRed.Formats;
using LibRed.Pages;
using LibRed.Storage;

namespace LibRed.IO;

/// <summary>
/// The single primitive every higher layer reads through: page-aligned, optionally
/// decrypted access to the database file. Owns the <see cref="FileStream"/> and the
/// resolved <see cref="JetFormatBase"/>.
/// </summary>
public sealed class PageChannel : IDisposable
{
    private readonly FileStream _stream;
    private readonly bool _readOnly;

    // Shared, write-through buffer pool for this file (one pool per physical file, shared across every channel
    // open on it — see PageCache). Turns a page read from a Seek()+Read() syscall into a memory copy, and keeps
    // coexisting handles coherent because they all read/write through the same pool. Never null after Open.
    private readonly string _path;
    private readonly PageCache _cache;

    // The file's identity (FileIdentity.Key), worked out once: it names this file in both shared registries, on
    // the way in and on the way out, and each working-out is file-system calls on every open and close.
    private readonly string _identity;

    // Page decryptor for a password-encrypted database (null when the file is unencrypted). Applied to
    // every page as it comes off disk; page 0 (the readable header) is a no-op inside the codec.
    private readonly IPageCodec? _codec;

    // Cross-handle page coordination, shared per file path. Acquired here (refcounted, like the cache) unless a
    // manager is injected for a test; _ownsLocks records which, so Dispose releases only the one we acquired.
    private readonly ILockManager? _locks;
    private readonly bool _ownsLocks;

    // The open transaction's savepoint bookkeeping (null when none is open). This channel is the I/O executor;
    // the Transaction only tracks what a savepoint rollback must restore.
    private Transaction? _active;

    // Deferred-write overlay for the open transaction: page -> uncommitted plaintext bytes. Transactional writes
    // land here instead of on disk / in the shared cache, so a concurrent channel on the same file never sees
    // this channel's uncommitted pages (read-committed isolation). Commit replays it; rollback discards it.
    // Per-channel and only touched while a transaction is open, so a channel's own single thread owns it — no
    // lock. `_txPageCount` is the logical page count during a transaction (committed pages plus any the overlay
    // allocated), since deferred allocations do not grow the file until commit.
    private readonly Dictionary<int, byte[]> _overlay = [];
    // Higher-layer parses of overlay pages (see SetParsedPage), private to the transaction as the bytes are. The
    // shared cache's parse describes the committed image, so it cannot serve an overlay page; without this every
    // read of a page the transaction had written decoded it again — an index leaf, ~600 entries, several times
    // per index for every row inserted. A page's parse goes whenever its overlay bytes change.
    private readonly Dictionary<int, object> _overlayParsed = [];
    // Committed plaintext image from which each transactional page was first derived. At commit, every image
    // must still match; otherwise another channel committed the same page and publishing this stale overlay
    // would silently lose that writer's change. For a registered user it is also the set of pages this channel
    // holds a write lock on (LockForWrite): every page with a baseline, which is every page that was in the file
    // when first written — an appended page has none, and ACE locks none (locking.md §6).
    private readonly Dictionary<int, byte[]?> _commitBaselines = [];
    private bool _schemaDirty;
    private int _txPageCount;

    // Pages freed by this handle that are held until it closes, as ACE holds them (page-05 §9.1): `_releasing` is
    // what the open transaction has staged, `_released` what has committed. A commit moves the one into the other,
    // a rollback discards the staged pages, and a savepoint rollback truncates them to the savepoint's count.
    private readonly List<int> _releasing = [];
    private readonly List<int> _released = [];

    // The TDEF counters a rollback, of the transaction or to a savepoint, does not take back — by page and offset, the
    // last value written — which it writes back over the restored page. ACE never reissues an id it has drawn, whether
    // the insert that drew it commits, fails or is rolled back, and keeps MSysObjects' and MSysACEs' row and index
    // counts through any rollback; a statement that fails keeps every table's counts as it left them, where a
    // ROLLBACK takes an ordinary table's back (FailedOnly) (measured). Baseline is the committed value the
    // transaction started from: the transaction's rollback writes back its change on top of what is committed then,
    // which another handle may have moved on — and nothing for a table the transaction created (no baseline).
    private readonly Dictionary<(int Page, int Offset), (int? Baseline, int Value, bool FailedOnly)> _keptCounters = [];
    // The FailedOnly counters written since a statement last accepted them (AcceptCounters).
    private readonly Dictionary<(int Page, int Offset), (int? Baseline, int Value)> _pendingCounters = [];
    // The TDEF pages the open transaction has published its AutoNumbers on and let go of ahead of its COMMIT
    // (PublishAheadOfCommit), each with the image it published: the page's overlay image derives from that image, not
    // from the file, which another user may have changed since. The transaction's next lock of the page — or its
    // COMMIT — reads the page again and puts the overlay's counting on top of it (LockForUpdate).
    private readonly Dictionary<int, byte[]> _publishedEarly = [];
    // Usage-map records the open transaction retired, whose bytes ACE takes back only when it commits.
    private readonly List<(int Page, int Row)> _reclaimAtCommit = [];

    // What the open transaction's writes depend on being TRUE of OTHER rows — a foreign key's parent row
    // existing, say. Reading a row writes no page, so such a dependency is invisible to the page-conflict
    // check above, and two transactions can each check the other's precondition and commit to a state neither
    // would have allowed (write skew). Each condition is re-evaluated at commit, under the publication lock,
    // so a commit that would leave a dangling reference is refused instead. A savepoint rollback truncates
    // them with the writes that needed them. A condition registered under a key is held once: `_dependencyKeys`
    // holds the keys of the conditions `_dependencies` still has.
    private readonly List<(Func<bool> StillHolds, string Violation, object? Key)> _dependencies = [];
    private readonly HashSet<object> _dependencyKeys = [];
    // Whether anything this channel wrote has been published, so a close knows the session changed the file.
    private bool _published;

    private PageChannel(FileStream stream, JetFormatBase format, bool readOnly, bool exclusive, string path,
        IPageCodec? codec, ILockManager? locks)
    {
        _stream = stream;
        _readOnly = readOnly;
        IsExclusive = exclusive;
        _format = format;
        _path = path;
        _codec = codec;
        _identity = FileIdentity.Key(path);
        // A shared open, read-only included, registers this channel as a Jet user of the file, as each ACE connection
        // is one — two connections in one process included (measured, locking.md §3). An exclusive one takes no lock
        // file, as ACE. First, before anything is acquired: another user barring new connections refuses the open
        // here (IOException), and Open disposes the stream.
        _user = exclusive ? null : JetUserRegistration.Connect(path);
        // A test may inject its own manager; otherwise share the per-path one (refcounted, released on Dispose).
        _ownsLocks = locks is null;
        _locks = locks ?? MonitorLockManager.AcquireKey(_identity);
        _cache = PageCache.AcquireKey(_identity, registered: _user is null ? null : (path, format));
        _seenSchemaGeneration = _cache.SchemaGeneration;
        _cache.InitFileLength(stream.Length);
        // The commit slot this channel's batches write (PublishBatch): slot 0 when it holds the file exclusively,
        // its user's slot when it is registered, none otherwise — a read-only channel never publishes a batch.
        // The table's place is the same in every version a raise can move the file to.
        if (!readOnly && (exclusive ? 0 : _user?.User) is { } slot)
            _commitSlot = format.CommitByteTableOffset + slot * format.CommitByteSlotSize;
        Allocator = new PageAllocator(this);
    }

    // This channel's registration as a Jet user of the file — its user number, its lock-file handle and the locks it
    // holds through it — or null when it has none: an exclusive open, off Windows, or a lock file that could not be
    // opened. One per channel, as ACE's connections are users of their own, so the operating system arbitrates between
    // two channels of one process exactly as between two processes: a page or table one holds refuses the other, with
    // ACE's retries and errors. Without one the channel takes no lock, and its transactions are checked against the
    // other channels' only at commit (PublishTransaction).
    private readonly JetUserRegistration? _user;

    // The file offset of this channel's commit slot, or null when it writes none (see the constructor).
    private readonly int? _commitSlot;

    /// <summary>The allocator owning this channel's page allocation and release paths.</summary>
    internal PageAllocator Allocator { get; }

    /// <summary>Whether a transaction is currently open on this channel.</summary>
    public bool InTransaction => _active is not null;

    /// <summary>Whether this channel was opened read-only, so it can never write a page.</summary>
    internal bool IsReadOnly => _readOnly;

    /// <summary>Refuses <paramref name="write"/> when this channel is read-only, with ACE's message for that kind of
    /// write; <paramref name="objectName"/> names the table or query a design change is to.</summary>
    /// <remarks>ACE reports these first, before the read-only refusal: the syntax, a name's legality, and a missing
    /// table, query, column, index or constraint that the statement finds or drops. It reports these after it: an
    /// INSERT's column list and values, a duplicate name, an index's columns, a relationship's target (verified vs
    /// ACE). So each writer calls this between the two, and the refusals below — at the first page written — are
    /// only what a writer that misses it meets.</remarks>
    internal void RefuseIfReadOnly(ReadOnlyWrite write, string? objectName = null)
    {
        if (!_readOnly) return;
        throw new InvalidOperationException(write switch
        {
            ReadOnlyWrite.Row => "Operation must use an updateable query.",
            ReadOnlyWrite.RowDelete => "Could not delete from specified tables.",
            ReadOnlyWrite.Design => $"Cannot modify the design of table '{objectName}'. It is in a read-only database.",
            _ => "Cannot update. Database or object is read-only.",
        });
    }

    /// <summary>Holds a freed page until this channel closes. Inside a transaction the page is staged with it —
    /// released only if the transaction commits; outside one it is committed at once.</summary>
    internal void ReleaseAtClose(int page)
    {
        if (_readOnly)
            throw new InvalidOperationException("This channel was opened read-only.");
        (_active is not null ? _releasing : _released).Add(page);
    }

    /// <summary>
    /// Records a condition this transaction's writes depend on — something it read and found true of rows it
    /// did not write, such as the parent row a foreign key needs. <paramref name="stillHolds"/> is evaluated
    /// again at commit, and a commit whose condition has since stopped holding is refused with
    /// <paramref name="violation"/>. Outside a transaction there is nothing to record: the statement's check
    /// and its write are one publication, which no other connection can interleave with.
    /// <para>A <paramref name="key"/> names the condition, by its own equality: one already held under an equal
    /// key is not registered again, so a transaction inserting a thousand children of one parent checks that
    /// parent once at commit, not a thousand times.</para>
    /// </summary>
    public void DependOn(Func<bool> stillHolds, string violation, object? key = null)
    {
        ArgumentNullException.ThrowIfNull(stillHolds);
        if (_active is null) return;
        if (key is not null && !_dependencyKeys.Add(key)) return;
        _dependencies.Add((stillHolds, violation, key));
    }

    /// <summary>The committed pages held for release at close, in the order they were freed.</summary>
    internal IReadOnlyList<int> PagesReleasedAtClose => _released;

    /// <summary>Forgets the held pages once they have been returned to the global free map.</summary>
    internal void ClearPagesReleasedAtClose() => _released.Clear();

    /// <summary>Whether this channel has published a page write since it opened.</summary>
    internal bool HasPublishedWrites => _published;

    private JetFormatBase _format;
    private long _seenSchemaGeneration;

    /// <summary>
    /// The resolved on-disk format, refreshed when another handle publishes a schema change. A database's
    /// format version can move up in place when DDL introduces a type that needs a newer one, which is what
    /// Access itself does. Local upgrades and rollback update the same format state.
    /// </summary>
    public JetFormatBase Format
    {
        get
        {
            if (_seenSchemaGeneration != _cache.SchemaGeneration)
                ResyncFormatVersion();
            return _format;
        }
    }

    internal void MarkSchemaChanged()
    {
        if (_active is not null) _schemaDirty = true;
        else _cache.MarkSchemaChanged();
    }

    /// <summary>The shared invalidation generation for catalog/format state on this file.</summary>
    internal long SchemaGeneration => _cache.SchemaGeneration;

    /// <summary>Changes whenever a committed page image is replaced or removed.</summary>
    internal long ImageGeneration => _cache.ImageGeneration;

    public int PageSize => _format.PageSize;

    /// <summary>The most pages a file can hold: ACE's 2 GiB limit (524288 Jet4/ACE pages).</summary>
    internal int MaxPageCount => (int)((1L << 31) / PageSize);

    /// <summary>Number of pages currently in the file — or, inside a transaction, the logical count including
    /// pages the overlay has allocated but not yet written to disk. Read from the shared cache's record of the
    /// file's length rather than the stream, so it costs no syscall.</summary>
    /// <remarks>Inside a transaction it is never less than the file's own count: another handle can commit pages
    /// onto the end of the file while this transaction is open, and a row, index entry or long value on one of them
    /// is committed data this transaction must be able to read — a commit-time dependency check reads exactly
    /// that. Alone on the file the two are the same, since this transaction's own pages do not reach the file
    /// until it commits.</remarks>
    public int PageCount => _active is not null
        ? Math.Max(_txPageCount, (int)(_cache.FileLength / PageSize))
        : (int)(_cache.FileLength / PageSize);

    /// <summary>
    /// Opens a database file, sniffs its Jet/ACE version from page 0 and resolves the
    /// matching <see cref="JetFormatBase"/>.
    /// </summary>
    /// <remarks><c>exclusive</c> takes the file for this channel alone (<c>FileShare.None</c>), the equivalent
    /// of ACE's <c>Mode=Share Exclusive</c>: the open fails while anyone else holds the file, and nobody else
    /// can open it until this channel closes. Off by default, because the ordinary state of a Jet database is
    /// shared. Operations that rewrite the whole file — changing a password or the page encoding — require it,
    /// exactly as ACE requires an exclusive connection for the same statements.</remarks>
    public static PageChannel Open(string path, bool readOnly = true, string? password = null,
        ILockManager? locks = null, bool exclusive = false)
    {
        // A Jet/ACE file is a shared-file database — Access, ODBC and OLE DB all open it with multiple
        // concurrent handles (a store's long-lived connection plus per-context connections to the same
        // file, as EF's test infrastructure does). So the default is to share read+write; an exclusive open
        // (FileShare.None) throws IOException the moment a second connection touches the same .accdb, which
        // is exactly what the caller of one asks for. Coexisting handles stay coherent by sharing a single
        // per-file write-through buffer pool (PageCache) rather than each caching independently — one pool
        // means one connection's writes are seen by the others, as the old straight-to-disk reads guaranteed.
        var stream = new FileStream(
            path,
            FileMode.Open,
            readOnly ? FileAccess.Read : FileAccess.ReadWrite,
            exclusive ? FileShare.None : FileShare.ReadWrite);

        try
        {
            var format = JetFormatBase.Detect(stream);

            // Read page 0 (always unencrypted) to detect encryption: a nonzero database key at 0x3E means the
            // data pages are ACE-encrypted, and the EncryptionInfo descriptor lives in the clear on this page.
            // A file too short to hold one page is truncated, not a database — say so, rather than letting
            // ReadExactly raise EndOfStreamException, which callers handling a damaged file do not catch.
            if (stream.Length < format.PageSize)
                throw new InvalidDataException(
                    $"The file is {stream.Length} bytes, shorter than the {format.PageSize}-byte page 0 a "
                    + $"{format.Version} database begins with.");
            var page0 = new byte[format.PageSize];
            stream.Seek(0, SeekOrigin.Begin);
            stream.ReadExactly(page0);
            int databaseKey = DatabaseDefinitionPage.ReadDatabaseKey(page0, format);
            // A nonzero database key means the pages are encrypted. ACE (.accdb) uses Office Agile encryption
            // (with a password); the pre-ACE Jet 3/4 formats (.mdb and the .mdw workgroup file) use the legacy
            // RC4 scheme keyed by the database key alone (no password).
            // ACE (.accdb) may use Agile (XML descriptor) or the older Office "Standard"/CryptoAPI scheme (binary
            // descriptor) — detect by descriptor, trying Agile first then Standard. The pre-ACE Jet 3/4 formats
            // (.mdb and the .mdw workgroup file) use the legacy RC4 scheme keyed by the database key alone.
            IPageCodec? codec = format.IsAccdb
                ? (IPageCodec?)AgileEncryption.TryCreate(page0, databaseKey, password, format)
                    ?? OfficeStandardEncryption.TryCreate(page0, databaseKey, password, format)
                : JetLegacyEncryption.TryCreate(databaseKey);

            // A nonzero database key means the file is encrypted; if no codec recognised the descriptor the scheme
            // is unsupported — fail clearly rather than decoding ciphertext as plaintext (which corrupts downstream).
            if (databaseKey != 0 && codec is null)
                throw new NotSupportedException("The database is encrypted with an unsupported scheme.");

            return new PageChannel(stream, format, readOnly, exclusive, path, codec, locks);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>Whether this channel holds the file to itself — see the <c>exclusive</c> argument to
    /// <see cref="Open"/>. Rewriting the file as a whole is only allowed on such a channel.</summary>
    public bool IsExclusive { get; }

    /// <summary>The database file this channel reads and writes.</summary>
    internal string Path => _path;

    /// <summary>The Jet user number this channel holds on the file — each channel its own — or null when it is not
    /// registered: an exclusive open, off Windows, or a lock file that could not be opened (see
    /// <see cref="JetUserRegistration"/>).</summary>
    internal int? UserNumber => _user?.User;

    /// <summary>Bars new connections, or admits them again, through this channel's registration
    /// (<see cref="JetUserRegistration.SetConnectionControl"/>); a channel that is not registered does nothing.</summary>
    internal void SetConnectionControl(bool bar) => _user?.SetConnectionControl(bar);

    /// <summary>Rejects a page number that cannot exist in this file. Page numbers come out of the file
    /// itself, so a bad one is corruption — without this the read seeks past the end and throws
    /// <c>EndOfStreamException</c>, which callers handling a damaged file do not catch. Called only on the
    /// paths that reach disk: a cache or overlay hit has already proved the page exists, and
    /// <see cref="PageCount"/> measures the stream.</summary>
    private void ValidatePageNumber(int pageNumber)
    {
        if (pageNumber < 0 || pageNumber >= PageCount)
            throw new InvalidDataException(
                $"Page {pageNumber} is outside the database, which holds {PageCount} pages.");
    }

    /// <summary>Reads a single page into a freshly allocated buffer.</summary>
    public PageBuffer ReadPage(int pageNumber)
    {
        var buffer = new byte[PageSize];
        ReadPage(pageNumber, buffer);
        return new PageBuffer(buffer, pageNumber);
    }

    /// <summary>
    /// Returns a page backed by the shared cache's own buffer, without copying it out — a zero-allocation read
    /// for callers that consume the bytes without mutating them (e.g. decoding one row
    /// during an index seek, where <see cref="ReadPage(int)"/>'s per-call 4 KB copy dominated). On a cache miss
    /// the page is read from disk and cached, then returned. Copy-on-write keeps an existing image stable,
    /// but it is no longer current after a write or eviction: a caller retaining it as a decode identity
    /// must compare it with a fresh read before reusing the decode.
    /// </summary>
    internal PageBuffer ReadPageShared(int pageNumber)
    {
        // Read-your-own-writes: a page this transaction has written lives only in the overlay until commit.
        if (_active is not null && _overlay.TryGetValue(pageNumber, out byte[]? buffered))
            return new PageBuffer(buffered, pageNumber);

        // Cache hit: the cache's own lock makes the lookup atomic and copy-on-write Store keeps the returned
        // array stable, so no page lock is needed — this is the hot path and stays coordination-free.
        if (_cache.TryGetArray(pageNumber, out byte[] cached))
            return new PageBuffer(cached, pageNumber);

        // Miss: read from disk under a shared page lock so a concurrent cross-handle write of this page can't
        // tear the read. Re-check the cache after acquiring — another handle may have filled it while we waited.
        _locks?.EnterShared(pageNumber);
        try
        {
            if (_cache.TryGetArray(pageNumber, out cached))
                return new PageBuffer(cached, pageNumber);

            ValidatePageNumber(pageNumber);
            var buffer = new byte[PageSize];
            long offset = (long)pageNumber * PageSize;
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.ReadExactly(buffer);
            _codec?.DecryptPage(pageNumber, buffer);
            _cache.Adopt(pageNumber, buffer); // handed back read-only, as a cache hit's array is
            return new PageBuffer(buffer, pageNumber);
        }
        finally { _locks?.ExitShared(pageNumber); }
    }

    /// <summary>Reads a single page into the supplied buffer (must be at least <see cref="PageSize"/>).</summary>
    public void ReadPage(int pageNumber, Span<byte> destination)
    {
        if (destination.Length < PageSize)
            throw new ArgumentException($"Buffer must be at least {PageSize} bytes.", nameof(destination));

        // Read-your-own-writes: a page this transaction has written lives only in the overlay until commit.
        if (_active is not null && _overlay.TryGetValue(pageNumber, out byte[]? buffered))
        {
            buffered.CopyTo(destination);
            return;
        }

        // Cache hit: served under the cache's own lock, no page lock needed (the hot path).
        if (_cache.TryRead(pageNumber, destination))
            return;

        // Miss: read from disk under a shared page lock (coordinates with a cross-handle write), re-checking
        // the cache after acquiring in case another handle filled it while we waited.
        _locks?.EnterShared(pageNumber);
        try
        {
            if (_cache.TryRead(pageNumber, destination))
                return;

            ValidatePageNumber(pageNumber);
            long offset = (long)pageNumber * PageSize;
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.ReadExactly(destination[..PageSize]);
            _codec?.DecryptPage(pageNumber, destination[..PageSize]);
            _cache.Store(pageNumber, destination[..PageSize]);
        }
        finally { _locks?.ExitShared(pageNumber); }
    }

    /// <summary>
    /// Writes a full page back to the file at <paramref name="pageNumber"/>. If the page lies beyond
    /// the current end of the file, the file is grown to accommodate it (any intervening pages are
    /// zero-filled). This is legitimate: a page taken from the global free-pages map can lie past the
    /// physical end (the map pre-accounts for growth, and allocation defers the physical write), and
    /// writing the page is what materialises it — the same growth Access performs on such a write.
    /// </summary>
    internal void WritePage(int pageNumber, ReadOnlySpan<byte> source) => WritePage(pageNumber, source, parsed: null);

    /// <summary>
    /// Writes a page as <see cref="WritePage(int, ReadOnlySpan{byte})"/> does, and keeps <paramref name="parsed"/>
    /// as its parse (see <see cref="SetParsedPage"/>), so the next read of the page need not decode the bytes just
    /// written. The caller vouches that it is exactly what parsing <paramref name="source"/> gives. Attached in the
    /// same publication as the write, so no other channel's write of the page can come between the two.
    /// </summary>
    internal void WritePage(int pageNumber, ReadOnlySpan<byte> source, object? parsed)
    {
        if (_readOnly)
            throw new InvalidOperationException("This channel was opened read-only.");
        if (source.Length != PageSize)
            throw new ArgumentException($"A page write must be exactly {PageSize} bytes.", nameof(source));
        ArgumentOutOfRangeException.ThrowIfNegative(pageNumber);

        // ACE-only full-database probe: the file reaches exactly 2 GiB (524288 Jet4/ACE pages)
        // and rejects the next allocation. Check before staging a page or extending the stream.
        if (pageNumber >= MaxPageCount)
            throw new InvalidOperationException("The database cannot grow beyond 2 GiB, the maximum file size.");

        // Inside a transaction, defer the write into the private overlay — invisible to other channels until
        // commit. Snapshot the page's prior overlay state (once per savepoint frame) so a savepoint rollback can
        // restore it, then buffer a private copy of the new bytes and advance the logical page count.
        if (_active is not null)
        {
            // A page let go after an early publication: what the caller read and changed derives from the image
            // published, and goes on top of the page as the lock now reads it, as the overlay's own image does.
            byte[]? published = _publishedEarly.GetValueOrDefault(pageNumber);
            LockForUpdate(pageNumber);
            if (_active.NeedsBeforeImage(pageNumber))
                _active.RecordBeforeImage(pageNumber, _overlay.TryGetValue(pageNumber, out byte[]? prior) ? prior : null);
            bool rebased = published is not null && _commitBaselines[pageNumber] is not null;
            _overlay[pageNumber] = rebased
                ? Rebased(pageNumber, source[..PageSize], published!, _commitBaselines[pageNumber]!)
                : source[..PageSize].ToArray();
            if (parsed is null || rebased) _overlayParsed.Remove(pageNumber);
            else _overlayParsed[pageNumber] = parsed;
            if (pageNumber >= _txPageCount) _txPageCount = pageNumber + 1;
            return;
        }

        WriteThrough(pageNumber, source, parsed);
    }

    /// <summary>Writes a page to disk and the shared cache (the committed path) outside a transaction: a batch of
    /// its own (see <see cref="PublishBatch"/>).</summary>
    private void WriteThrough(int pageNumber, ReadOnlySpan<byte> source, object? parsed)
    {
        byte[] copy = source[..PageSize].ToArray();
        _cache.PublishLocked(() =>
        {
            // A write that leaves the page as committed is no batch (PublishBatch), and has no use for the page's lock.
            if (ReadCommittedPageOrNull(pageNumber) is { } committed && SamePage(pageNumber, committed, copy)) return;
            // Taken before the write replaces it: what a failed slot write puts the page back to. The page's lock
            // goes after the batch's closing slot write, as ACE's does (locking.md §6).
            byte[]? baseline = LockForWrite(pageNumber);
            try { PublishBatch([pageNumber], _ => copy, _ => baseline, _ => parsed); }
            finally { if (baseline is not null) _user?.Unlock(JetLockRegion.WriteRead, pageNumber, exclusive: true); }
        });
    }

    /// <summary>Inside a transaction, takes the write lock on <paramref name="pageNumber"/> the first time the
    /// transaction is to change it, and notes the committed image the change derives from (<see cref="LockForWrite"/>);
    /// a page already locked is left as it is. A page written is locked here on its first write; a page whose current
    /// contents decide the change — a table's counters, the global maps — is locked here before it is read, as ACE
    /// locks, re-reads and then changes it (locking.md §4), so no image read before the lock is used after it. Held as
    /// a written page's lock is: to the end of the transaction, or of the savepoint frame that took it. Outside a
    /// transaction it does nothing: each page write is a batch of its own and locks its page (<see cref="WriteThrough"/>).</summary>
    /// <param name="pageNumber">The page.</param>
    /// <param name="refuseStale">False for a caller that reads everything it uses only after this call — a rollback
    /// putting the global maps right, the close's merge: a lock newly taken after another user changed the page then
    /// only drops the cache, rather than refusing the statement as <see cref="LockForWrite"/> otherwise does.</param>
    /// <remarks>A TDEF page the transaction let go of after publishing its AutoNumbers ahead of COMMIT
    /// (<see cref="PublishAheadOfCommit"/>) is locked again here, and read as the file has it now — another user may
    /// have drawn an AutoNumber, or counted a row, meanwhile — and the transaction's overlay image of it is put on top
    /// of that (<see cref="Rebased"/>), as ACE's COMMIT writes its counting onto the page as the file has it then
    /// (locking.md §5). The counters the transaction keeps through a rollback move with it: each is still its change
    /// on top of what is committed.</remarks>
    internal void LockForUpdate(int pageNumber, bool refuseStale = true)
    {
        if (_active is null || _readOnly || _commitBaselines.ContainsKey(pageNumber)) return;
        byte[]? committed = _commitBaselines[pageNumber] = LockForWrite(pageNumber, refuseStale);
        if (!_publishedEarly.Remove(pageNumber, out byte[]? published) || committed is null) return;

        if (_overlay.TryGetValue(pageNumber, out byte[]? image))
        {
            _overlay[pageNumber] = Rebased(pageNumber, image, published, committed);
            _overlayParsed.Remove(pageNumber);
        }
        foreach ((int page, int offset) in _keptCounters.Keys.Where(k => k.Page == pageNumber).ToArray())
        {
            int moved = CounterMove(offset, published, committed);
            (int? baseline, int value, bool failedOnly) = _keptCounters[(page, offset)];
            _keptCounters[(page, offset)] = (baseline + moved, unchecked(value + moved), failedOnly);
        }
        foreach ((int page, int offset) in _pendingCounters.Keys.Where(k => k.Page == pageNumber).ToArray())
        {
            int moved = CounterMove(offset, published, committed);
            (int? baseline, int value) = _pendingCounters[(page, offset)];
            _pendingCounters[(page, offset)] = (baseline + moved, unchecked(value + moved));
        }
    }

    /// <summary>How far another user has moved the counter at <paramref name="offset"/> from the image this transaction
    /// published to the page as the file has it now.</summary>
    private static int CounterMove(int offset, byte[] published, byte[] committed) => unchecked(
        BinaryPrimitives.ReadInt32LittleEndian(committed.AsSpan(offset)) - BinaryPrimitives.ReadInt32LittleEndian(published.AsSpan(offset)));

    /// <summary><paramref name="image"/> — the transaction's image of TDEF page <paramref name="page"/>, derived from
    /// <paramref name="published"/>, what it published of it ahead of COMMIT — put on top of
    /// <paramref name="committed"/>, the page as the file has it now: each counter the transaction has written there
    /// as its change added to the file's value, every other byte the transaction has left as published as the file
    /// has it.</summary>
    private byte[] Rebased(int page, ReadOnlySpan<byte> image, byte[] published, byte[] committed)
    {
        byte[] rebased = image[..PageSize].ToArray();
        for (int i = 0; i < PageSize; i++)
            if (rebased[i] == published[i]) rebased[i] = committed[i];
        foreach (int offset in CounterOffsets(page))
            BinaryPrimitives.WriteInt32LittleEndian(rebased.AsSpan(offset), unchecked(
                BinaryPrimitives.ReadInt32LittleEndian(image[offset..]) + CounterMove(offset, published, committed)));
        return rebased;
    }

    /// <summary>The offsets on <paramref name="page"/> of the counters the open transaction has written there
    /// (<see cref="KeepThroughRollback"/>).</summary>
    private IEnumerable<int> CounterOffsets(int page) =>
        _keptCounters.Keys.Concat(_pendingCounters.Keys).Where(k => k.Page == page).Select(k => k.Offset).Distinct();

    /// <summary>Takes this user's write lock on <paramref name="pageNumber"/>, which is about to be written for the
    /// first time in a batch, and then reads the committed image the write derives from (null for a page past the
    /// end of the committed file). Locked only for a registered user, and only a page already in the file: ACE locks
    /// no page it appends (locking.md §4, §6). Held until the batch that writes it has ended, or the transaction or
    /// savepoint frame that first wrote it is rolled back — <see cref="_commitBaselines"/> records which pages.</summary>
    /// <exception cref="InvalidOperationException">Another user holds the page and does not let go within ACE's
    /// retries.</exception>
    /// <exception cref="StaleReadException">The lock was newly taken, another user has committed since this
    /// statement's scope began, and the page as the file now has it is not the image this process held of it before
    /// (or that image cannot be known — <see cref="PageCache.RefreshAtLock"/>): what the caller read of it is older
    /// than the file, so the write cannot go ahead on it. The cache is dropped first; the caller rolls back what it
    /// did and runs the statement again. Not with <paramref name="refuseStale"/> false (see
    /// <see cref="LockForUpdate"/>).</exception>
    private byte[]? LockForWrite(int pageNumber, bool refuseStale = true)
    {
        if (_user is not { } user || pageNumber >= (int)(_cache.FileLength / PageSize))
            return ReadCommittedPageOrNull(pageNumber);

        // Joined: this channel has held the page throughout, so nobody else has written it since this caller read it.
        // Newly taken: the page is read again, as ACE reads a page it has just locked, and what the caller may have
        // read of it — the image this process held before another process's commit dropped it — must still be what
        // the file holds; a commit that left the page as it was is no reason to stop. Another channel of this process
        // publishes through the shared cache, and not while this statement holds the publish gate, so what the
        // statement read of the page is what that channel left.
        if (user.Lock(JetLockRegion.WriteRead, pageNumber, exclusive: true, JetLockPurpose.PageWrite) == true
            && (!_cache.RefreshAtLock(pageNumber, out byte[]? before)
                || before is not null && !SamePage(pageNumber, before, ReadCommittedPageOrNull(pageNumber)))
            && refuseStale)
        {
            user.Unlock(JetLockRegion.WriteRead, pageNumber, exclusive: true);
            throw new StaleReadException();
        }

        byte[]? baseline = ReadCommittedPageOrNull(pageNumber);
        // A page with no baseline holds no lock. A transactional write made outside the publish gate can meet another
        // channel's failed publication truncating the file back below the page.
        if (baseline is null) user.Unlock(JetLockRegion.WriteRead, pageNumber, exclusive: true);
        return baseline;
    }

    /// <summary>The pages the open transaction holds write locks on (see <see cref="LockForWrite"/>) — taken before its
    /// state is cleared, for <see cref="Unlock"/> to let go of once its commit's batch has ended or its rollback is
    /// done. Empty for a channel that takes no locks.</summary>
    private int[] LockedPages() => _user is null
        ? []
        : [.. _commitBaselines.Where(kv => kv.Value is not null).Select(kv => kv.Key)];

    private void Unlock(int[] pages)
    {
        foreach (int page in pages) _user!.Unlock(JetLockRegion.WriteRead, page, exclusive: true);
    }

    // The table locks, long-value locks and referenced-key locks this channel holds (locking.md §4), each as its region,
    // its page — a table's definition page, a chained long value's first page, or the parent index page a foreign key's
    // referenced key was found on — and whether it is exclusive, in the order taken. Those in `_transactionLocks` — table
    // locks and referenced keys — belong to the open transaction and go when it ends, none at a savepoint rollback: ACE
    // keeps a table a failed statement, or one rolled back in a nested transaction, wrote in use until the transaction
    // ends (measured). Transaction.TableLockCountAt says how many of them were taken before a frame opened
    // (InUseByThisConnection). Those in `_statementLocks` belong to the statement and go when its outermost scope ends
    // (`_scopeDepth`), unless a result still to be read takes its table locks over (KeepStatementTableLocks). Per
    // channel, and so — as the overlay — owned by the channel's own thread.
    private readonly List<(JetLockRegion Region, int Page, bool Exclusive)> _transactionLocks = [];
    private readonly List<(JetLockRegion Region, int Page, bool Exclusive)> _statementLocks = [];
    private int _scopeDepth;

    // The table locks owned by something other than a statement or a transaction — a statement's result while it is
    // read, or one read or enumeration of a table outside every statement (HeldTableLocks) — until it lets them go, or
    // this channel closes. Under its own lock: a result can be let go of from another thread.
    private readonly List<HeldTableLocks> _heldTableLocks = [];

    /// <summary>Takes the table locks ACE takes for <paramref name="use"/> of the table whose definition is on
    /// <paramref name="tdefPage"/> (locking.md §4): its shared byte in the table-read region to read it; to write it,
    /// that and the table-write region's shared byte, taken under a momentary exclusive lock in the deny-write region;
    /// for a design change, the table-read region exclusively. A lock the channel already holds is not taken twice.</summary>
    /// <returns>The locks, when the caller is to own them — a read outside every statement, which holds them until it
    /// disposes them; null otherwise.</returns>
    /// <remarks>
    /// <para>A table's write and design locks belong to the open transaction, held until it commits or rolls back —
    /// for a statement on its own, its implicit transaction, so ACE's DDL locks go inside the statement as measured,
    /// and in an explicit one until <c>COMMIT</c>, as ACE holds a table it has written or changed the design of
    /// (measured: another connection's <c>ALTER TABLE</c> is refused until then, after a statement that failed too);
    /// outside one, to the statement — the outermost <see cref="ReadConsistent{T}"/> or
    /// <see cref="WriteExclusive{T}"/> scope. A statement's read locks belong to the statement inside a transaction
    /// too — a query's, and those of the tables a writing statement only reads — since ACE's go when the query's
    /// reader closes, before <c>COMMIT</c>, and an <c>INSERT … SELECT</c>'s source is another connection's to alter
    /// once the statement has returned (measured); a query's result can take them over
    /// (<see cref="KeepStatementTableLocks"/>). A design change of a table this connection itself has in use is
    /// refused at once (<see cref="InUseByThisConnection"/>).
    /// Outside every statement — a table read through the Core API, or a query's rows read after it returned, in a
    /// transaction or not — the read lock is the caller's, for the read or the enumeration it makes; a write or design
    /// change outside both a statement and a transaction takes none (the Core API's writes open a transaction of their
    /// own). Only a registered user takes the operating-system locks. Every channel keeps the record of what its
    /// statement, its transaction and its results have in use — an exclusive one and one off Windows too, since ACE
    /// refuses a connection's own design change on an exclusive open as on a shared one (measured) — but for the
    /// caller-owned read, which an unregistered channel does not record. A read-only channel takes its read locks
    /// only.</para>
    /// <para>Refused, each lock is retried as ACE retries and then refused with ACE's 3211 naming
    /// <paramref name="name"/> — the read byte with 3008, after two rounds when the table is opened for reading and one
    /// when it is opened for writing, as ACE retries a <c>SELECT</c>, <c>UPDATE</c> or <c>DELETE</c> and an
    /// <c>INSERT</c> (measured). One newly taken after another user's slot has moved since the statement's scope
    /// looked throws <see cref="StaleReadException"/>, with what this call took let go: the other user may have changed
    /// the table's design, and what the statement planned against is older than the file. A caller-owned one, with no
    /// statement to run again, drops the cache instead, so the rows are read as the file has them.</para>
    /// </remarks>
    internal HeldTableLocks? LockTable(int tdefPage, string name, TableUse use)
    {
        JetUserRegistration? user = _user;
        if (_readOnly && use != TableUse.Read) return null;
        bool statement = _scopeDepth > 0 && (_active is null || use == TableUse.Read);
        bool callers = _scopeDepth == 0 && use == TableUse.Read;
        if (!statement && (callers ? user is null : _active is null)) return null;
        List<(JetLockRegion Region, int Page, bool Exclusive)> owner =
            statement ? _statementLocks : callers ? [] : _transactionLocks;
        // Held already by the owner, or — for a statement — by its transaction, which outlasts it.
        bool Holds(JetLockRegion region, bool exclusive) =>
            owner.Contains((region, tdefPage, exclusive)) || statement && _transactionLocks.Contains((region, tdefPage, exclusive));

        int from = owner.Count;
        bool took = false;
        try
        {
            if (callers || !Holds(JetLockRegion.TableRead, use == TableUse.Design))
            {
                if (use == TableUse.Design && InUseByThisConnection(tdefPage))
                    throw new InvalidOperationException(JetUserRegistration.TableInUseMessage(name));
                // Opened for reading, ACE retries the read byte two rounds; for writing — an INSERT's target — one.
                took |= user?.Lock(JetLockRegion.TableRead, tdefPage, use == TableUse.Design,
                    use == TableUse.Read ? JetLockPurpose.TableRead : JetLockPurpose.Table, name) == true;
                owner.Add((JetLockRegion.TableRead, tdefPage, use == TableUse.Design));
            }
            if (use == TableUse.Write && !Holds(JetLockRegion.TableWrite, exclusive: false))
            {
                user?.Lock(JetLockRegion.TableDenyWrite, tdefPage, exclusive: true, JetLockPurpose.Table, name);
                try
                {
                    took |= user?.Lock(JetLockRegion.TableWrite, tdefPage, exclusive: false, JetLockPurpose.Table, name) == true;
                    owner.Add((JetLockRegion.TableWrite, tdefPage, false));
                }
                finally { user?.Unlock(JetLockRegion.TableDenyWrite, tdefPage, exclusive: true); }
            }
        }
        catch
        {
            UnlockHeld(owner, from);
            throw;
        }

        if (took && _cache.RefreshIfOthersCommitted() && !callers)
        {
            UnlockHeld(owner, from);
            throw new StaleReadException(JetUserRegistration.TableInUseMessage(name));
        }

        // A design change reads the definition page and lays it out afresh, which can move the counters on it (a dropped
        // index's entry goes from the real-index block): a page let go after an early publication is locked again
        // first, so what it reads already carries the file's counting, at the offsets the transaction counted on.
        if (use == TableUse.Design && _publishedEarly.ContainsKey(tdefPage)) LockForUpdate(tdefPage);
        return callers ? Hold(owner, started: true) : null;
    }

    /// <summary>Hands the table locks the statement has taken to its result, which holds them until it has been read,
    /// as ACE's open reader holds the table's read byte until it is closed — whether or not its rows have all been
    /// fetched, inside a transaction too (measured, locking.md §4, §5). Called by the statement as it returns, inside
    /// its outermost scope; null when there is nothing to hand over — inside an enclosing scope, which holds them to its
    /// own end, or with no table lock the statement owns (a transaction's are the transaction's).</summary>
    /// <remarks>The result disposes them when its rows have been read or it is closed. One whose reading has not begun
    /// (<see cref="HeldTableLocks.Start"/>) by the time this channel's next statement begins lets them go then: its
    /// rows are read, if ever, as a Core API read is, each enumeration under locks of its own. This channel's close
    /// lets go of all of them.</remarks>
    internal HeldTableLocks? KeepStatementTableLocks()
    {
        if (_scopeDepth != 1) return null;
        List<(JetLockRegion Region, int Page, bool Exclusive)> tables = [.. _statementLocks.Where(l => l.Region != JetLockRegion.WriteRead)];
        if (tables.Count == 0) return null;
        _statementLocks.RemoveAll(l => l.Region != JetLockRegion.WriteRead);
        return Hold(tables, started: false);
    }

    private HeldTableLocks Hold(List<(JetLockRegion Region, int Page, bool Exclusive)> locks, bool started)
    {
        var held = new HeldTableLocks(this, locks, started);
        lock (_heldTableLocks) _heldTableLocks.Add(held);
        return held;
    }

    /// <summary>Lets go of <paramref name="held"/>'s locks, once.</summary>
    internal void Release(HeldTableLocks held)
    {
        lock (_heldTableLocks)
            if (!_heldTableLocks.Remove(held)) return;
        UnlockHeld(held.Locks, 0);
    }

    /// <summary>As a statement begins: lets go of the locks of every result whose reading never began
    /// (<see cref="KeepStatementTableLocks"/>).</summary>
    private void ReleaseUnreadResults()
    {
        HeldTableLocks[] unread;
        lock (_heldTableLocks)
        {
            if (_heldTableLocks.Count == 0) return;
            unread = [.. _heldTableLocks.Where(h => !h.Started)];
        }
        foreach (HeldTableLocks held in unread) Release(held);
    }

    /// <summary>Whether this connection itself has the table whose definition is on <paramref name="tdefPage"/> in use,
    /// which refuses its own design change of the table at once, with 3211 and no retries, as ACE refuses it
    /// (measured, locking.md §4): a result of its own still open on the table, or one read or enumeration of it; or its
    /// open transaction, having written the table in an earlier statement — one that matched no row, failed or was
    /// rolled back in a nested transaction included. Not a table the transaction only read, nor one it created or
    /// changed the design of (it then holds the design lock, and is not asked here).</summary>
    private bool InUseByThisConnection(int tdefPage)
    {
        // The innermost frame is the statement's own (a lone statement's transaction, or its savepoint in an explicit
        // one): what the transaction held when it opened, earlier statements took.
        int earlier = _active is { } transaction ? transaction.TableLockCountAt(new Savepoint(transaction.Depth - 1)) : 0;
        for (int i = 0; i < earlier && i < _transactionLocks.Count; i++)
            if (_transactionLocks[i] == (JetLockRegion.TableWrite, tdefPage, false)) return true;
        lock (_heldTableLocks)
            return _heldTableLocks.Exists(held => held.Locks.Contains((JetLockRegion.TableRead, tdefPage, false)));
    }

    /// <summary>Runs <paramref name="read"/>, the read of a chained long value whose first page is
    /// <paramref name="firstPage"/>, under this user's shared byte on that page, as ACE reads one (locking.md §4): a
    /// writer holding the page refuses it, retried as ACE retries and then refused with ACE's 3624. Only the first page
    /// of a chain is locked, and no page of a value stored on a single page or in its row — ACE locks none of those
    /// (measured).</summary>
    /// <remarks>
    /// <para>Held for the statement — the outermost <see cref="ReadConsistent{T}"/> or <see cref="WriteExclusive{T}"/>
    /// scope — inside an explicit transaction too, since ACE lets it go after the statement there as outside one; one
    /// newly taken after another user's slot has moved since the scope looked throws <see cref="StaleReadException"/>,
    /// with the lock let go, as <see cref="LockTable"/> does: what the statement read may be older than the file. A read
    /// outside every scope — a query's rows read after the statement returned, a Core API call — holds it for the read
    /// alone, and a lock newly taken there drops the cache if another user has committed, so the chain is read as the
    /// file has it.</para>
    /// <para>Only a registered user, read-only included, as ACE's read-only user takes it too; a page this connection
    /// holds already, a write lock included, is joined (<see cref="JetUserRegistration.Lock"/>).</para>
    /// </remarks>
    internal T ReadLongValue<T>(int firstPage, Func<T> read)
    {
        LockPage(firstPage, JetLockPurpose.LongValueRead, out bool alone);
        if (!alone) return read();
        try { return read(); }
        finally { UnlockPage(firstPage, exclusive: false); }
    }

    /// <summary>Lets go of a page lock <see cref="LockPage"/> left to its caller.</summary>
    internal void UnlockPage(int page, bool exclusive) => _user!.Unlock(JetLockRegion.WriteRead, page, exclusive);

    /// <summary>Takes this user's lock on <paramref name="page"/> in the page write/read region for something other
    /// than writing it (locking.md §4): on the first page of a chained long value, the shared byte to read it
    /// (<see cref="ReadLongValue{T}"/>), and exclusively for an <c>UPDATE</c> that replaces it or a <c>DELETE</c> that
    /// removes it, as ACE's writer locks the old chain's first page before it gives the chain back (measured); on the
    /// parent's index page a foreign key's referenced key was found on, the shared byte for a child row's check, as
    /// ACE's <c>INSERT</c> and foreign-key <c>UPDATE</c> take it (measured). A refusal ends as
    /// <see cref="JetUserRegistration.Lock"/> says for the purpose: 3624 for a read and a referenced key, 3202 for a
    /// replacement; a removal refused returns false, and the caller deletes the row and leaves the chain allocated, as
    /// ACE does (measured).</summary>
    /// <param name="page">The chain's first page, or the index page holding the referenced key.</param>
    /// <param name="purpose">What the page is locked for.</param>
    /// <param name="alone">True when the lock belongs to no statement or transaction — a long-value read or write
    /// outside every statement — and the caller lets it go (<see cref="UnlockPage"/>) once it has read the chain, or
    /// given it back.</param>
    /// <returns>False for a removal refused; true otherwise, a page no lock is taken on included (no registered user, a
    /// read-only writer, a write or a referenced key outside every transaction).</returns>
    /// <remarks>
    /// <para>A long-value lock is held for the statement — the outermost scope — inside an explicit transaction too:
    /// ACE lets go of the read byte after the statement there as outside one, and of the writer's lock within 1.5 s of
    /// the statement, where the data page it wrote is held to <c>COMMIT</c> (measured). Outside every statement — a Core
    /// API read or write — it is held for that read or write alone, never to the end of the transaction: another
    /// connection reading the value meanwhile reads the committed chain, as it does beside ACE's transaction once ACE has
    /// let go.</para>
    /// <para>A referenced key's byte is the transaction's, held as the pages it writes are — to the end of an explicit
    /// transaction, to the statement's for one on its own — as ACE holds it with the statement's page locks, to
    /// <c>COMMIT</c> or <c>ROLLBACK</c> (measured): another user's <c>DELETE</c> of any parent row on that page, which
    /// writes it, is refused meanwhile, and so is its <c>INSERT</c> of a parent row there. Taken once per page in a
    /// transaction; a savepoint rollback does not let it go (unmeasured).</para>
    /// <para>One newly taken after another user's slot has moved since the scope looked throws
    /// <see cref="StaleReadException"/>, with the lock let go, as <see cref="LockTable"/> does: what the statement, or
    /// the Core API's writer, read may be older than the file. A read lock newly taken outside every scope drops the
    /// cache if another user has committed, so the chain is read as the file has it.</para>
    /// <para>Only a registered user; a read-only one takes the read lock, as ACE's read-only user takes it too.</para>
    /// </remarks>
    internal bool LockPage(int page, JetLockPurpose purpose, out bool alone)
    {
        alone = false;
        bool exclusive = purpose is JetLockPurpose.LongValueReplace or JetLockPurpose.LongValueRemove;
        bool referenced = purpose == JetLockPurpose.ReferencedKey;
        if (_user is not { } user || page <= 0 || page >= PageCount || exclusive && _readOnly) return true;
        bool statement = _scopeDepth > 0;
        if (referenced ? _active is null || _transactionLocks.Contains((JetLockRegion.WriteRead, page, false))
            : !statement && exclusive && _active is null)
            return true;   // a write or a referenced key outside every transaction, or a key's page held already

        bool? took = user.Lock(JetLockRegion.WriteRead, page, exclusive, purpose);
        if (took is null) return false;
        if (took.Value && _cache.RefreshIfOthersCommitted() && (statement || exclusive || referenced))
        {
            user.Unlock(JetLockRegion.WriteRead, page, exclusive);
            throw new StaleReadException(exclusive
                ? JetUserRegistration.CouldNotSaveMessage
                : JetUserRegistration.CouldNotReadMessage);
        }

        if (referenced) _transactionLocks.Add((JetLockRegion.WriteRead, page, false));
        else if (statement) _statementLocks.Add((JetLockRegion.WriteRead, page, exclusive));
        else alone = true;
        return true;
    }

    /// <summary>Lets go of the locks in <paramref name="held"/> from position <paramref name="from"/> on, the
    /// newest first.</summary>
    private void UnlockHeld(List<(JetLockRegion Region, int Page, bool Exclusive)> held, int from)
    {
        for (int i = held.Count - 1; i >= from; i--)
        {
            (JetLockRegion region, int page, bool exclusive) = held[i];
            held.RemoveAt(i);
            _user?.Unlock(region, page, exclusive);
        }
    }

    /// <summary>
    /// Publishes one batch of page writes — a transaction's commit, or a write outside one — as ACE does
    /// (docs/format/locking.md §6), under the publish gate the caller holds. Pages appended to the file go first;
    /// then, when two or more existing pages follow, this channel's commit slot is set to <c>00 00</c>; then the
    /// existing pages; and the batch ends with the slot set to its counter plus one. A channel with no slot writes
    /// the pages alone, in the same order.
    /// </summary>
    /// <remarks>
    /// <para>If a write fails part-way, the pages already published are put back from <paramref name="baselineOf"/>
    /// (an appended page by truncating it again), and the slot is put back to the value it had, since nothing the
    /// batch wrote remains — the caller's transaction is still open, to retry or roll back. If a page cannot be put
    /// back, the slot is left as the batch left it — <c>00 00</c> for a batch of two or more existing pages, which
    /// says, truthfully, that the file is mid-write — and the failure says the file is left mid-publish.</para>
    /// <para>Nothing here waits for another user: a registered user already holds the write lock on every existing
    /// page the batch writes (<see cref="LockForWrite"/>), and its caller releases them after the closing slot
    /// write.</para>
    /// </remarks>
    private void PublishBatch(int[] pages, Func<int, byte[]> imageOf, Func<int, byte[]?> baselineOf,
        Func<int, object?> parsedOf)
    {
        // A page left as it was committed is not written: a batch that changes no page is no batch, and leaves the slot
        // alone — as a failed statement's rollback that undoes everything leaves ACE's (locking.md §6).
        pages = [.. pages.Where(p => baselineOf(p) is not { } baseline || !SamePage(p, baseline, imageOf(p)))];
        // Appended pages first, each group ascending so the file grows monotonically. A page is appended when it had no
        // committed image to derive from: the file has since been measured again, and another user may have grown it
        // past a page this batch appends.
        int[] ordered = [.. pages.Where(p => baselineOf(p) is null).Order(), .. pages.Where(p => baselineOf(p) is not null).Order()];
        int existing = pages.Count(p => baselineOf(p) is not null);

        ushort? counter = _commitSlot is null
            ? null
            : BinaryPrimitives.ReadUInt16LittleEndian(ReadCommittedPageOrNull(0).AsSpan(_commitSlot.Value));
        bool slotWritten = false;
        var published = new List<int>(ordered.Length);
        try
        {
            foreach (int page in ordered)
            {
                bool appended = baselineOf(page) is null;
                if (counter is not null && existing >= 2 && !appended && !slotWritten)
                {
                    slotWritten = true;
                    WriteCommitSlot(0);
                }
                byte[] image = imageOf(page);
                // A page appended only to keep the file contiguous — zeros, below a page this user took — may be one
                // another user (or another channel) has reserved and since written: never written over it. A zero page
                // of this user's own that now lies inside the file holds zeros there already, since no other user
                // writes a page this one took. So an appended zero page the file already reaches is not written
                // (locking.md §4).
                if (appended && IsZeroPage(image) && FileLengthOnDisk() >= (long)(page + 1) * PageSize)
                    continue;
                WriteThroughUnderPublishLock(page, image);
                // The image is now the committed one, so a parse of it stays true: handed on, the next statement's
                // read of the page does not decode it again — each SQL statement is a transaction of its own, and
                // consecutive inserts write the same index leaves.
                if (parsedOf(page) is { } parsed) _cache.SetParsed(page, parsed);
                published.Add(page);
            }

            if (counter is { } previous && ordered.Length > 0)
            {
                slotWritten = true;
                WriteCommitSlot(NextCommitCount(previous));
            }
        }
        catch (Exception publishFailure)
        {
            // The restore writes to the same file that just failed to accept a write, so they can fail
            // too — and if one does, the original cause must not be swallowed by the cleanup's exception.
            // Collect both: the caller needs the publish failure to know why the commit failed, and the
            // restore failure to know the file was left mid-publish rather than rolled back.
            List<Exception>? restoreFailures = null;
            for (int i = published.Count - 1; i >= 0; i--)
            {
                int page = published[i];
                try
                {
                    if (baselineOf(page) is { } baseline)
                    {
                        WriteThroughUnderPublishLock(page, baseline);
                    }
                    else
                    {
                        // A null baseline is a tail page the batch appended. Validation proved no other writer
                        // had claimed it, and the publish gate excludes one while we truncate it again — in this
                        // process. Another user may have grown the file past it since: then it stays, unreferenced,
                        // rather than cut that user's pages off.
                        if (FileLengthOnDisk() <= (long)(page + 1) * PageSize)
                        {
                            _stream.SetLength((long)page * PageSize);
                            _cache.SetFileLength((long)page * PageSize);
                        }
                        _cache.Remove(page);
                    }
                }
                catch (Exception restoreFailure)
                {
                    (restoreFailures ??= []).Add(
                        new IOException($"Could not restore page {page} after a failed commit publication.", restoreFailure));
                }
            }

            if (restoreFailures is null && slotWritten)
            {
                try { WriteCommitSlot(counter!.Value); }
                catch (Exception restoreFailure)
                {
                    restoreFailures = [new IOException("Could not restore the commit slot after a failed commit publication.", restoreFailure)];
                }
            }

            if (restoreFailures is null) throw;
            throw new AggregateException(
                "A commit publication failed and the already-published pages could not all be restored; " +
                "the file is left mid-publish.", [publishFailure, .. restoreFailures]);
        }
    }

    /// <summary>The file's length as it is now: for a registered user measured, since another user appends without
    /// telling this process until its slot is next read (locking.md §4); otherwise the length this process's channels
    /// keep, which only they change.</summary>
    private long FileLengthOnDisk() => _user is null
        ? _cache.FileLength
        : Math.Max(_cache.FileLength, RandomAccess.GetLength(_stream.SafeFileHandle));

    private bool IsZeroPage(ReadOnlySpan<byte> page) => page[..PageSize].IndexOfAnyExcept((byte)0) < 0;

    /// <summary>The counter a batch stores after <paramref name="previous"/>: one more, little-endian 16-bit
    /// (page-00 §2.2). What ACE stores after <c>FF FF</c> is not measured; the count wraps to <c>00 01</c>, the value
    /// a fresh file's slot starts at, rather than into <c>00 00</c> or <c>01 00</c>, which mean mid-write and a
    /// corrupted page. A slot left at either of those by a user that died is counted on from the same start.</summary>
    private static ushort NextCommitCount(ushort previous) =>
        previous is < 2 or ushort.MaxValue ? (ushort)0x0100 : (ushort)(previous + 1);

    /// <summary>Writes <paramref name="value"/> to this channel's commit slot: a 2-byte write at its offset in the
    /// file, handed to the OS at once as ACE's is, never by writing page 0 — every other user's slot is theirs —
    /// and the same bytes into the cached image of page 0, so readers in this process see what the file holds.</summary>
    private void WriteCommitSlot(ushort value)
    {
        int offset = _commitSlot!.Value;
        Span<byte> bytes = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        _stream.Seek(offset, SeekOrigin.Begin);
        _stream.Write(bytes);
        _stream.Flush(flushToDisk: false);
        // A slot of this process's own: nothing for its channels, which share the cache, to refresh for.
        _cache.OwnSlotWritten(offset, bytes);

        // Copy-on-write, as every cached image is replaced rather than written into; not resident, the next read
        // takes it from the file, which already has it.
        if (_cache.TryGetArray(0, out byte[] cached))
        {
            byte[] page0 = cached.AsSpan(0, PageSize).ToArray();
            bytes.CopyTo(page0.AsSpan(offset));
            _cache.Adopt(0, page0);
        }
    }

    /// <summary>Runs a logical read against one committed page-set generation. Shared: other readers on this
    /// file run concurrently; only a publication excludes them. The outermost scope is a statement: the table locks
    /// it took for itself go when it ends, unless its result takes them over (<see cref="LockTable"/>,
    /// <see cref="KeepStatementTableLocks"/>).</summary>
    internal T ReadConsistent<T>(Func<T> action)
    {
        BeginScope();
        try { return _cache.ReadConsistent(action); }
        finally { EndScope(); }
    }

    /// <summary>Runs a logical write with every other reader and writer on this file excluded, so its pages
    /// publish as one unit. A statement, as <see cref="ReadConsistent{T}"/> is: one that succeeds inside a transaction
    /// still open when it returns publishes what it took of the global maps, and the AutoNumbers it drew, before it
    /// lets the gate go — one that fails there, once rolled back, the AutoNumbers (<see cref="PublishAheadOfCommit"/>).</summary>
    internal T WriteExclusive<T>(Func<T> action)
    {
        BeginScope();
        try
        {
            return _cache.PublishLocked(() =>
            {
                T result;
                try { result = action(); }
                catch when (_scopeDepth == 1)
                {
                    PublishAheadOfCommit(failed: true);
                    throw;
                }
                if (_scopeDepth == 1) PublishAheadOfCommit(failed: false);
                return result;
            });
        }
        finally { EndScope(); }
    }

    /// <summary>Publishes, ahead of the open transaction's COMMIT, the pages it has taken out of the global free-pages
    /// map and the AutoNumbers it has drawn, and lets go of the maps' pages and of the tables' definition pages it has
    /// only counted on, as ACE writes page 1 and the TDEF page (its AutoNumber only) and lets them go about 500 ms
    /// after an INSERT inside a transaction (locking.md §5): another user can then reserve a run of its own and draw
    /// the next AutoNumber while the transaction is open, and none of them can take a page or an id this one took.
    /// Called at the end of each statement in the transaction, which is sooner than ACE's deferral, as LibRed's
    /// autocommit write already is; for one that <paramref name="failed"/>, once its savepoint is rolled back, only the
    /// definition pages, as ACE writes a failed INSERT's AutoNumber early and lets the page go (measured).</summary>
    /// <remarks>
    /// <para>Only a registered user's transaction. The maps only after a statement that succeeded, and only when what it
    /// changed of them is pages taken — bits cleared, in place (<see cref="PageAllocator.TakenOnly"/>): a page it freed
    /// is still used by the committed rows until it commits, and a map it grew or converted cannot be split from the rest
    /// of its change. A definition page
    /// only when what it changed of it is counters (<see cref="CountersAheadOfCommit"/>). Otherwise the pages stay in
    /// the overlay, locked, and publish at COMMIT as before. Nor when another channel of this process has committed one
    /// of them since this transaction locked it — the commit-time conflict check is left to refuse that — or a rollback
    /// to an open savepoint would put an image of one back.</para>
    /// <para>What is published goes as one batch (<see cref="PublishBatch"/>), which moves this user's commit slot — a
    /// batch with no page changed writes nothing, a definition page the statement only counted on included — and the
    /// pages' locks go. The maps' pages leave the overlay: the next change to the maps in the transaction locks page 1
    /// again and reads it as the file has it then. A definition page stays in it with the transaction's counting on it,
    /// which goes onto the page as the file has it at the transaction's next lock of it, or at its COMMIT
    /// (<see cref="LockForUpdate"/>). A rollback gives back the pages its undone writes had taken
    /// (<see cref="PageAllocator.RollBackTo"/>), and keeps the AutoNumbers, which are the file's now.</para>
    /// </remarks>
    private void PublishAheadOfCommit(bool failed)
    {
        if (_active is not { } transaction || _user is not { } user || _readOnly) return;
        var images = new Dictionary<int, byte[]>();

        int[] maps = failed ? [] : [.. Allocator.GlobalMapPages(page => _commitBaselines.GetValueOrDefault(page) is not null)
            .Where(page => _commitBaselines.GetValueOrDefault(page) is not null)];
        var taken = new List<int>();
        foreach (int page in maps)
        {
            if (!_overlay.TryGetValue(page, out byte[]? image)) continue;   // locked to be read, left as it was
            byte[] baseline = _commitBaselines[page]!;
            if (transaction.RestoresImageOf(page) || !SamePage(page, baseline, ReadCommittedPageOrNull(page))
                || Allocator.TakenOnly(page, baseline, image) is not { } pages)
            {
                images.Clear();
                taken.Clear();
                maps = [];
                break;
            }
            images[page] = image;
            taken.AddRange(pages);
        }

        var definitions = new List<int>();
        foreach (int page in CountedPages())
        {
            if (CountersAheadOfCommit(transaction, page) is not { } image) continue;
            images[page] = image;
            definitions.Add(page);
        }
        if (maps.Length == 0 && definitions.Count == 0) return;

        PublishBatch([.. images.Keys], page => images[page], page => _commitBaselines[page], _ => null);
        foreach (int page in maps)
        {
            _overlay.Remove(page);
            _overlayParsed.Remove(page);
            _commitBaselines.Remove(page);
            user.Unlock(JetLockRegion.WriteRead, page, exclusive: true);
        }
        Allocator.TakenEarly(taken);
        foreach (int page in definitions)
        {
            _commitBaselines.Remove(page);
            user.Unlock(JetLockRegion.WriteRead, page, exclusive: true);
            _publishedEarly[page] = images[page];
            // The AutoNumbers drawn are the file's now: kept through a rollback, as each is, with nothing left of them
            // for the rollback to write (RestoreKeptCounters).
            foreach (((int counted, int offset), (_, int value, bool failedOnly)) in _keptCounters.ToArray())
                if (counted == page && !failedOnly) _keptCounters[(counted, offset)] = (value, value, false);
        }
    }

    /// <summary>The pages the open transaction has written counters on (<see cref="KeepThroughRollback"/>) — its
    /// tables' definition pages.</summary>
    private int[] CountedPages() => [.. _keptCounters.Keys.Concat(_pendingCounters.Keys).Select(k => k.Page).Distinct()];

    /// <summary>What the open transaction publishes of definition page <paramref name="page"/> ahead of its COMMIT: the
    /// page as it locked it with the AutoNumbers it has drawn there — the counters kept through every rollback — and
    /// nothing else, as ACE's early write carries only the AutoNumber, its row and index counts written at COMMIT
    /// (locking.md §5). Null when the page is not one to let go: not locked by the transaction, or changed by it in
    /// more than counters (a design change), or carrying another counter kept through every rollback (a catalog
    /// table's counts), or changed in the file since it was locked, or one a rollback to an open savepoint would put
    /// an image of back.</summary>
    private byte[]? CountersAheadOfCommit(Transaction transaction, int page)
    {
        if (_commitBaselines.GetValueOrDefault(page) is not { } baseline || !_overlay.TryGetValue(page, out byte[]? image)
            || transaction.RestoresImageOf(page) || !SamePage(page, baseline, ReadCommittedPageOrNull(page)))
            return null;

        byte[] counted = (byte[])baseline.Clone();
        foreach (int offset in CounterOffsets(page))
            image.AsSpan(offset, sizeof(int)).CopyTo(counted.AsSpan(offset));
        if (!SamePage(page, counted, image)) return null;

        byte[] published = (byte[])baseline.Clone();
        foreach (((int counter, int offset), (_, _, bool failedOnly)) in _keptCounters)
        {
            if (counter != page || failedOnly) continue;
            if (offset != _format.TdefLastAutoNumberOffset && offset != _format.TdefComplexAutoNumberOffset) return null;
            image.AsSpan(offset, sizeof(int)).CopyTo(published.AsSpan(offset));
        }
        return published;
    }

    // The outermost scope is a statement: it lets go of the results before it that were never read
    // (KeepStatementTableLocks).
    private void BeginScope()
    {
        if (_scopeDepth++ == 0) ReleaseUnreadResults();
    }

    private void EndScope()
    {
        if (--_scopeDepth == 0 && _statementLocks.Count > 0) UnlockHeld(_statementLocks, 0);
    }

    /// <remarks><paramref name="source"/> is a whole page the cache takes over as it stands — this channel's own
    /// copy of a write, an overlay page, or a committed baseline — so it must be one nothing writes into again.</remarks>
    private void WriteThroughUnderPublishLock(int pageNumber, byte[] source)
    {
        _locks?.EnterExclusive(pageNumber);
        try
        {
            // The cache holds plaintext and the disk holds ciphertext (for an encrypted file), so encrypt a copy
            // on the way to disk — the mirror of ReadPage's decrypt — while caching the plaintext. Page 0 is a
            // no-op inside the codec (never page-encrypted).
            ReadOnlySpan<byte> toDisk = source.AsSpan(0, PageSize);
            byte[]? encrypted = null;
            if (_codec is not null)
            {
                encrypted = source.AsSpan(0, PageSize).ToArray();
                _codec.EncryptPage(pageNumber, encrypted);
                toDisk = encrypted;
            }

            // Page 0 ends in the commit-slot table, and every slot but this user's own belongs to another user, who
            // writes it with no lock (locking.md §6). So page 0 is written up to the table and no further, and its
            // cached image takes the table from the file — every other user's slot as it is now, not as this
            // image last saw it. Our own slot is the batch's to write (PublishBatch).
            int table = pageNumber == 0 ? _format.CommitByteTableOffset : PageSize;
            if (table < PageSize) toDisk = toDisk[..table];

            long offset = (long)pageNumber * PageSize;
            long length = _cache.FileLength;
            // Another user may have grown the file since this one last measured it (it appends into runs it reserved
            // in the global map, locking.md §4): growing it is measured against the file itself, so it never cuts
            // the file back below that user's pages.
            if (offset + PageSize > length) length = FileLengthOnDisk();
            if (offset > length)
                _stream.SetLength(offset); // zero-fills the gap up to this page
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.Write(toDisk);
            if (offset + PageSize > _cache.FileLength)
                _cache.SetFileLength(Math.Max(length, offset + PageSize));

            if (table < PageSize)
            {
                // The partial write is shorter than the stream's buffer: hand it to the OS now, as a whole page's
                // write would have gone, then take the table back as the file has it. The whole page is read —
                // a read as long as the stream's buffer goes straight to the file, where a short one would leave
                // the start of page 1 sitting in that buffer — and only the table is kept from it.
                _stream.Flush(flushToDisk: false);
                byte[] merged = new byte[PageSize];
                _stream.Seek(0, SeekOrigin.Begin);
                _stream.ReadExactly(merged);
                source.AsSpan(0, table).CopyTo(merged);
                source = merged;
            }

            // Write through: the pool now holds the just-written (plaintext) image, so a subsequent read (this
            // channel or any other on the file) sees it without touching disk.
            _cache.Adopt(pageNumber, source);
            _published = true;
        }
        finally { _locks?.ExitExclusive(pageNumber); }
    }

    /// <summary>
    /// Begins a page-level transaction. Subsequent <see cref="WritePage(int, ReadOnlySpan{byte})"/> calls snapshot the
    /// original bytes of each page they touch, so <see cref="RollbackTransaction"/> can undo them.
    /// Reads continue to see writes made within the transaction (read-your-writes). Nesting is not
    /// supported.
    /// </summary>
    public Transaction BeginTransaction()
    {
        // Allowed read-only, as ACE allows it: nothing can be written into it, so it commits and rolls back nothing.
        if (_active is not null)
            throw new InvalidOperationException("A transaction is already in progress.");
        ClearTransactionState();
        _schemaDirty = false;
        _txPageCount = PageCount; // committed count at start (PageCount is still file-based while _active is null)
        return _active = new Transaction(_txPageCount, Allocator.State);
    }

    /// <summary>Commits the current transaction: publishes every buffered overlay page to disk and the shared
    /// cache — making the writes visible to other channels for the first time — as one batch
    /// (<see cref="PublishBatch"/>). No-op if no transaction is open. <paramref name="flush"/> hands the stream's
    /// buffer to the OS before returning — used by an explicit user commit, which ACE writes synchronously; an
    /// implicit per-statement autocommit passes false. Neither forces the OS cache to disk: ACE never calls
    /// FlushFileBuffers, on commit or on close, so LibRed doesn't either.</summary>
    public void CommitTransaction(bool flush = true)
    {
        if (_active is null) return;
        Allocator.ReturnGroups();
        ReclaimRetiredRecords();
        PublishTransaction();
        Allocator.TakenEarly(null);   // the pages it took early are the committed file's now
        _keptCounters.Clear();   // the counters it kept are on its TDEF pages
        _pendingCounters.Clear();
        if (flush) _stream.Flush(flushToDisk: false);
    }

    /// <summary>Publishes the open transaction's pages as one batch (<see cref="PublishBatch"/>), once they are checked
    /// against what other channels and users have committed since, and ends it: its page and table locks go. Left open
    /// if the check or the publication fails, for the caller to roll back.</summary>
    private void PublishTransaction()
    {
        int[] pages = _overlay.Keys.ToArray();
        Array.Sort(pages);
        _cache.PublishLocked(() =>
        {
            // A definition page it let go of after publishing its AutoNumbers is locked again, briefly, and its counting
            // goes onto the page as the file has it now, as ACE's COMMIT does (locking.md §5): an AutoNumber another
            // user drew meanwhile, and a row another user counted, stay.
            foreach (int page in _publishedEarly.Keys.ToArray()) LockForUpdate(page, refuseStale: false);
            foreach (int page in pages)
            {
                byte[]? current = ReadCommittedPageOrNull(page);
                byte[]? baseline = _commitBaselines[page];
                // A page this transaction appended that the file now reaches — another user or channel grew it past
                // the page — conflicts only where both wrote it: a zero page on either side is one that only keeps the
                // file contiguous, and the publication leaves the other's (PublishBatch).
                if (!SamePage(page, baseline, current)
                    && !(baseline is null && current is not null && (IsZeroPage(current) || IsZeroPage(_overlay[page]))))
                    throw new InvalidOperationException(
                        $"Transaction write conflict on page {page}: another connection committed a change to this page.");
            }

            // And what the transaction merely READ and relied on. Evaluated here, inside the publication lock,
            // so nothing can change between the check and the publish it guards.
            foreach ((Func<bool> stillHolds, string violation, _) in _dependencies)
                if (!stillHolds())
                    throw new InvalidOperationException(violation);

            // Keep the transaction open until every page has published: a failure part-way puts the published
            // ones back from their validated committed baselines, so the caller can still roll back.
            PublishBatch(pages, page => _overlay[page], page => _commitBaselines[page],
                page => _overlayParsed.GetValueOrDefault(page));

            // The batch has ended with its slot write: its page locks go now, as ACE's do (locking.md §6).
            int[] locked = LockedPages();
            _active = null;
            _released.AddRange(_releasing);   // the pages it freed are now the session's to hold until close
            ClearTransactionState();
            Unlock(locked);
            UnlockHeld(_transactionLocks, 0);   // and its table and referenced-key locks, as ACE's go at COMMIT (locking.md §5)
            if (_schemaDirty) _cache.MarkSchemaChanged();
            _schemaDirty = false;
        });
    }

    /// <summary>
    /// Rolls the current transaction back by discarding its overlay. Nothing the transaction wrote ever reached
    /// disk or the shared cache, so there is nothing to restore, truncate or flush. No-op if none is open.
    /// <paramref name="failed"/> says a statement failed, rather than a ROLLBACK asked for it (see
    /// <see cref="KeepThroughRollback"/>).
    /// </summary>
    public void RollbackTransaction(bool failed = false)
    {
        if (_active is null) return;
        Storage.PageAllocator.AllocationState? allocation = _active.AllocationAt(new Savepoint(0));
        if (allocation is not null) Allocator.LockGlobalMapsFor(allocation);
        // Its page locks go last: the kept counters below are written back onto TDEF pages it may hold, and a page
        // still held is written without waiting for another user.
        int[] locked = LockedPages();
        ClearTransactionState();   // including the staged releases: a rolled-back free frees nothing
        _schemaDirty = false;
        _active = null;
        try
        {
            ResyncFormatVersion();   // a discarded format raise must not stay raised in memory
            // What the rollback writes is gathered into two transactions of its own, each published as one batch, a page
            // it leaves as committed not at all: first the AutoNumbers drawn, as ACE writes them at once, then the global
            // maps put right and the counting a failure keeps, as ACE's rollback writes them (locking.md §6).
            foreach (bool failedOnly in (ReadOnlySpan<bool>)[false, true])
            {
                BeginTransaction();
                try
                {
                    if (failedOnly && allocation is not null) Allocator.RollBackTo(allocation);
                    RestoreKeptCounters(failed, onCommitted: true, failedOnly);
                    PublishTransaction();
                }
                catch
                {
                    Unlock(LockedPages());
                    ClearTransactionState();
                    _active = null;
                    throw;
                }
            }
            _keptCounters.Clear();
        }
        finally
        {
            Unlock(locked);
            UnlockHeld(_transactionLocks, 0);
        }
    }

    /// <summary>Notes <paramref name="value"/> as the counter at <paramref name="offset"/> of TDEF page
    /// <paramref name="page"/> that the open transaction just wrote, for a rollback to keep — with
    /// <paramref name="failedOnly"/>, only the rollback of a statement that failed, and only once
    /// <see cref="AcceptCounters"/> has accepted it.</summary>
    internal void KeepThroughRollback(int page, int offset, int value, bool failedOnly = false)
    {
        if (_active is null) return;
        int? baseline = _keptCounters.TryGetValue((page, offset), out var kept) ? kept.Baseline
            : _pendingCounters.TryGetValue((page, offset), out var pending) ? pending.Baseline
            : ReadCommittedPageOrNull(page) is { } committed && PageHeader.ReadType(committed) == PageType.TableDefinition
                ? BinaryPrimitives.ReadInt32LittleEndian(committed.AsSpan(offset))
                : null;
        if (failedOnly) _pendingCounters[(page, offset)] = (baseline, value);
        else _keptCounters[(page, offset)] = (baseline, value, false);
    }

    /// <summary>Takes back usage-map record <paramref name="row"/> of page <paramref name="page"/>, flagged deleted
    /// with its bytes still in place: inside a statement of a user transaction — run under a savepoint — when the
    /// transaction commits, as ACE takes back a retired map record's bytes then (see
    /// <c>SchemaEditor.RetireMapRecords</c>); otherwise now, as ACE does for a statement on its own (measured: a
    /// DROP TABLE's records each slide as they go).</summary>
    internal void ReclaimAtCommit(int page, int row)
    {
        _reclaimAtCommit.Add((page, row));
        if (_active is null || _active.Depth == 1) ReclaimRetiredRecords();
    }

    /// <summary>Takes back each retired map record still flagged — a savepoint rollback may have put one back.</summary>
    private void ReclaimRetiredRecords()
    {
        foreach ((int page, int row) in _reclaimAtCommit)
        {
            byte[] bytes = ReadPage(page).Span.ToArray();
            if (DataPage.ReadSlot(bytes, Format, row).Flags != RowSlotFlags.Deleted) continue;
            DataPage.ReclaimRow(bytes, Format, row);
            WritePage(page, bytes);
        }
        _reclaimAtCommit.Clear();
    }

    /// <summary>Accepts the counters written since the last acceptance: a row the statement has finished with, or
    /// the counting ACE does for a row it refuses at an index, which a failure of the statement later on then leaves
    /// standing — where a row refused before that, by a CHECK say, has its counting taken back (measured).</summary>
    internal void AcceptCounters()
    {
        foreach (((int page, int offset), (int? baseline, int value)) in _pendingCounters)
            _keptCounters[(page, offset)] = (baseline, value, true);
        _pendingCounters.Clear();
    }

    /// <summary>Writes each kept counter back onto its TDEF — where that page still is one, the table not having
    /// been created by the writes just undone. Into the enclosing transaction after a savepoint rollback, as the
    /// value written where the overlay still holds the page; with <paramref name="onCommitted"/>, after the
    /// transaction's, and onto a page the overlay no longer holds, as the change written on top of what is committed
    /// now. A rollback no statement's failure caused takes back what only a failure keeps, which no
    /// later rollback then writes back either. With <paramref name="only"/>, only the counters only a failure keeps
    /// (true), or only those every rollback keeps (false).</summary>
    private void RestoreKeptCounters(bool failed, bool onCommitted, bool? only = null)
    {
        _pendingCounters.Clear();
        foreach (((int page, int offset), (int? baseline, int value, bool failedOnly)) in _keptCounters.ToArray())
        {
            if (failedOnly && !failed)
            {
                _keptCounters.Remove((page, offset));
                continue;
            }
            if (only is { } kind && failedOnly != kind) continue;
            // A page the overlay no longer holds is the committed page — which another user may have moved on since an
            // early publication let it go — and takes the change, as after the transaction's rollback.
            bool ontoCommitted = onCommitted || !_overlay.ContainsKey(page);
            if (page >= PageCount || ontoCommitted && baseline is null) continue;
            byte[] tdef = ReadPage(page).Span.ToArray();
            int current = BinaryPrimitives.ReadInt32LittleEndian(tdef.AsSpan(offset));
            int kept = ontoCommitted ? unchecked(current + value - baseline!.Value) : value;
            if (PageHeader.ReadType(tdef) != PageType.TableDefinition || current == kept) continue;
            BinaryPrimitives.WriteInt32LittleEndian(tdef.AsSpan(offset), kept);
            WritePage(page, tdef);
        }
    }

    /// <summary>Drops everything the open transaction accumulated — its unpublished pages, the images they
    /// were derived from, the conditions its writes depend on, and the page releases it staged. A commit has
    /// already moved the releases into the session's committed list before calling this; a rollback has not,
    /// which is the whole difference between them.</summary>
    private void ClearTransactionState()
    {
        _reclaimAtCommit.Clear();
        _overlay.Clear();
        _overlayParsed.Clear();
        _commitBaselines.Clear();
        _publishedEarly.Clear();
        _dependencies.Clear();
        _dependencyKeys.Clear();
        _releasing.Clear();
    }

    /// <summary>
    /// Raises the file's format version byte (page 0, <c>0x14</c>) to <paramref name="version"/>, in place,
    /// clears the minor byte at <c>0x15</c> as ACE's own raise does — to <c>0x00</c> whatever the target, even
    /// <c>0x03</c>, whose created files carry <c>0x01</c> — and swaps <see cref="Format"/> to match. Returns false — writing nothing — when the file already meets it,
    /// so callers can call this unconditionally.
    /// </summary>
    /// <remarks>
    /// The write goes through <see cref="WritePage(int, ReadOnlySpan{byte})"/> rather than to the stream, so it joins the calling
    /// statement's transaction overlay: the upgrade commits with the DDL that needed it, or is discarded with
    /// it. Page 0 is never page-encrypted, so the write is byte-transparent even on an encrypted file.
    /// Only those two bytes move. The ACE format classes above 0x02 override nothing but
    /// <see cref="JetFormatBase.Version"/> — same page size, same offsets — so the swap changes what the
    /// database reports about itself and nothing about how it is parsed.
    /// </remarks>
    internal bool RaiseFormatVersion(byte version)
    {
        byte[] page0 = ReadPage(0).Span.ToArray();
        if (JetFormatBase.ReadVersionByte(page0) >= version) return false;

        // A Jet MDB carries the "Standard Jet DB" identifier, which JetFormatBase.Detect pairs with version
        // 0x00/0x01 only. Raising one to an ACE version writes a file nothing can reopen — not LibRed, not
        // Access. JetDatabase refuses to create that same pair; refuse to upgrade into it too.
        if (!Format.IsAccdb)
            throw new NotSupportedException(
                $"Cannot raise this database to version 0x{version:X2}: it is a Jet MDB " +
                $"(\"{JetFormatBase.JetIdentifier}\"), and only an ACCDB can take that version. " +
                "The statement needs a data type this format cannot store.");

        Format.WriteVersion(page0, version, minor: 0x00);
        WritePage(0, page0);
        _format = JetFormatBase.FromVersionByte(version);
        return true;
    }

    /// <summary>Re-derives <see cref="Format"/> from the version byte now visible on page 0 — after a rollback
    /// that may have discarded a raise, and when another handle's schema change becomes visible, since a raise
    /// is something ANOTHER connection can do to this file and this one would otherwise go on reporting the
    /// version the file had when it opened. Only on those paths, so it costs nothing in the ordinary case, and
    /// page 0 is cached by then.</summary>
    internal void ResyncFormatVersion()
    {
        long generation = _cache.SchemaGeneration;
        byte onDisk = JetFormatBase.ReadVersionByte(ReadPageShared(0).Span);
        _seenSchemaGeneration = generation;
        if ((byte)_format.Version == onDisk) return;
        try
        {
            _format = JetFormatBase.FromVersionByte(onDisk);
        }
        catch (NotSupportedException)
        {
            // A version byte this build does not know is not a reason to fail a rollback. The open already
            // decided how to read the file — JetFormatBase.Detect reads an unknown ACE byte with the latest
            // known layout — and re-deriving it strictly here disagrees with that decision, so every rollback
            // on such a file threw although the file had opened and read perfectly well. The format the open
            // chose stands.
        }
    }

    /// <summary>Opens a savepoint in the current transaction; pass the handle to
    /// <see cref="RollbackToSavepoint"/> or <see cref="ReleaseSavepoint"/>.</summary>
    public Savepoint CreateSavepoint()
    {
        if (_active is null)
            throw new InvalidOperationException("No transaction is in progress.");
        return _active.Save(_txPageCount, _releasing.Count, _dependencies.Count, Allocator.State, _transactionLocks.Count);
    }

    /// <summary>Rolls the transaction back to <paramref name="savepoint"/>: undoes every write made since it was
    /// created and drops pages allocated after it, leaving the savepoint (and the transaction) open.
    /// <paramref name="failed"/> as for <see cref="RollbackTransaction"/>.</summary>
    public void RollbackToSavepoint(Savepoint savepoint, bool failed = false)
    {
        if (_active is null)
            throw new InvalidOperationException("No transaction is in progress.");
        int releaseCount = _active.ReleaseCountAt(savepoint);
        int dependencyCount = _active.DependencyCountAt(savepoint);
        Storage.PageAllocator.AllocationState? allocation = _active.AllocationAt(savepoint);
        var (before, pageCount) = _active.TakeForRollbackTo(savepoint);
        int[] unlocked = RestoreOverlay(before, pageCount);
        try
        {
            // Locked once the overlay is restored, which lets go of every page the frame first locked: the maps' pages
            // among them when the transaction published them early since (PublishAheadOfCommit).
            if (allocation is not null)
            {
                Allocator.LockGlobalMapsFor(allocation);
                Allocator.RollBackTo(allocation);
            }
            // Before the pages first written after the savepoint are let go: a kept counter goes back onto a TDEF
            // page among them, which is then written again without waiting for another user.
            RestoreKeptCounters(failed, onCommitted: false);
        }
        finally
        {
            // The page locks the frame first took go with its writes. Its table locks stay to the end of the
            // transaction: a table a failed statement wrote, or one rolled back in a nested transaction, refuses
            // another connection's ALTER TABLE until ACE's outer transaction ends (measured).
            Unlock(unlocked);
        }
        _releasing.RemoveRange(releaseCount, _releasing.Count - releaseCount);
        // The writes that needed them are undone, so the conditions are no longer anything to hold the commit to —
        // and a key they held is free to be registered again by a write made after the rollback.
        _dependencies.RemoveRange(dependencyCount, _dependencies.Count - dependencyCount);
        _dependencyKeys.Clear();
        foreach ((_, _, object? key) in _dependencies)
            if (key is not null) _dependencyKeys.Add(key);
    }

    /// <summary>Releases <paramref name="savepoint"/>, merging its changes into the enclosing scope. Only the
    /// innermost open savepoint may be released.</summary>
    public void ReleaseSavepoint(Savepoint savepoint)
    {
        if (_active is null)
            throw new InvalidOperationException("No transaction is in progress.");
        _active.Release(savepoint);
    }

    /// <summary>Restores the overlay to a savepoint: each before-image is re-applied newest-first so the oldest
    /// snapshot wins for a page touched in several frames — a <c>null</c> image means the page was absent at the
    /// savepoint, so drop it. The logical page count is reset, dropping overlay pages the rolled-back frames
    /// allocated. Nothing touches disk or the shared cache; the overlay never left this channel. Returns the pages
    /// whose write locks the dropped baselines held — those first written after the savepoint — for the caller to let
    /// go of (see <see cref="LockForWrite"/>).</summary>
    private int[] RestoreOverlay(List<KeyValuePair<int, byte[]?>> before, int pageCount)
    {
        foreach (var (page, image) in before)
        {
            if (image is null) _overlay.Remove(page);
            else _overlay[page] = image;
            _overlayParsed.Remove(page);
        }
        _txPageCount = pageCount;
        var unlocked = new List<int>();
        foreach (int page in _commitBaselines.Keys.Where(p => !_overlay.ContainsKey(p)).ToArray())
        {
            if (_commitBaselines[page] is not null && _user is not null) unlocked.Add(page);
            _commitBaselines.Remove(page);
        }
        // A definition page let go after an early publication, and first written in a frame rolled back, is the file's
        // again: the transaction has nothing on it but the AutoNumbers it published (RestoreKeptCounters).
        foreach (int page in _publishedEarly.Keys.Where(p => !_overlay.ContainsKey(p)).ToArray())
            _publishedEarly.Remove(page);
        ResyncFormatVersion();   // page 0 may have been one of the restored images
        return [.. unlocked];
    }

    private byte[]? ReadCommittedPageOrNull(int pageNumber)
    {
        int committedPageCount = (int)(_cache.FileLength / PageSize);
        if (pageNumber < 0 || pageNumber >= committedPageCount) return null;

        // The cache's own array serves: it is never written into (a store replaces it), and a baseline is only
        // compared and, on a failed publish, written back — so a copy of it bought nothing, once per page a
        // transaction touched and again for each page at commit.
        if (_cache.TryGetArray(pageNumber, out byte[] cached)) return cached;

        _locks?.EnterShared(pageNumber);
        try
        {
            if (_cache.TryGetArray(pageNumber, out cached)) return cached;
            var buffer = new byte[PageSize];
            _stream.Seek((long)pageNumber * PageSize, SeekOrigin.Begin);
            _stream.ReadExactly(buffer);
            _codec?.DecryptPage(pageNumber, buffer);
            _cache.Adopt(pageNumber, buffer); // a baseline is only ever compared or published, never written into
            return buffer;
        }
        finally { _locks?.ExitShared(pageNumber); }
    }

    // One array is one image: the cache replaces an array rather than writing into it, so a baseline that is still
    // the committed array has not changed. Only a page reloaded since (evicted, read back) needs its bytes compared.
    // Page 0's commit-slot table is not part of it: every batch, ours or another user's, changes a slot there, and
    // no page write carries the table (WriteThroughUnderPublishLock), so a difference confined to it is no conflict.
    internal bool SamePage(int page, byte[]? left, byte[]? right) =>
        ReferenceEquals(left, right)
        || left is not null && right is not null
            && left.AsSpan(0, page == 0 ? _format.CommitByteTableOffset : PageSize)
                .SequenceEqual(right.AsSpan(0, page == 0 ? _format.CommitByteTableOffset : PageSize));

    /// <summary>Retrieves a higher-layer parse of a page previously stored via <see cref="SetParsedPage"/>
    /// (e.g. an index page's decoded entries), or false if none is cached. The parse is dropped automatically
    /// when the page is written (any channel) or evicted, so a hit is always consistent with the current bytes.</summary>
    internal bool TryGetParsedPage(int pageNumber, out object? parsed)
    {
        // A page buffered in this transaction's overlay has uncommitted bytes; the shared parsed cache reflects
        // the committed image, so it is served from the transaction's own parses instead.
        if (_active is not null && _overlay.ContainsKey(pageNumber))
            return _overlayParsed.TryGetValue(pageNumber, out parsed);
        return _cache.TryGetParsed(pageNumber, out parsed);
    }

    /// <summary>Caches a higher-layer parse of a (resident) page so repeated reads — e.g. a B-tree descent that
    /// re-visits the same root/internal pages — can skip re-decoding it. The caller must not mutate the object
    /// afterwards, as it is shared with other readers of the same file.</summary>
    internal void SetParsedPage(int pageNumber, object parsed)
    {
        // A transaction-local parse goes with the transaction's bytes, never onto the shared (committed) entry.
        if (_active is not null && _overlay.ContainsKey(pageNumber))
        {
            _overlayParsed[pageNumber] = parsed;
            return;
        }
        _cache.SetParsed(pageNumber, parsed);
    }

    public void Flush() => _stream.Flush(flushToDisk: true);

    public void Dispose()
    {
        // A transaction left open publishes nothing; the pages it held are let go for the other channels and users.
        if (_active is not null)
        {
            Unlock(LockedPages());
            ClearTransactionState();
            _active = null;
        }
        UnlockHeld(_transactionLocks, 0);
        UnlockHeld(_statementLocks, 0);
        HeldTableLocks[] held;
        lock (_heldTableLocks) held = [.. _heldTableLocks];
        foreach (HeldTableLocks owner in held) Release(owner);
        if (!_readOnly) _stream.Flush(flushToDisk: false);
        _stream.Dispose();
        _user?.Dispose();   // deregisters, and deletes the lock file when no other user — or channel — is connected
        PageCache.ReleaseKey(_identity); // last channel on this file drops the shared pool
        if (_ownsLocks) MonitorLockManager.ReleaseKey(_identity); // and the shared lock manager
    }
}

/// <summary>What a statement does with a table, which decides the table locks it takes
/// (<see cref="PageChannel.LockTable"/>).</summary>
internal enum TableUse
{
    /// <summary>Reads its rows.</summary>
    Read,
    /// <summary>Writes its rows.</summary>
    Write,
    /// <summary>Changes its design, creates it or drops it.</summary>
    Design,
}

/// <summary>Table locks owned by something other than a statement or a transaction: a statement's result while its rows
/// are read (<see cref="PageChannel.KeepStatementTableLocks"/>), or one read or enumeration of a table outside every
/// statement (<see cref="PageChannel.LockTable"/>). Disposing lets them go; so does the channel's close.</summary>
internal sealed class HeldTableLocks : IDisposable
{
    private readonly PageChannel _channel;

    internal HeldTableLocks(PageChannel channel, List<(JetLockRegion Region, int Page, bool Exclusive)> locks, bool started)
    {
        _channel = channel;
        Locks = locks;
        _started = started;
    }

    private volatile bool _started;

    internal List<(JetLockRegion Region, int Page, bool Exclusive)> Locks { get; }

    /// <summary>Whether the reading they are held for has begun.</summary>
    internal bool Started => _started;

    /// <summary>Marks the reading as begun: the locks are then held until disposed.</summary>
    internal void Start() => _started = true;

    public void Dispose() => _channel.Release(this);
}

/// <summary>The kinds of write a read-only database refuses, each with its own message from ACE.</summary>
public enum ReadOnlyWrite
{
    /// <summary>An SQL INSERT or UPDATE: <i>"Operation must use an updateable query."</i></summary>
    Row,
    /// <summary>An SQL DELETE: <i>"Could not delete from specified tables."</i></summary>
    RowDelete,
    /// <summary>A change to a table's design, or the drop of a table, index or query:
    /// <i>"Cannot modify the design of table 'X'. It is in a read-only database."</i></summary>
    Design,
    /// <summary>A new query (CREATE VIEW/PROCEDURE), a make-table query, or a row written through the table API
    /// rather than SQL — as a DAO recordset's AddNew, Edit and Delete are (all verified):
    /// <i>"Cannot update. Database or object is read-only."</i></summary>
    DatabaseOrObject,
}
