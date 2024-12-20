﻿using System.Globalization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace EntityFrameworkCore.Jet.Metadata.Internal
{
    /// <summary>
    ///     <para>
    ///         This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///         the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///         any release. You should only use it directly in your code with extreme caution and knowing that
    ///         doing so can result in application failures when updating to a new Entity Framework Core release.
    ///     </para>
    ///     <para>
    ///         The service lifetime is <see cref="ServiceLifetime.Singleton" />. This means a single instance
    ///         is used by many <see cref="DbContext" /> instances. The implementation must be thread-safe.
    ///         This service cannot depend on services registered as <see cref="ServiceLifetime.Scoped" />.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     Initializes a new instance of this class.
    /// </remarks>
    /// <param name="dependencies"> Parameter object containing dependencies for this service. </param>
    public class JetAnnotationProvider(RelationalAnnotationProviderDependencies dependencies) : RelationalAnnotationProvider(dependencies)
    {

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override IEnumerable<IAnnotation> For(ITableIndex index, bool designTime)
        {
            if (!designTime)
            {
                yield break;
            }

            // Model validation ensures that these facets are the same on all mapped indexes
            var modelIndex = index.MappedIndexes.First();

            var table = index.Table;

            var includeProperties = modelIndex.GetJetIncludeProperties();
            if (includeProperties != null)
            {
                var includeColumns = (IReadOnlyList<string?>)includeProperties
                    .Select(
                        p => modelIndex.DeclaringEntityType.FindProperty(p)!
                            .GetColumnName(StoreObjectIdentifier.Table(table.Name, table.Schema)))
                    .ToArray();

                yield return new Annotation(
                    JetAnnotationNames.Include,
                    includeColumns);
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override IEnumerable<IAnnotation> For(IColumn column, bool designTime)
        {
            //Need to do this in both design and runtime
            if (!designTime)
            {
                yield break;
            }

            var table = StoreObjectIdentifier.Table(column.Table.Name, column.Table.Schema);
            var property = column.PropertyMappings
                .Select(m => m.Property)
                .FirstOrDefault(
                    p => p.GetValueGenerationStrategy(table)
                         == JetValueGenerationStrategy.IdentityColumn);
            if (property != null)
            {
                var seed = property.GetJetIdentitySeed(table);
                var increment = property.GetJetIdentityIncrement(table);

                yield return new Annotation(
                    JetAnnotationNames.Identity,
                    string.Format(CultureInfo.InvariantCulture, "{0}, {1}", seed ?? 1, increment ?? 1));
            }
            else
            {
                if (column is JsonColumn) yield break;
                property = column.PropertyMappings.First().Property;
                if (property.DeclaringType is IEntityType entityType)
                {
                    // Only return auto increment for integer single column primary key
                    var primaryKey = entityType.FindPrimaryKey();
                    if (primaryKey != null
                        && primaryKey.Properties.Count == 1
                        && primaryKey.Properties[0] == property
                        && property.ValueGenerated == ValueGenerated.OnAdd
                        && property.ClrType.UnwrapNullableType().IsInteger()
                        && !HasConverter(property))
                    {
                        yield return new Annotation(
                            JetAnnotationNames.Identity,
                            string.Format(CultureInfo.InvariantCulture, "{0}, {1}", 1, 1));
                    }
                }
            }
        }

        private static bool HasConverter(IProperty property)
            => (property.GetValueConverter() ?? property.FindTypeMapping()?.Converter) != null;
    }
}