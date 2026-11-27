using LibRed.Catalog;
using LibRed.Sql.Binding;
using System.Runtime.CompilerServices;

namespace LibRed.Engine.Schema;

/// <summary>
/// Adapts the Core <see cref="JetCatalog"/> to the SQL binder's <see cref="ISchemaProvider"/>.
/// This is the inverted dependency in action: LibRed.Sql defines the interface,
/// LibRed.Engine supplies the storage-backed implementation.
/// </summary>
public sealed class CatalogSchemaProvider(JetCatalog catalog) : ISchemaProvider
{
    private readonly JetCatalog _catalog = catalog;
    private readonly ConditionalWeakTable<TableDefinition.DdlMetadata, TableSchema> _tables = new();

    public ITableSchema? GetTable(string name)
    {
        // The magic INFORMATION_SCHEMA.<view> tables are virtual (no catalog TableDefinition) — expose a synthetic
        // schema so binding validates; the executor materialises their rows from the catalog.
        if (InformationSchema.IsInformationSchema(name))
            return new InformationSchemaTable(name, _catalog.Names);

        TableDefinition? def = _catalog.FindTable(name);
        return def is null ? null
            : _tables.GetValue(def.Metadata!, ddl => new TableSchema(ddl, _catalog.Names));
    }

    /// <summary>A view of INFORMATION_SCHEMA, its columns named apart by <paramref name="names"/>.</summary>
    private sealed class InformationSchemaTable(string name, IEqualityComparer<string> names) : ITableSchema
    {
        public string Name => name;
        public IReadOnlyList<IColumnSchema> Columns { get; } =
            InformationSchema.ColumnsOf(name).Zip(InformationSchema.ColumnTypesOf(name),
                (column, type) => (IColumnSchema)new VirtualColumn(column, type)).ToList();
        public IColumnSchema? FindColumn(string col) => Columns.FirstOrDefault(c => names.Equals(c.Name, col));
    }

    private sealed class VirtualColumn(string name, Type clrType) : IColumnSchema
    {
        public string Name => name;
        public bool IsNullable => true;
        public Type ClrType => clrType;
    }

    /// <summary>A stored table, its columns named apart by <paramref name="names"/>, the catalog's
    /// <see cref="JetCatalog.Names"/>.</summary>
    private sealed class TableSchema(TableDefinition.DdlMetadata def, IEqualityComparer<string> names) : ITableSchema
    {
        public string Name => def.Name;
        public IReadOnlyList<IColumnSchema> Columns => field ??=
            Array.AsReadOnly(def.Columns.Select(c => (IColumnSchema)new ColumnSchema(c)).ToArray());

        public IColumnSchema? FindColumn(string name) => Columns.FirstOrDefault(c => names.Equals(c.Name, name));
    }

    private sealed class ColumnSchema(ColumnDef col) : IColumnSchema
    {
        public string Name { get; } = col.Name;
        public bool IsNullable { get; } = col.IsNullable;
        public Type ClrType { get; } = JetClrTypeMap.ToClrType(col.Type);
    }
}
