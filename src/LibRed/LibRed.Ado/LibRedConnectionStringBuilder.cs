using System.Data.Common;

namespace LibRed.Data;

/// <summary>
/// Connection string builder for the LibRed provider. LibRed connection strings are always
/// <c>Data Source=...</c> pairs, optionally with a database password, so unlike EFCore.Jet's
/// <c>DbConnectionStringBuilderExtensions.GetDataSource/SetDataSource</c> (which branch on
/// <c>DataAccessProviderType</c>/ODBC/OLE DB and throw for any other builder type), this reads
/// and writes the "Data Source" key directly - no provider-type gate to fall into.
/// </summary>
// CA1010 asks for ICollection<T>; the non-generic ICollection comes from DbConnectionStringBuilder, which
// every ADO.NET provider inherits as-is.
#pragma warning disable CA1010
public class LibRedConnectionStringBuilder : DbConnectionStringBuilder
#pragma warning restore CA1010
{
    /// <summary>
    /// Reads "Data Source", "DataSource", or "DBQ" (whichever is present - matches
    /// <see cref="LibRedConnection" />'s own tolerant key parsing), but always writes the
    /// canonical "Data Source" key.
    /// </summary>
    public string? DataSource
    {
        get => TryGetValue("Data Source", out var value)
            || TryGetValue("DataSource", out value)
            || TryGetValue("DBQ", out value)
                ? (string)value
                : null;
        set => this["Data Source"] = value;
    }

    /// <summary>
    /// The database password. Reads "Password" (LibRed's own key), "Jet OLEDB:Database Password" (ACE OLE DB's
    /// key) or "PWD" (the ODBC Access driver's key), whichever is present in that order, but always writes the
    /// canonical "Password".
    /// </summary>
    public string? Password
    {
        get => TryGetValue("Password", out var value)
            || TryGetValue("Jet OLEDB:Database Password", out value)
            || TryGetValue("PWD", out value)
                ? (string)value
                : null;
        set => this["Password"] = value;
    }

    /// <summary>
    /// The one connection-string parse every LibRed caller uses: an empty string gives an empty builder, a bare
    /// path (no '=') is taken as the data source, anything else goes through the framework parser (so quoted
    /// values and the Data Source / DataSource / DBQ and password aliases all work). It returns the normalized
    /// form: the canonical "Data Source" (expanded to a full path) and "Password" keys, with the alias spellings
    /// removed. Where aliases are mixed, the value the <see cref="DataSource" /> and <see cref="Password" />
    /// getters would have returned is the one kept.
    /// </summary>
    public static LibRedConnectionStringBuilder Parse(string connectionString)
    {
        var builder = new LibRedConnectionStringBuilder();
        if (string.IsNullOrWhiteSpace(connectionString)) return builder;

        // Allow a bare path as the whole connection string.
        if (!connectionString.Contains('='))
            builder.DataSource = connectionString.Trim();
        else
            // Let the framework parser handle quoted/escaped values. In particular, splitting on
            // semicolons selects the wrong file for a valid value such as Data Source="a;b.accdb".
            // When aliases are mixed, prefer the canonical spelling, then DataSource, then DBQ.
            builder.ConnectionString = connectionString;

        // Leave the data source expanded under its one canonical key, so every caller reads the full path.
        string? dataSource = builder.DataSource;
        if (!string.IsNullOrEmpty(dataSource))
        {
            builder.Remove("DataSource");
            builder.Remove("DBQ");
            builder.DataSource = ExpandPath(dataSource);
        }

        string? password = builder.Password;
        if (password != null)
        {
            builder.Remove("Jet OLEDB:Database Password");
            builder.Remove("PWD");
            builder.Password = password;
        }
        return builder;
    }

    /// <summary>
    /// Resolves to a full path and defaults to a ".accdb" extension - matches EFCore.Jet.Data's
    /// internal <c>JetStoreDatabaseHandling.ExpandFileName</c>/<c>EnsureFileExtension</c>, so
    /// LibRedConnection agrees with <c>JetConnection</c>'s bootstrap (see
    /// <c>LibRedConnection.CreateDatabase</c>) about which file e.g. <c>Data Source=Foo</c> actually names.
    /// Without this, a bare name like "Foo" would resolve here to a different path than the
    /// "Foo.accdb" that DAO/ADOX actually creates, and DatabaseExists/DropDatabase/Open would all
    /// silently look in the wrong place.
    /// </summary>
    private static string ExpandPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return string.IsNullOrEmpty(Path.GetExtension(fullPath)) ? fullPath + ".accdb" : fullPath;
    }
}