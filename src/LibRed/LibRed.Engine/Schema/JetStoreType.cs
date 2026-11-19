using LibRed.Catalog;

namespace LibRed.Engine.Schema;

/// <summary>
/// The single source for a column's SQL store-type presentation, shared by the two consumers that must agree:
/// LibRed's <see cref="InformationSchema"/> views (DATA_TYPE / IS_NULLABLE / CHARACTER_MAXIMUM_LENGTH) and the
/// database-first scaffolder (<c>LibRedDatabaseModelFactory</c>, which composes <c>DatabaseColumn.StoreType</c>).
/// Duplicating these derivations is exactly how the two diverged before — the store-type name map and the
/// "an AutoNumber/counter is never nullable" rule live here once. Mirrors EFCore.Jet's
/// <c>AdoxSchema.GetDataTypeString</c> (bare name + separate facet columns).
/// </summary>
public static class JetStoreType
{
    /// <summary>An AutoNumber (counter) column is never nullable — its non-null behaviour comes from the
    /// AutoNumber flag, not the LvProp <c>Required</c> property (verified; matches Access/DAO).
    /// <para>A complex column is the exception, because it carries the same <c>0x04</c> flag without being a
    /// counter in this sense. ACE reports an attachment column as <b>nullable</b> over OLE DB's schema
    /// rowset, and DAO reports <c>Required = False</c> for it; treating the flag alone as decisive
    /// contradicted both.</para>
    /// <para>A Yes/No column is never nullable either, whether or not it is Required: its value is one bit of the
    /// row, a Null written to it is stored as False, and ACE's OLE DB schema rowset reports it so (verified).
    /// That goes by the type the column reports, so a calculated column whose expression yields Yes/No is not
    /// nullable although its value is stored in two bytes, while a calculated Long or Text one is (verified against
    /// ACE's Columns rowset).</para>
    /// <para>The column's own <c>Required</c> property comes on top of what its type allows
    /// (<see cref="TypeHoldsNull"/>).</para></summary>
    public static bool IsNullable(ColumnDef column) => TypeHoldsNull(column) && column.IsNullable;

    /// <summary>Whether the column's type can hold a Null at all, whatever its <c>Required</c> property says — what a
    /// view over the column reports (verified against ACE's Columns rowset for a view: an AutoNumber, a stored and a
    /// calculated Yes/No are not nullable, a required Long and a calculated Long are). See
    /// <see cref="IsNullable"/>.</summary>
    public static bool TypeHoldsNull(ColumnDef column) =>
        (!column.IsAutoNumber || column.Type == JetDataType.Complex) && ClrTypeHoldsNull(JetClrTypeMap.ToClrType(column.ReportedType));

    /// <summary>Whether a value of <paramref name="clrType"/> can be Null: a Yes/No never is. For a view's computed
    /// column, which has no stored column behind it, this is the whole answer.</summary>
    public static bool ClrTypeHoldsNull(Type? clrType) => clrType != typeof(bool);

    /// <summary>The bare type name (no facets) — <c>INFORMATION_SCHEMA.COLUMNS.DATA_TYPE</c>, with length /
    /// precision / scale reported in their own columns. Mirrors <c>AdoxSchema.GetDataTypeString</c>. Like every
    /// facet here it names the type the column reports (<see cref="ColumnDef.ReportedType"/>), not a calculated
    /// column's storage type.</summary>
    public static string TypeName(ColumnDef column) => column.ReportedType switch
    {
        JetDataType.Boolean => "bit",
        JetDataType.Byte => "byte",
        JetDataType.Int16 => "smallint",
        JetDataType.Int32 => column.IsAutoNumber ? "counter" : "integer",
        JetDataType.Int64 => "bigint",
        JetDataType.Single => "single",
        JetDataType.Double => "double",
        JetDataType.Currency => "currency",
        JetDataType.DateTime => "datetime",
        JetDataType.DateTimeExtended => "datetime2",
        JetDataType.Guid => "guid",
        JetDataType.FixedPoint => "decimal",
        // Text keeps its fixed/variable distinction (adChar/adVarChar → char/varchar). Binary does NOT: ADOX
        // reports every binary column as adVarBinary (it never surfaces fixed adBinary), so a fixed binary column
        // collapses to varbinary — match that, or INFORMATION_SCHEMA/scaffolding disagree with EFCore.Jet's output.
        // A BigBinary column is reported as adVarBinary too, with its own DefinedSize (verified).
        JetDataType.Text => column.IsFixedLength ? "char" : "varchar",
        JetDataType.Binary or JetDataType.BigBinary => "varbinary",
        JetDataType.Memo => "longchar",
        JetDataType.Ole => "longbinary",
        // A complex (multi-value / attachment) column has no type of its own on this surface, because OLE DB
        // cannot express one: ACE flattens it to long text, giving the identical schema row to a Memo column
        // — type 130, flags 234, max length 0, and System.String at the Memo ceiling in a reader's schema
        // table. Only DAO names it (type 101, dbAttachment). Reported as Memo is, so a caller reading this
        // sees what it would see through ACE. Falling through to the default gave a bare "varchar", which is
        // also a malformed store type — every other name either carries its facet or takes none at all.
        JetDataType.Complex => "longchar",
        _ => "varchar",
    };

    /// <summary>Length facet: characters for text (on-disk bytes are 2 per char), bytes for binary; null for
    /// everything else. This is <c>CHARACTER_MAXIMUM_LENGTH</c>.</summary>
    public static int? MaxLength(ColumnDef column) => column.ReportedType switch
    {
        JetDataType.Text => Math.Max(1, column.Length / 2),
        JetDataType.Binary or JetDataType.BigBinary => Math.Max(1, column.Length),
        _ => null,
    };

    /// <summary>The full store type EF's type mapping consumes — bare name plus its facet suffix, e.g.
    /// <c>varchar(255)</c>, <c>decimal(18,0)</c>, <c>counter</c>. Built from <see cref="TypeName"/> so it can
    /// never disagree with the INFORMATION_SCHEMA presentation. No space after the precision/scale comma —
    /// EF's own store types are unspaced (SQL Server, and what the scaffolding tests expect).</summary>
    public static string StoreType(ColumnDef column)
    {
        string name = TypeName(column);
        return column.ReportedType switch
        {
            JetDataType.FixedPoint => $"{name}({column.Precision},{column.Scale})",
            JetDataType.Text or JetDataType.Binary or JetDataType.BigBinary => $"{name}({MaxLength(column)})",
            _ => name,
        };
    }
}