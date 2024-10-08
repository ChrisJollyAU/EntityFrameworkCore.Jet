// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using EntityFrameworkCore.Jet.Infrastructure.Internal;
using EntityFrameworkCore.Jet.Internal;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage;
using static System.Formats.Asn1.AsnWriter;

namespace EntityFrameworkCore.Jet.Storage.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class JetTypeMappingSource : RelationalTypeMappingSource
    {
        private readonly JetByteArrayTypeMapping _fixedLengthBinary = new JetByteArrayTypeMapping("binary");
        private readonly JetByteArrayTypeMapping _variableLengthBinary = new JetByteArrayTypeMapping("varbinary");
        private readonly JetByteArrayTypeMapping _variableLengthMaxBinary = new JetByteArrayTypeMapping("varbinary", storeTypePostfix: StoreTypePostfix.None);
        private readonly JetByteArrayTypeMapping _unboundedBinary = new JetByteArrayTypeMapping("longbinary", storeTypePostfix: StoreTypePostfix.None);

        private readonly JetBoolTypeMapping _bit = new JetBoolTypeMapping("bit"); // JET bits are not nullable
        private readonly JetBoolTypeMapping _bool = new JetBoolTypeMapping("smallint");

        // We just map counter etc. to integer. Whether an integer property/column is actually a counter
        // is determined by the value generation type.
        private readonly IntTypeMapping _counter = new JetIntTypeMapping("integer");

        private readonly JetByteTypeMapping _byte = new JetByteTypeMapping("byte", DbType.Byte); // unsigned, there is no signed byte in Jet
        private readonly ShortTypeMapping _smallint = new ShortTypeMapping("smallint", DbType.Int16);
        private readonly IntTypeMapping _integer = new JetIntTypeMapping("integer");
        private readonly JetLongTypeMapping _bigint = new JetLongTypeMapping("decimal(20, 0)", precision: 20, scale: 0, StoreTypePostfix.PrecisionAndScale);

        private readonly JetFloatTypeMapping _single = new JetFloatTypeMapping("single");
        private readonly JetDoubleTypeMapping _double = new JetDoubleTypeMapping("double");

        private readonly JetDecimalTypeMapping _decimal = new JetDecimalTypeMapping("decimal(18,2)", DbType.Decimal, precision: 18, scale: 2, StoreTypePostfix.PrecisionAndScale);
        private readonly JetDecimalTypeMapping _decimal18_0 = new JetDecimalTypeMapping("decimal", DbType.Decimal, precision: 18, scale: 0);
        private readonly JetDecimalTypeMapping _currency = new JetDecimalTypeMapping("currency", DbType.Currency, storeTypePostfix:StoreTypePostfix.None);

        private readonly JetDateTimeTypeMapping _datetime;
        private readonly JetDateTimeTypeMapping _dateasdatetime;
        private readonly JetDateTimeOffsetTypeMapping _datetimeoffset;
        private readonly JetDateOnlyTypeMapping _dateonly;
        private readonly JetTimeSpanTypeMapping _timespan;
        private readonly JetTimeOnlyTypeMapping _timeonly;

        private readonly JetStringTypeMapping _fixedLengthUnicodeString = new JetStringTypeMapping("char", unicode: true);
        private readonly JetStringTypeMapping _variableLengthUnicodeString = new JetStringTypeMapping("varchar", unicode: true);
        private readonly JetStringTypeMapping _variableLengthMaxUnicodeString = new JetStringTypeMapping("varchar", unicode: true, size: 255, storeTypePostfix: StoreTypePostfix.Size);
        private readonly JetStringTypeMapping _unboundedUnicodeString = new JetStringTypeMapping("longchar", unicode: true, storeTypePostfix: StoreTypePostfix.None);
        private readonly JetJsonTypeMapping _jsonTypeMapping = new JetJsonTypeMapping("longchar");
        private readonly JetGuidTypeMapping _guid = new JetGuidTypeMapping("uniqueidentifier", DbType.Guid);
        private readonly JetByteArrayTypeMapping _rowversion = new JetByteArrayTypeMapping("varbinary", size: 8,
            comparer: new ValueComparer<byte[]>(
                (v1, v2) => StructuralComparisons.StructuralEqualityComparer.Equals(v1, v2),
                v => StructuralComparisons.StructuralEqualityComparer.GetHashCode(v),
                v => v.ToArray()));

        private readonly Dictionary<string, RelationalTypeMapping[]> _storeTypeMappings;
        private readonly Dictionary<Type, RelationalTypeMapping> _clrTypeMappings;
        private readonly HashSet<string> _disallowedMappings;

        private readonly IJetOptions _options;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public JetTypeMappingSource(
            [NotNull] TypeMappingSourceDependencies dependencies,
            [NotNull] RelationalTypeMappingSourceDependencies relationalDependencies,
            [NotNull] IJetOptions options)
            : base(dependencies, relationalDependencies)
        {
            // References:
            // https://docs.microsoft.com/en-us/previous-versions/office/developer/office2000/aa140015(v=office.10)
            // https://docs.microsoft.com/en-us/office/vba/access/concepts/error-codes/comparison-of-data-types
            // https://support.office.com/en-us/article/equivalent-ansi-sql-data-types-7a0a6bef-ef25-45f9-8a9a-3c5f21b5c65d
            // https://sourcedaddy.com/ms-access/sql-data-types.html

            // TODO: Check the types and their mappings against
            //       https://docs.microsoft.com/en-us/previous-versions/office/developer/office2000/aa140015(v=office.10)

            _datetime = new JetDateTimeTypeMapping("datetime", options, dbType: DbType.DateTime);
            _dateasdatetime = new JetDateTimeTypeMapping("date", options, dbType: DbType.Date);
            _datetimeoffset = new JetDateTimeOffsetTypeMapping("datetime", options);
            _dateonly = new JetDateOnlyTypeMapping("date", options, dbType: DbType.Date);
            _timeonly = new JetTimeOnlyTypeMapping("time", options);
            _timespan = new JetTimeSpanTypeMapping("datetime", options);

            _storeTypeMappings
                = new Dictionary<string, RelationalTypeMapping[]>(StringComparer.OrdinalIgnoreCase)
                {
                    {"binary",                     new []{_fixedLengthBinary}},

                    {"varbinary",                  new []{_variableLengthBinary}},
                    {"binary varying",             new[] { _variableLengthBinary }},
                    {"bit varying",                new[] { _variableLengthBinary }},

                    {"longbinary",                 new[] { _unboundedBinary }},
                    {"general",                    new[] { _unboundedBinary }},
                    {"image",                      new[] { _unboundedBinary }},
                    {"oleobject",                  new[] { _unboundedBinary }},

                    {"bit",                        new[] { _bit }},
                    {"boolean",                    new[] { _bit }},
                    {"logical",                    new[] { _bit }},
                    {"logical1",                   new[] { _bit }},
                    {"yesno",                      new[] { _bit }},

                    {"counter",                    new[] { _counter }},
                    {"identity",                   new[] { _counter }},
                    {"autoincrement",              new[] { _counter }},

                    {"byte",                       new[] { _byte }},
                    {"tinyint",                    new[] { _byte }},
                    {"integer1",                   new[] { _byte }},

                    {"smallint",                   new[] { _smallint }},
                    {"short",                      new[] { _smallint }},
                    {"integer2",                   new[] { _smallint }},

                    {"integer",                    new[] { _integer }},
                    {"long",                       new[] { _bigint }},
                    {"int",                        new[] { _integer }},
                    {"integer4",                   new[] { _integer }},

                    {"single",                     new[] { _single }},
                    {"real",                       new[] { _single }},
                    {"float4",                     new[] { _single }},
                    {"ieeesingle",                 new[] { _single }},

                    {"double",                     new[] { _double }},
                    {"float",                      new[] { _double }},
                    {"float8",                     new[] { _double }},
                    {"ieeedouble",                 new[] { _double }},
                    {"number",                     new[] { _double }},

                    {"decimal",                    new[] { _decimal18_0 }},
                    {"numeric",                    new[] { _decimal18_0 }},
                    {"dec",                        new[] { _decimal18_0 }},

                    {"currency",                   new[] { _currency }},
                    {"money",                      new[] { _currency }},

                    {"datetime",                   new[] { _datetime }},
                    {"date",                       new RelationalTypeMapping[] { _dateonly, _dateasdatetime }},
                    {"time",                       new RelationalTypeMapping[] { _timeonly, _timespan }},

                    {"char",                       new[] { _fixedLengthUnicodeString }},
                    {"alphanumeric",               new[] { _fixedLengthUnicodeString }},
                    {"character",                  new[] { _fixedLengthUnicodeString }},
                    {"nchar",                      new[] { _fixedLengthUnicodeString }},
                    {"national char",              new[] { _fixedLengthUnicodeString }},
                    {"national character",         new[] { _fixedLengthUnicodeString }},

                    {"varchar",                    new[] { _variableLengthUnicodeString }},
                    {"string",                     new[] { _variableLengthUnicodeString }},
                    {"char varying",               new[] { _variableLengthUnicodeString }},
                    {"character varying",          new[] { _variableLengthUnicodeString }},
                    {"national char varying",      new[] { _variableLengthUnicodeString }},
                    {"national character varying", new[] { _variableLengthUnicodeString }},

                    {"longchar",                   new[] { _unboundedUnicodeString }},
                    {"longtext",                   new[] { _unboundedUnicodeString }},
                    {"memo",                       new[] { _unboundedUnicodeString }},
                    {"note",                       new[] { _unboundedUnicodeString }},
                    {"ntext",                      new[] { _unboundedUnicodeString }},

                    {"guid",                       new[] { _guid }},
                    {"uniqueidentifier",           new[] { _guid }},

                    {"timestamp",                  new[] { _rowversion }},
                };

            _clrTypeMappings
                = new Dictionary<Type, RelationalTypeMapping>
                {
                    {typeof(bool), _bool},
                    {typeof(byte), _byte},
                    {typeof(short), _smallint},
                    {typeof(int), _integer},
                    {typeof(long), _bigint},
                    {typeof(float), _single},
                    {typeof(double), _double},
                    {typeof(decimal), _decimal}, // CHECK: Is this supported or do we need to use CURRENCY?
                    {typeof(DateTime), _datetime},
                    {typeof(DateOnly), _dateonly},
                    {typeof(DateTimeOffset), _datetimeoffset},
                    {typeof(TimeSpan), _timespan},
                    {typeof(TimeOnly), _timeonly},
                    {typeof(Guid), _guid},
                    { typeof(JsonElement), _jsonTypeMapping }
                };

            // These are disallowed only if specified without any kind of length specified in parenthesis.
            // This is because we don't try to make a new type from this string and any max length value
            // specified in the model, which means use of these strings is almost certainly an error, and
            // if it is not an error, then using, for example, varbinary(1) will work instead.
            _disallowedMappings
                = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                };

            _options = options;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override void ValidateMapping(CoreTypeMapping? mapping, IProperty? property)
        {
            if (mapping is RelationalTypeMapping relationalMapping)
            {
                if (_disallowedMappings.Contains(relationalMapping.StoreType))
                {
                    if (property == null)
                    {
                        throw new ArgumentException(JetStrings.UnqualifiedDataType(relationalMapping.StoreType));
                    }

                    throw new ArgumentException(
                        JetStrings.UnqualifiedDataTypeOnProperty(relationalMapping.StoreType, property.Name));
                }
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
            => base.FindMapping(mappingInfo)
               ?? FindRawMapping(mappingInfo)?.WithTypeMappingInfo(mappingInfo);

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        private RelationalTypeMapping? FindRawMapping(RelationalTypeMappingInfo mappingInfo)
        {
            var clrType = mappingInfo.ClrType;
            var storeTypeName = mappingInfo.StoreTypeName;

            if (storeTypeName != null)
            {
                var storeTypeNameBase = mappingInfo.StoreTypeNameBase;
                // If the TEXT store type is used with a size argument like `TEXT(n)`, it is handled as a synonym
                // for `VARCHAR(n)`.
                // If the TEXT store type is used without a size argument like `TEXT`, it is handled as a synonym
                // for `LONGCHAR`. 
                // See "Notes" in: https://support.office.com/en-us/article/equivalent-ansi-sql-data-types-7a0a6bef-ef25-45f9-8a9a-3c5f21b5c65d
                if (storeTypeNameBase!.Equals("text", StringComparison.OrdinalIgnoreCase) &&
                    !mappingInfo.IsFixedLength.GetValueOrDefault())
                {
                    if (mappingInfo.Size.GetValueOrDefault() > 0)
                    {
                        return clrType == null
                               || _variableLengthUnicodeString.ClrType == clrType
                            ? _variableLengthUnicodeString
                            : null;
                    }
                    else
                    {
                        return clrType == null
                               || _unboundedUnicodeString.ClrType == clrType
                            ? _unboundedUnicodeString
                            : null;
                    }
                }

                if (_storeTypeMappings.TryGetValue(storeTypeName, out var mappings)
                    || _storeTypeMappings.TryGetValue(storeTypeNameBase, out mappings))
                {
                    // We found the user-specified store type. No CLR type was provided - we're probably scaffolding from an existing database,
                    // take the first mapping as the default.
                    if (clrType is null)
                    {
                        return mappings[0];
                    }

                    // A CLR type was provided - look for a mapping between the store and CLR types. If not found, fail
                    // immediately.
                    foreach (var m in mappings)
                    {
                        if (m.ClrType == clrType)
                        {
                            return m;
                        }
                    }

                    return null;
                }

                // A store type name was provided, but is unknown. This could be a domain (alias) type, in which case
                // we proceed with a CLR type lookup (if the type doesn't exist at all the failure will come later).
            }

            if (clrType != null)
            {
                if (_clrTypeMappings.TryGetValue(clrType, out var mapping))
                {
                    return mapping;
                }

                if (clrType == typeof(string))
                {
                    var isFixedLength = mappingInfo.IsFixedLength == true;

                    const int maxCharColumnSize = 255;
                    const int maxIndexedCharColumnSize = 255;

                    var size = mappingInfo.Size ?? (mappingInfo.IsKeyOrIndex ? (int?)maxIndexedCharColumnSize : null);
                    bool extendtolongchar = false;
                    if (size > maxCharColumnSize)
                    {
                        size = isFixedLength ? maxCharColumnSize : (int?)null;
                        extendtolongchar = true;
                    }

                    if (_options.UseShortTextForSystemString && size == null && !extendtolongchar)

                    {
                        return new JetStringTypeMapping("varchar", unicode: true, size: 255);
                    }

                    return size == null
                        ? _unboundedUnicodeString
                        : new JetStringTypeMapping(
                            storeType: isFixedLength
                                ? _fixedLengthUnicodeString.StoreTypeNameBase
                                : _variableLengthUnicodeString.StoreTypeNameBase,
                            size: size,
                            unicode: true,
                            useKeyComparison: mappingInfo.IsKeyOrIndex);
                }

                if (clrType == typeof(byte[]))
                {
                    if (mappingInfo.IsRowVersion == true)
                    {
                        return _rowversion;
                    }

                    if (mappingInfo.ElementTypeMapping == null)
                    {
                        var isFixedLength = mappingInfo.IsFixedLength == true;

                        const int maxBinaryColumnSize = 510;

                        var size = mappingInfo.Size ?? (mappingInfo.IsKeyOrIndex ? (int?)maxBinaryColumnSize : null);
                        if (size > maxBinaryColumnSize)
                        {
                            size = isFixedLength ? maxBinaryColumnSize : (int?)null;
                        }

                        return size == null
                            ? _unboundedBinary
                            : new JetByteArrayTypeMapping(
                                size: size,
                                storeType: isFixedLength
                                    ? _fixedLengthBinary.StoreTypeNameBase
                                    : _variableLengthBinary.StoreTypeNameBase);
                    }
                }
            }

            return null;
        }

        private static readonly List<string> NameBasesUsingPrecision = new()
        {
            "decimal",
            "dec",
            "numeric"
        };

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override string? ParseStoreTypeName(
            string? storeTypeName,
            ref bool? unicode,
            ref int? size,
            ref int? precision,
            ref int? scale)
        {
            if (storeTypeName == null)
            {
                return null;
            }

            var originalSize = size;
            var parsedName = base.ParseStoreTypeName(storeTypeName, ref unicode, ref size, ref precision, ref scale);

            if (size.HasValue
                && NameBasesUsingPrecision.Any(n => storeTypeName.StartsWith(n, StringComparison.OrdinalIgnoreCase)))
            {
                precision = size;
                size = originalSize;
            }

            return parsedName;
        }
    }
}
