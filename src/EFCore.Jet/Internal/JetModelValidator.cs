﻿// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using EntityFrameworkCore.Jet.Metadata;
using EntityFrameworkCore.Jet.Metadata.Internal;

namespace EntityFrameworkCore.Jet.Internal
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
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </remarks>
    public class JetModelValidator(
        ModelValidatorDependencies dependencies,
        RelationalModelValidatorDependencies relationalDependencies) : RelationalModelValidator(dependencies, relationalDependencies)
    {

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override void Validate(IModel model, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            ValidateIndexIncludeProperties(model, logger);

            base.Validate(model, logger);

            ValidateDecimalColumns(model, logger);
            ValidateByteIdentityMapping(model, logger);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected virtual void ValidateDecimalColumns(
            IModel model,
            IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            foreach (IConventionProperty property in model.GetEntityTypes()
                .SelectMany(t => t.GetDeclaredProperties())
                .Where(
                    p => p.ClrType.UnwrapNullableType() == typeof(decimal)
                        && !p.IsForeignKey()))
            {
                var valueConverterConfigurationSource = property.GetValueConverterConfigurationSource();
                var valueConverterProviderType = property.GetValueConverter()?.ProviderClrType;
                if (!ConfigurationSource.Convention.Overrides(valueConverterConfigurationSource)
                    && typeof(decimal) != valueConverterProviderType)
                {
                    continue;
                }

                var columnTypeConfigurationSource = property.GetColumnTypeConfigurationSource();
                if (((columnTypeConfigurationSource == null
                            && ConfigurationSource.Convention.Overrides(property.GetTypeMappingConfigurationSource()))
                        || (columnTypeConfigurationSource != null
                            && ConfigurationSource.Convention.Overrides(columnTypeConfigurationSource)))
                    && (ConfigurationSource.Convention.Overrides(property.GetPrecisionConfigurationSource())
                        || ConfigurationSource.Convention.Overrides(property.GetScaleConfigurationSource())))
                {
                    logger.DecimalTypeDefaultWarning((IProperty)property);
                }

                if (property.IsKey())
                {
                    logger.DecimalTypeKeyWarning((IProperty)property);
                }
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected virtual void ValidateByteIdentityMapping(
            IModel model,
            IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            foreach (var entityType in model.GetEntityTypes())
            {
                foreach (var property in entityType.GetDeclaredProperties()
                    .Where(
                        p => p.ClrType.UnwrapNullableType() == typeof(byte)
                            && p.GetValueGenerationStrategy() == JetValueGenerationStrategy.IdentityColumn))
                {
                    logger.ByteIdentityColumnWarning(property);
                }
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override void ValidateValueGeneration(
            IEntityType entityType,
            IKey key,
            IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            if (entityType.GetTableName() != null
                && (string?)entityType[RelationalAnnotationNames.MappingStrategy] == RelationalAnnotationNames.TpcMappingStrategy)
            {
                foreach (var storeGeneratedProperty in key.Properties.Where(
                             p => (p.ValueGenerated & ValueGenerated.OnAdd) != 0
                                  && p.GetValueGenerationStrategy() == JetValueGenerationStrategy.IdentityColumn))
                {
                    logger.TpcStoreGeneratedIdentityWarning(storeGeneratedProperty);
                }
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected virtual void ValidateIndexIncludeProperties(
            IModel model,
            IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            foreach (var index in model.GetEntityTypes().SelectMany(t => t.GetDeclaredIndexes()))
            {
                var includeProperties = index.GetJetIncludeProperties();
                if (includeProperties?.Count > 0)
                {
                    var notFound = includeProperties
                        .FirstOrDefault(i => index.DeclaringEntityType.FindProperty(i) == null);

                    if (notFound != null)
                    {
                        throw new InvalidOperationException(
                            JetStrings.IncludePropertyNotFound(index.DeclaringEntityType.DisplayName(), notFound));
                    }

                    var duplicate = includeProperties
                        .GroupBy(i => i)
                        .Where(g => g.Count() > 1)
                        .Select(y => y.Key)
                        .FirstOrDefault();

                    if (duplicate != null)
                    {
                        throw new InvalidOperationException(
                            JetStrings.IncludePropertyDuplicated(index.DeclaringEntityType.DisplayName(), duplicate));
                    }

                    var inIndex = includeProperties
                        .FirstOrDefault(i => index.Properties.Any(p => i == p.Name));

                    if (inIndex != null)
                    {
                        throw new InvalidOperationException(
                            JetStrings.IncludePropertyInIndex(index.DeclaringEntityType.DisplayName(), inIndex));
                    }
                }
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override void ValidateSharedColumnsCompatibility(
            IReadOnlyList<IEntityType> mappedTypes,
            in StoreObjectIdentifier storeObject,
            IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            base.ValidateSharedColumnsCompatibility(mappedTypes, storeObject, logger);

            var identityColumns = new Dictionary<string, IProperty>();

            foreach (var property in mappedTypes.SelectMany(et => et.GetDeclaredProperties()))
            {
                var columnName = property.GetColumnName(storeObject);
                if (columnName == null)
                {
                    continue;
                }
                if (property.GetValueGenerationStrategy(storeObject) == JetValueGenerationStrategy.IdentityColumn)
                {
                    identityColumns[columnName] = property;
                }
            }

            if (identityColumns.Count > 1)
            {
                var sb = new StringBuilder()
                    .AppendJoin(identityColumns.Values.Select(p => "'" + p.DeclaringType.DisplayName() + "." + p.Name + "'"));
                throw new InvalidOperationException(JetStrings.MultipleIdentityColumns(sb, storeObject.DisplayName()));
            }
        }

        /// <inheritdoc />
        protected override void ValidateCompatible(
            IProperty property,
            IProperty duplicateProperty,
            string columnName,
            in StoreObjectIdentifier storeObject,
            IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            base.ValidateCompatible(property, duplicateProperty, columnName, storeObject, logger);

            var propertyStrategy = property.GetValueGenerationStrategy(storeObject);
            var duplicatePropertyStrategy = duplicateProperty.GetValueGenerationStrategy(storeObject);
            if (propertyStrategy != duplicatePropertyStrategy)
            {
                var isConflicting = ((IConventionProperty)property)
                                    .FindAnnotation(JetAnnotationNames.ValueGenerationStrategy)
                                    ?.GetConfigurationSource()
                                    == ConfigurationSource.Explicit
                                    || propertyStrategy != JetValueGenerationStrategy.None;
                var isDuplicateConflicting = ((IConventionProperty)duplicateProperty)
                                             .FindAnnotation(JetAnnotationNames.ValueGenerationStrategy)
                                             ?.GetConfigurationSource()
                                             == ConfigurationSource.Explicit
                                             || duplicatePropertyStrategy != JetValueGenerationStrategy.None;

                if (isConflicting && isDuplicateConflicting)
                {
                    throw new InvalidOperationException(
                        JetStrings.DuplicateColumnNameValueGenerationStrategyMismatch(
                            duplicateProperty.DeclaringType.DisplayName(),
                            duplicateProperty.Name,
                            property.DeclaringType.DisplayName(),
                            property.Name,
                            columnName,
                            storeObject.DisplayName()));
                }
            }
            else
            {
                switch (propertyStrategy)
                {
                    case JetValueGenerationStrategy.IdentityColumn:
                        var increment = property.GetJetIdentityIncrement(storeObject);
                        var duplicateIncrement = duplicateProperty.GetJetIdentityIncrement(storeObject);
                        if (increment != duplicateIncrement)
                        {
                            throw new InvalidOperationException(
                                JetStrings.DuplicateColumnIdentityIncrementMismatch(
                                    duplicateProperty.DeclaringType.DisplayName(),
                                    duplicateProperty.Name,
                                    property.DeclaringType.DisplayName(),
                                    property.Name,
                                    columnName,
                                    storeObject.DisplayName()));
                        }

                        var seed = property.GetJetIdentitySeed(storeObject);
                        var duplicateSeed = duplicateProperty.GetJetIdentitySeed(storeObject);
                        if (seed != duplicateSeed)
                        {
                            throw new InvalidOperationException(
                                JetStrings.DuplicateColumnIdentitySeedMismatch(
                                    duplicateProperty.DeclaringType.DisplayName(),
                                    duplicateProperty.Name,
                                    property.DeclaringType.DisplayName(),
                                    property.Name,
                                    columnName,
                                    storeObject.DisplayName()));
                        }

                        break;
                }
            }
        }
    }
}