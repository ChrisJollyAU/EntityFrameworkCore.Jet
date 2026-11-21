using EntityFrameworkCore.LibRed.Design.Internal;
using EntityFrameworkCore.LibRed.Scaffolding.Internal;
using LibRed.Crypto;
using LibRed.Data;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LibRed.EFCore.Tests;

public class ScaffoldingTests
{
    private static readonly string Northwind = Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb");

    // Resolve the factory through the design-time container so the scaffolding logger is injected.
    private static IDatabaseModelFactory Factory()
    {
        var services = new ServiceCollection();
        new LibRedDesignTimeServices().ConfigureDesignTimeServices(services);
        return services.BuildServiceProvider().GetRequiredService<IDatabaseModelFactory>();
    }

    [Fact]
    public void Model_factory_reads_tables_columns_keys_from_the_catalog()
    {
        var model = Factory().Create($"Data Source={Northwind}", new DatabaseModelFactoryOptions());

        // User tables are present; system (MSys*) tables are excluded.
        var tableNames = model.Tables.Select(t => t.Name).ToList();
        Assert.Contains("Customers", tableNames);
        Assert.Contains("Orders", tableNames);
        Assert.DoesNotContain(tableNames, n => n.StartsWith("MSys", StringComparison.OrdinalIgnoreCase));

        var orders = model.Tables.Single(t => t.Name == "Orders");

        // Columns with store types.
        var orderId = orders.Columns.Single(c => c.Name == "OrderID");
        Assert.Equal("counter", orderId.StoreType); // autonumber PK
        Assert.Equal("datetime", orders.Columns.Single(c => c.Name == "OrderDate").StoreType);
        // CustomerID is a fixed-length 5-char text column in Northwind (the EFCore.Jet fixture maps it
        // nchar(5)), so it scaffolds as char(5); ShipName is an ordinary variable-length text → varchar.
        Assert.Equal("char(5)", orders.Columns.Single(c => c.Name == "CustomerID").StoreType);
        Assert.StartsWith("varchar(", orders.Columns.Single(c => c.Name == "ShipName").StoreType);

        // Primary key.
        Assert.NotNull(orders.PrimaryKey);
        Assert.Equal(["OrderID"], orders.PrimaryKey!.Columns.Select(c => c.Name));

        // Foreign keys: Orders references Customers and Employees.
        var principals = orders.ForeignKeys.Select(f => f.PrincipalTable.Name).ToList();
        Assert.Contains("Customers", principals);
        Assert.Contains("Employees", principals);
    }

    [Fact]
    public void Composite_primary_key_is_read()
    {
        var model = Factory()
            .Create($"Data Source={Northwind}", new DatabaseModelFactoryOptions());

        var orderDetails = model.Tables.Single(t => t.Name == "Order Details");
        Assert.Equal(["OrderID", "ProductID"], orderDetails.PrimaryKey!.Columns.Select(c => c.Name));
    }

    [Fact]
    public void Nullability_and_defaults_are_read_from_the_catalog()
    {
        var model = Factory().Create($"Data Source={Northwind}", new DatabaseModelFactoryOptions());

        var customers = model.Tables.Single(t => t.Name == "Customers");
        // A Required (NOT NULL) non-counter column reads as non-nullable; an optional one as nullable.
        Assert.False(customers.Columns.Single(c => c.Name == "CompanyName").IsNullable);
        Assert.True(customers.Columns.Single(c => c.Name == "ContactName").IsNullable);

        // A counter is never nullable — even though it carries no Required property.
        var orders = model.Tables.Single(t => t.Name == "Orders");
        Assert.False(orders.Columns.Single(c => c.Name == "OrderID").IsNullable);

        // Column DefaultValue (expression source text) is surfaced as DefaultValueSql.
        var od = model.Tables.Single(t => t.Name == "Order Details");
        Assert.Equal("1", od.Columns.Single(c => c.Name == "Quantity").DefaultValueSql);
        Assert.Equal("0", od.Columns.Single(c => c.Name == "UnitPrice").DefaultValueSql);
        Assert.Null(customers.Columns.Single(c => c.Name == "ContactName").DefaultValueSql);
    }

    [Fact]
    public void A_bare_path_connection_string_scaffolds()
    {
        var model = Factory().Create(Northwind, new DatabaseModelFactoryOptions());
        Assert.Contains("Customers", model.Tables.Select(t => t.Name));
    }

    [Fact]
    public void A_password_protected_database_scaffolds_with_the_connection_strings_password()
    {
        string path = Path.Combine(Path.GetTempPath(), $"scaffold-pwd-{Guid.NewGuid():N}.accdb");
        File.Copy(Northwind, path);
        try
        {
            using (JetDatabase db = JetDatabase.Open(path, readOnly: false, exclusive: true))
                DatabaseEncryption.SetPassword(db, "abc", AccessEncryption.Agile);

            var model = Factory().Create($"Data Source={path};Password=abc", new DatabaseModelFactoryOptions());
            Assert.Contains("Customers", model.Tables.Select(t => t.Name));
        }
        finally { try { File.Delete(path); } catch (IOException) { } }
    }

    // A default stored with a leading = (as ACE stores DEFAULT =7) is the value without it; the text stays verbatim.
    [Fact]
    public void A_default_with_a_leading_equals_scaffolds_as_the_value_without_it()
    {
        string path = Path.Combine(Path.GetTempPath(), $"scaffold-default-{Guid.NewGuid():N}.accdb");
        File.Copy(Northwind, path);
        try
        {
            using (var conn = new LibRedConnection($"Data Source={path}"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CREATE TABLE D (Id long PRIMARY KEY, A long DEFAULT 7, B long DEFAULT =7, " +
                                  "C text(20) DEFAULT 'hi', E text(20) DEFAULT ='hi')";
                cmd.ExecuteNonQuery();
            }

            var columns = Factory().Create($"Data Source={path}", new DatabaseModelFactoryOptions())
                .Tables.Single(t => t.Name == "D").Columns;
            Assert.Equal("=7", columns.Single(c => c.Name == "B").DefaultValueSql);
            Assert.Equal(7, columns.Single(c => c.Name == "A").DefaultValue);
            Assert.Equal(7, columns.Single(c => c.Name == "B").DefaultValue);
            Assert.Equal("hi", columns.Single(c => c.Name == "C").DefaultValue);
            Assert.Equal("hi", columns.Single(c => c.Name == "E").DefaultValue);
        }
        finally { try { File.Delete(path); } catch (IOException) { } }
    }

    // A calculated column is as nullable as the type it reports: one yielding Yes/No is not nullable although it is
    // stored in two bytes, and a calculated Long or Text one is. Measured: ACE's Columns rowset says Id:N A:N B:R L:N X:N
    // for this table (CalculatedColumnSqlAccessTests), as do INFORMATION_SCHEMA.COLUMNS and the Columns rowset.
    [Fact]
    public void A_calculated_YesNo_column_scaffolds_as_not_nullable_as_ACE_reports_it()
    {
        string path = Path.Combine(Path.GetTempPath(), $"scaffold-calc-{Guid.NewGuid():N}.accdb");
        File.Copy(Northwind, path);
        try
        {
            using (var conn = new LibRedConnection($"Data Source={path}"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CREATE TABLE T (Id LONG PRIMARY KEY, A LONG, B YESNO AS ([A] > 0), L LONG AS ([A]*2), " +
                                  "X TEXT(20) AS (\"n\" & [A]))";
                cmd.ExecuteNonQuery();
            }

            var columns = Factory().Create($"Data Source={path}", new DatabaseModelFactoryOptions())
                .Tables.Single(t => t.Name == "T").Columns;
            Assert.Equal("Id:N A:N B:R L:N X:N", string.Join(" ", columns.Select(c => $"{c.Name}:{(c.IsNullable ? "N" : "R")}")));
        }
        finally { try { File.Delete(path); } catch (IOException) { } }
    }

    // A calculated column scaffolds as the type it reports, which INFORMATION_SCHEMA.COLUMNS gives too: ACE's Columns
    // rowset reports the Yes/No result as a not-nullable Yes/No and the Text result as long text of length 0, whatever
    // their descriptors store (measured, CalculatedColumnSqlAccessTests).
    [Fact]
    public void Calculated_columns_scaffold_as_INFORMATION_SCHEMA_reports_them()
    {
        string path = Path.Combine(Path.GetTempPath(), $"scaffold-calc-types-{Guid.NewGuid():N}.accdb");
        File.Copy(Northwind, path);
        try
        {
            var view = new List<string>();
            using (var conn = new LibRedConnection($"Data Source={path}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE T (Id LONG PRIMARY KEY, A LONG, B YESNO AS ([A] > 0), L LONG AS ([A]*2), " +
                                      "X TEXT(20) AS (\"n\" & [A]), D DOUBLE AS ([A]/3))";
                    cmd.ExecuteNonQuery();
                }
                using var select = conn.CreateCommand();
                select.CommandText = "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM `INFORMATION_SCHEMA.COLUMNS` " +
                                     "WHERE TABLE_NAME = 'T' ORDER BY ORDINAL_POSITION";
                using var reader = select.ExecuteReader();
                while (reader.Read()) view.Add($"{reader.GetString(0)}:{reader.GetString(1)}:{(reader.GetBoolean(2) ? "N" : "R")}");
            }

            var columns = Factory().Create($"Data Source={path}", new DatabaseModelFactoryOptions())
                .Tables.Single(t => t.Name == "T").Columns;
            string scaffolded = string.Join(" ", columns.Select(c => $"{c.Name}:{c.StoreType}:{(c.IsNullable ? "N" : "R")}"));
            Assert.Equal("Id:integer:N A:integer:N B:bit:R L:integer:N X:longchar:N D:double:N", scaffolded);
            Assert.Equal(string.Join(" ", view), scaffolded);
        }
        finally { try { File.Delete(path); } catch (IOException) { } }
    }

    [Fact]
    public void On_delete_set_null_foreign_key_scaffolds_as_set_null()
    {
        string path = Path.Combine(Path.GetTempPath(), $"scaffold-fk-{Guid.NewGuid():N}.accdb");
        File.Copy(Northwind, path);
        try
        {
            using (var conn = new LibRedConnection($"Data Source={path}"))
            {
                conn.Open();
                Exec(conn, "CREATE TABLE P (Id long PRIMARY KEY, N long)");
                Exec(conn, "CREATE TABLE C (Id long PRIMARY KEY, ParentId long, " +
                           "CONSTRAINT FK_C FOREIGN KEY (ParentId) REFERENCES P (Id) ON DELETE SET NULL)");
            }

            var model = Factory().Create($"Data Source={path}", new DatabaseModelFactoryOptions());
            var fk = model.Tables.Single(t => t.Name == "C").ForeignKeys.Single();
            Assert.Equal("P", fk.PrincipalTable.Name);
            Assert.Equal("SetNull", fk.OnDelete?.ToString());
        }
        finally { try { File.Delete(path); } catch (IOException) { } }

        static void Exec(LibRedConnection c, string sql)
        { using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery(); }
    }

    [Fact]
    public void Table_filter_is_honoured()
    {
        var model = Factory()
            .Create($"Data Source={Northwind}",
                new DatabaseModelFactoryOptions(tables: ["Shippers"], schemas: []));

        Assert.Equal(["Shippers"], model.Tables.Select(t => t.Name));
    }

    [Fact]
    public void Design_time_services_resolve_the_libred_model_factory()
    {
        var services = new ServiceCollection();
        new LibRedDesignTimeServices().ConfigureDesignTimeServices(services);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<LibRedDatabaseModelFactory>(provider.GetRequiredService<IDatabaseModelFactory>());
    }
}
