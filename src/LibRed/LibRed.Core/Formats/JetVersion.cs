using LibRed.Catalog;

namespace LibRed.Formats
{
    // CA1707 (no underscores in identifiers): each member names the format version AND the Access release it
    // shipped with — Version16_2016 — which is how the spec, the probes and the ACE documentation all refer
    // to them. Scoped to this enum so the rule keeps working everywhere else.
#pragma warning disable CA1707
    public enum JetVersion
    {
        Version3 = 0x0,
        Version4 = 0x1,
        Version12_2007 = 0x2,
        Version14_2010 = 0x3,
        Version15_2013 = 0x4,
        Version16_2016 = 0x5,
        Version17_2019 = 0x6,
    }
#pragma warning restore CA1707

    /// <summary>
    /// Which format version a column type needs in order to exist on disk at all.
    /// </summary>
    /// <remarks>
    /// Stated over <see cref="JetDataType"/> rather than over a SQL type name, so it holds in the layer that
    /// actually writes the descriptor: every <c>JetDatabase</c> DDL method takes a <c>ColumnSpec</c> carrying a
    /// raw <see cref="JetDataType"/> straight to the writer, the SQL front end's among them (its
    /// <c>AccessTypeMapper</c> maps a type whatever the file's version). <c>SchemaEditor.RaiseFormatFor</c> raises
    /// the file to what <see cref="Required"/> says, and <see cref="EnsureStorable"/> refuses what reaches the
    /// writer unraised — a 0x13 descriptor in an ACE 12 file is a column Access cannot read.
    /// <para>BIGINT and DATETIME2 arrived in DIFFERENT versions — verified against files authored with each
    /// feature enabled: Large Number forces ACE 16 (version byte 0x05), Date/Time Extended ACE 17 (0x06).</para>
    /// </remarks>
    public static class JetDataTypeVersions
    {
        /// <summary>The minimum format version <paramref name="type"/> can be stored in, or <c>null</c> when
        /// every format can hold it.</summary>
        public static (JetVersion Min, string Label)? Required(JetDataType type) => type switch
        {
            JetDataType.Int64 => (JetVersion.Version16_2016, "Access 2016 (ACE 16)"),
            JetDataType.DateTimeExtended => (JetVersion.Version17_2019, "Access 2019+ (ACE 17)"),
            _ => null,
        };

        /// <summary>Throws when <paramref name="type"/> cannot exist in <paramref name="version"/>. Callers
        /// that are able to upgrade the file raise it first (see <c>SchemaEditor.RaiseFormatFor</c>),
        /// which is what Access itself does; this catches the ones that did not or could not.</summary>
        public static void EnsureStorable(JetDataType type, JetVersion version, string columnName)
        {
            if (Required(type) is { } required && version < required.Min)
                throw new NotSupportedException(
                    $"Column '{columnName}' is {type}, which requires {required.Label} or later; "
                    + $"this database is {version}.");
        }
    }
}