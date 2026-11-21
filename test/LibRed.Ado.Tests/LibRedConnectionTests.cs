using LibRed.Crypto;
using LibRed.Data;
using LibRed.Tests.Shared;
using Xunit;

namespace LibRed.Ado.Tests;

public class LibRedConnectionTests
{
    [Fact]
    public void Quoted_semicolon_in_data_source_is_not_treated_as_a_separator()
    {
        string path = Path.Combine(Path.GetTempPath(), "libred;quoted.accdb");

        using var connection = new LibRedConnection($"Data Source=\"{path}\";Mode=Read");

        Assert.Equal(Path.GetFullPath(path), connection.DataSource);
    }

    [Fact]
    public void Canonical_data_source_wins_when_aliases_are_mixed()
    {
        string canonical = Path.Combine(Path.GetTempPath(), "canonical.accdb");
        string alias = Path.Combine(Path.GetTempPath(), "alias.accdb");

        using var connection = new LibRedConnection($"DBQ={alias};Data Source={canonical}");

        Assert.Equal(Path.GetFullPath(canonical), connection.DataSource);
    }

    [Fact]
    public void Jet4_mdb_opens_with_the_database_password_and_refuses_without_or_with_a_wrong_one()
    {
        string path = TemporaryDatabase.CreatePath("libred_pwd_mdb", ".mdb");
        try
        {
            JetDatabase.Create(path, version: 0x01);
            using (JetDatabase db = JetDatabase.Open(path, readOnly: false, exclusive: true))
                DatabaseEncryption.SetJetPassword(db, "abc");

            AssertPasswordHandling(path);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void Agile_accdb_opens_with_the_database_password_and_refuses_without_or_with_a_wrong_one()
    {
        string path = TemporaryDatabase.CreatePath("libred_pwd_accdb");
        try
        {
            JetDatabase.Create(path);
            using (JetDatabase db = JetDatabase.Open(path, readOnly: false, exclusive: true))
                DatabaseEncryption.SetPassword(db, "abc", AccessEncryption.Agile);

            AssertPasswordHandling(path);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    private static void AssertPasswordHandling(string path)
    {
        foreach (string key in new[] { "Password", "Jet OLEDB:Database Password", "PWD" })
        {
            using var ok = new LibRedConnection($"Data Source={path};{key}=abc");
            ok.Open();
            Assert.Equal(System.Data.ConnectionState.Open, ok.State);
        }

        using var none = new LibRedConnection($"Data Source={path}");
        Assert.Throws<InvalidOperationException>(() => none.Open());

        using var wrong = new LibRedConnection($"Data Source={path};Jet OLEDB:Database Password=wrong");
        Assert.Throws<UnauthorizedAccessException>(() => wrong.Open());
    }

    [Theory]
    [InlineData(".accdb", null)]
    [InlineData(".mdb", (byte)0x01)]
    public void A_file_with_no_password_opens_whatever_password_the_string_carries(string extension, byte? version)
    {
        string path = TemporaryDatabase.CreatePath("libred_nopwd", extension);
        try
        {
            if (version is { } v) JetDatabase.Create(path, version: v); else JetDatabase.Create(path);

            using var connection = new LibRedConnection($"Data Source={path};Password=abc");
            connection.Open();
            Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void Password_key_takes_precedence_over_the_Jet_OLEDB_key_when_both_are_given()
    {
        string path = TemporaryDatabase.CreatePath("libred_pwd_mixed", ".mdb");
        try
        {
            JetDatabase.Create(path, version: 0x01);
            using (JetDatabase db = JetDatabase.Open(path, readOnly: false, exclusive: true))
                DatabaseEncryption.SetJetPassword(db, "abc");

            // Deliberate precedence: "Password" is read first, so the Jet OLEDB key only counts when it is absent.
            using var wrongFirst = new LibRedConnection($"Data Source={path};Password=wrong;Jet OLEDB:Database Password=abc");
            Assert.Throws<UnauthorizedAccessException>(() => wrongFirst.Open());

            using var rightFirst = new LibRedConnection($"Data Source={path};Password=abc;Jet OLEDB:Database Password=wrong");
            rightFirst.Open();
            Assert.Equal(System.Data.ConnectionState.Open, rightFirst.State);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void Connection_string_builder_writes_the_password_under_the_password_key()
    {
        var builder = new LibRedConnectionStringBuilder { DataSource = "a.accdb", Password = "abc" };

        Assert.Contains("password=abc", builder.ConnectionString, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("abc", new LibRedConnectionStringBuilder { ConnectionString = "Password=abc" }.Password);
        Assert.Equal("abc", new LibRedConnectionStringBuilder { ConnectionString = "PWD=abc" }.Password);
        Assert.Equal("abc", new LibRedConnectionStringBuilder { ConnectionString = "Jet OLEDB:Database Password=abc" }.Password);
    }

    [Fact]
    public void Parse_normalizes_the_data_source_alias_to_one_canonical_key()
    {
        var builder = LibRedConnectionStringBuilder.Parse("DBQ=x.accdb");

        Assert.Single(builder.Keys.Cast<string>());
        Assert.True(builder.ContainsKey("Data Source"));
        Assert.Equal(Path.GetFullPath("x.accdb"), builder.DataSource);
    }

    [Fact]
    public void Parse_normalizes_the_password_alias_to_the_password_key()
    {
        var builder = LibRedConnectionStringBuilder.Parse("PWD=abc;Data Source=x");

        Assert.Equal(["data source", "password"], builder.Keys.Cast<string>().Select(k => k.ToLowerInvariant()).Order(StringComparer.Ordinal));
        Assert.Equal("abc", builder["Password"]);

        var mixed = LibRedConnectionStringBuilder.Parse("PWD=wrong;Jet OLEDB:Database Password=abc;Data Source=x");
        Assert.Equal("abc", mixed["Password"]);
        Assert.False(mixed.ContainsKey("PWD"));
        Assert.False(mixed.ContainsKey("Jet OLEDB:Database Password"));
    }

    [Fact]
    public void GetConnectionString_gives_the_normalized_expanded_data_source()
    {
        Assert.Equal(Path.GetFullPath("LibRed.accdb"),
            LibRedConnectionStringBuilder.Parse(LibRedConnection.GetConnectionString("LibRed.accdb")).DataSource);
        Assert.StartsWith("Data Source=", LibRedConnection.GetConnectionString("LibRed.accdb"), StringComparison.Ordinal);
        Assert.Equal(string.Empty, LibRedConnection.GetConnectionString(""));
    }

    [Fact]
    public void Parse_keeps_a_quoted_semicolon_a_bare_path_and_an_empty_string()
    {
        Assert.Equal(Path.GetFullPath("a;b.accdb"), LibRedConnectionStringBuilder.Parse("Data Source=\"a;b.accdb\"").DataSource);
        Assert.Equal(Path.GetFullPath("bare.accdb"), LibRedConnectionStringBuilder.Parse("  bare.accdb ").DataSource);
        Assert.Equal(string.Empty, LibRedConnectionStringBuilder.Parse("").ConnectionString);
    }
}
