// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using EntityFrameworkCore.Jet.FunctionalTests.TestUtilities;
using EntityFrameworkCore.Jet.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using EntityFrameworkCore.Jet.Internal;
using EntityFrameworkCore.Jet.Metadata;
using EntityFrameworkCore.Jet.Metadata.Internal;
using EntityFrameworkCore.Jet.Migrations.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;
using Assert = Xunit.Assert;

// ReSharper disable InconsistentNaming
namespace EntityFrameworkCore.Jet.FunctionalTests
{
    public class JetMigrationsSqlGeneratorTest : MigrationsSqlGeneratorTestBase
    {
        [ConditionalFact]
        public virtual void AddColumnOperation_identity_legacy()
        {
            Generate(
                new AddColumnOperation
                {
                    Table = "People",
                    Name = "Id",
                    ClrType = typeof(int),
                    ColumnType = "int",
                    DefaultValue = 0,
                    IsNullable = false,
                    [JetAnnotationNames.ValueGenerationStrategy] =
                        JetValueGenerationStrategy.IdentityColumn
                });

            AssertSql(
                @"ALTER TABLE `People` ADD `Id` counter NOT NULL;
");
        }

        public override void AddColumnOperation_without_column_type()
        {
            base.AddColumnOperation_without_column_type();

            AssertSql(
                @"ALTER TABLE `People` ADD `Alias` nvarchar(max) NOT NULL;
");
        }

        public override void AddColumnOperation_with_unicode_no_model()
        {
            base.AddColumnOperation_with_unicode_no_model();

            AssertSql(
                @"ALTER TABLE `Person` ADD `Name` varchar(max) NULL;
");
        }

        public override void AddColumnOperation_with_fixed_length_no_model()
        {
            base.AddColumnOperation_with_fixed_length_no_model();

            AssertSql(
                @"ALTER TABLE `Person` ADD `Name` char(100) NULL;
");
        }

        public override void AddColumnOperation_with_maxLength_no_model()
        {
            base.AddColumnOperation_with_maxLength_no_model();

            AssertSql(
                @"ALTER TABLE `Person` ADD `Name` varchar(30) NULL;
");
        }

        public override void AddColumnOperation_with_maxLength_overridden()
        {
            base.AddColumnOperation_with_maxLength_overridden();

            AssertSql(
                @"ALTER TABLE `Person` ADD `Name` varchar(32) NULL;
");
        }

        public override void AddColumnOperation_with_precision_and_scale_overridden()
        {
            base.AddColumnOperation_with_precision_and_scale_overridden();

            AssertSql(
                @"ALTER TABLE `Person` ADD `Pi` decimal(15,10) NOT NULL;
");
        }

        public override void AddColumnOperation_with_precision_and_scale_no_model()
        {
            base.AddColumnOperation_with_precision_and_scale_no_model();

            AssertSql(
                @"ALTER TABLE `Person` ADD `Pi` decimal(20,7) NOT NULL;
");
        }

        public override void AddColumnOperation_with_unicode_overridden()
        {
            base.AddColumnOperation_with_unicode_overridden();

            AssertSql(
                @"ALTER TABLE `Person` ADD `Name` nvarchar(max) NULL;
");
        }

        [ConditionalFact]
        public virtual void AddColumnOperation_with_rowversion_overridden()
        {
            Generate(
                modelBuilder => modelBuilder.Entity<Person>().Property<byte[]>("RowVersion"),
                new AddColumnOperation
                {
                    Table = "Person",
                    Name = "RowVersion",
                    ClrType = typeof(byte[]),
                    IsRowVersion = true,
                    IsNullable = true
                });

            AssertSql(
                @"ALTER TABLE `Person` ADD `RowVersion` rowversion NULL;
");
        }

        [ConditionalFact]
        public virtual void AddColumnOperation_with_rowversion_no_model()
        {
            Generate(
                new AddColumnOperation
                {
                    Table = "Person",
                    Name = "RowVersion",
                    ClrType = typeof(byte[]),
                    IsRowVersion = true,
                    IsNullable = true
                });

            AssertSql(
                @"ALTER TABLE `Person` ADD `RowVersion` rowversion NULL;
");
        }

        public override void AlterColumnOperation_without_column_type()
        {
            base.AlterColumnOperation_without_column_type();

            AssertSql(
                @"DECLARE @var0 sysname;
SELECT @var0 = [d].`Name`
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'`People`') AND [c].`Name` = 'LuckyNumber');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE `People` DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE `People` ALTER COLUMN [LuckyNumber] int NOT NULL;
");
        }

        public override void AddForeignKeyOperation_without_principal_columns()
        {
            base.AddForeignKeyOperation_without_principal_columns();

            AssertSql(
                @"ALTER TABLE `People` ADD FOREIGN KEY (`SpouseId`) REFERENCES `People`;
");
        }

        [ConditionalFact]
        public virtual void AlterColumnOperation_with_identity_legacy()
        {
            Generate(
                new AlterColumnOperation
                {
                    Table = "People",
                    Name = "Id",
                    ClrType = typeof(int),
                    [JetAnnotationNames.ValueGenerationStrategy] =
                        JetValueGenerationStrategy.IdentityColumn
                });

            AssertSql(
                @"DECLARE @var0 sysname;
SELECT @var0 = [d].`Name`
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'`People`') AND [c].`Name` = 'Id');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE `People` DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE `People` ALTER COLUMN [Id] int NOT NULL;
");
        }

        [ConditionalFact]
        public virtual void AlterColumnOperation_with_index_no_oldColumn()
        {
            Generate(
                modelBuilder => modelBuilder
                    .HasAnnotation(CoreAnnotationNames.ProductVersion, "1.0.0-rtm")
                    .Entity<Person>(
                        x =>
                        {
                            x.Property<string>("Name").HasMaxLength(30);
                            x.HasIndex("Name");
                        }),
                new AlterColumnOperation
                {
                    Table = "Person",
                    Name = "Name",
                    ClrType = typeof(string),
                    MaxLength = 30,
                    IsNullable = true,
                    OldColumn = new AddColumnOperation()
                });

            AssertSql(
                @"DECLARE @var0 sysname;
SELECT @var0 = [d].`Name`
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'`Person`') AND [c].`Name` = 'Name');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE `Person` DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE `Person` ALTER COLUMN `Name` nvarchar(30) NULL;
");
        }

        [ConditionalFact]
        public virtual void AlterColumnOperation_with_added_index()
        {
            Generate(
                modelBuilder => modelBuilder
                    .HasAnnotation(CoreAnnotationNames.ProductVersion, "1.1.0")
                    .Entity<Person>(
                        x =>
                        {
                            x.Property<string>("Name").HasMaxLength(30);
                            x.HasIndex("Name");
                        }),
                new AlterColumnOperation
                {
                    Table = "Person",
                    Name = "Name",
                    ClrType = typeof(string),
                    MaxLength = 30,
                    IsNullable = true,
                    OldColumn = new AddColumnOperation { ClrType = typeof(string), IsNullable = true }
                },
                new CreateIndexOperation
                {
                    Name = "IX_Person_Name",
                    Table = "Person",
                    Columns = new[] { "Name" }
                });

            AssertSql(
                @"DECLARE @var0 sysname;
SELECT @var0 = [d].`Name`
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'`Person`') AND [c].`Name` = 'Name');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE `Person` DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE `Person` ALTER COLUMN `Name` nvarchar(30) NULL;
GO

CREATE INDEX [IX_Person_Name] ON `Person` (`Name`);
");
        }

        [ConditionalFact]
        public virtual void AlterColumnOperation_with_added_index_no_oldType()
        {
            Generate(
                modelBuilder => modelBuilder
                    .HasAnnotation(CoreAnnotationNames.ProductVersion, "2.1.0")
                    .Entity<Person>(
                        x =>
                        {
                            x.Property<string>("Name");
                            x.HasIndex("Name");
                        }),
                new AlterColumnOperation
                {
                    Table = "Person",
                    Name = "Name",
                    ClrType = typeof(string),
                    IsNullable = true,
                    OldColumn = new AddColumnOperation { ClrType = typeof(string), IsNullable = true }
                },
                new CreateIndexOperation
                {
                    Name = "IX_Person_Name",
                    Table = "Person",
                    Columns = new[] { "Name" }
                });

            AssertSql(
                @"DECLARE @var0 sysname;
SELECT @var0 = [d].`Name`
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'`Person`') AND [c].`Name` = 'Name');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE `Person` DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE `Person` ALTER COLUMN `Name` nvarchar(450) NULL;
GO

CREATE INDEX [IX_Person_Name] ON `Person` (`Name`);
");
        }

        [ConditionalFact]
        public virtual void AlterColumnOperation_identity_legacy()
        {
            Generate(
                modelBuilder => modelBuilder.HasAnnotation(CoreAnnotationNames.ProductVersion, "1.1.0"),
                new AlterColumnOperation
                {
                    Table = "Person",
                    Name = "Id",
                    ClrType = typeof(long),
                    [JetAnnotationNames.ValueGenerationStrategy] = JetValueGenerationStrategy.IdentityColumn,
                    OldColumn = new AddColumnOperation
                    {
                        ClrType = typeof(int),
                        [JetAnnotationNames.ValueGenerationStrategy] = JetValueGenerationStrategy.IdentityColumn
                    }
                });

            AssertSql(
                @"ALTER TABLE `Person` DROP CONSTRAINT [' + @var0 + '];
ALTER TABLE `Person` ALTER COLUMN [Id] bigint NOT NULL;
");
        }

        [ConditionalFact]
        public virtual void AlterColumnOperation_add_identity_legacy()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => Generate(
                    modelBuilder => modelBuilder.HasAnnotation(CoreAnnotationNames.ProductVersion, "1.1.0"),
                    new AlterColumnOperation
                    {
                        Table = "Person",
                        Name = "Id",
                        ClrType = typeof(int),
                        [JetAnnotationNames.ValueGenerationStrategy] = JetValueGenerationStrategy.IdentityColumn,
                        OldColumn = new AddColumnOperation { ClrType = typeof(int) }
                    }));

            Assert.Equal(JetStrings.AlterIdentityColumn, ex.Message);
        }

        [ConditionalFact]
        public virtual void AlterColumnOperation_remove_identity_legacy()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => Generate(
                    modelBuilder => modelBuilder.HasAnnotation(CoreAnnotationNames.ProductVersion, "1.1.0"),
                    new AlterColumnOperation
                    {
                        Table = "Person",
                        Name = "Id",
                        ClrType = typeof(int),
                        OldColumn = new AddColumnOperation
                        {
                            ClrType = typeof(int),
                            [JetAnnotationNames.ValueGenerationStrategy] = JetValueGenerationStrategy.IdentityColumn
                        }
                    }));

            Assert.Equal(JetStrings.AlterIdentityColumn, ex.Message);
        }

        [ConditionalFact]
        public virtual void CreateDatabaseOperation()
        {
            Generate(
                new JetCreateDatabaseOperation { Name = "Northwind" });

            AssertSql(
                @"CREATE DATABASE `Northwind`;
");
        }

//         [ConditionalFact]
//         public virtual void CreateDatabaseOperation_with_filename()
//         {
//             Generate(
//                 new JetCreateDatabaseOperation { Name = "Northwind", FileName = "Narf.mdf" });
//
//             var expectedFile = Path.GetFullPath("Narf.mdf");
//             var expectedLog = Path.GetFullPath("Narf_log.ldf");
//
//             AssertSql(
//                 $@"CREATE DATABASE [Northwind]
// ON (NAME = 'Narf', FILENAME = '{expectedFile}')
// LOG ON (NAME = 'Narf_log', FILENAME = '{expectedLog}');
// GO
//
// IF SERVERPROPERTY('EngineEdition') <> 5
// BEGIN
//     ALTER DATABASE [Northwind] SET READ_COMMITTED_SNAPSHOT ON;
// END;
// ");
//         }

//         [ConditionalFact]
//         public virtual void CreateDatabaseOperation_with_filename_and_datadirectory()
//         {
//             var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
//
//             Generate(
//                 new JetCreateDatabaseOperation { Name = "Northwind", FileName = "|DataDirectory|Narf.mdf" });
//
//             var expectedFile = Path.Combine(baseDirectory, "Narf.mdf");
//             var expectedLog = Path.Combine(baseDirectory, "Narf_log.ldf");
//
//             AssertSql(
//                 $@"CREATE DATABASE [Northwind]
// ON (NAME = 'Narf', FILENAME = '{expectedFile}')
// LOG ON (NAME = 'Narf_log', FILENAME = '{expectedLog}');
// GO
//
// IF SERVERPROPERTY('EngineEdition') <> 5
// BEGIN
//     ALTER DATABASE [Northwind] SET READ_COMMITTED_SNAPSHOT ON;
// END;
// ");
//         }

//         [ConditionalFact]
//         public virtual void CreateDatabaseOperation_with_filename_and_custom_datadirectory()
//         {
//             var dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
//
//             AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);
//
//             Generate(
//                 new JetCreateDatabaseOperation { Name = "Northwind", FileName = "|DataDirectory|Narf.mdf" });
//
//             AppDomain.CurrentDomain.SetData("DataDirectory", null);
//
//             var expectedFile = Path.Combine(dataDirectory, "Narf.mdf");
//             var expectedLog = Path.Combine(dataDirectory, "Narf_log.ldf");
//
//             AssertSql(
//                 $@"CREATE DATABASE [Northwind]
// ON (NAME = 'Narf', FILENAME = '{expectedFile}')
// LOG ON (NAME = 'Narf_log', FILENAME = '{expectedLog}');
// GO
//
// IF SERVERPROPERTY('EngineEdition') <> 5
// BEGIN
//     ALTER DATABASE [Northwind] SET READ_COMMITTED_SNAPSHOT ON;
// END;
// ");
//         }

//         [ConditionalFact]
//         public virtual void CreateDatabaseOperation_with_collation()
//         {
//             Generate(
//                 new JetCreateDatabaseOperation { Name = "Northwind", Collation = "German_PhoneBook_CI_AS" });
//
//             AssertSql(
//                 @"CREATE DATABASE [Northwind]
// COLLATE German_PhoneBook_CI_AS;
// GO
//
// IF SERVERPROPERTY('EngineEdition') <> 5
// BEGIN
//     ALTER DATABASE [Northwind] SET READ_COMMITTED_SNAPSHOT ON;
// END;
// ");
//         }

        [ConditionalFact]
        public virtual void AlterDatabaseOperation_collation()
        {
            Generate(
                new AlterDatabaseOperation { Collation = "German_PhoneBook_CI_AS" });

            Assert.Contains(
                "COLLATE German_PhoneBook_CI_AS",
                Sql);
        }

        // [ConditionalFact]
        // public virtual void AlterDatabaseOperation_memory_optimized()
        // {
        //     Generate(
        //         new AlterDatabaseOperation { [JetAnnotationNames.MemoryOptimized] = true });
        //
        //     Assert.Contains(
        //         "CONTAINS MEMORY_OPTIMIZED_DATA;",
        //         Sql);
        // }

        [ConditionalFact]
        public virtual void DropDatabaseOperation()
        {
            Generate(
                new JetDropDatabaseOperation { Name = "Northwind" });

            AssertSql(
                @"DROP DATABASE `Northwind`;
");
        }

        [ConditionalFact(Skip = "Jet does not support sequences")]
        public virtual void MoveSequenceOperation_legacy()
        {
            Generate(
                new RenameSequenceOperation
                {
                    Name = "EntityFrameworkHiLoSequence",
                    Schema = "dbo",
                    NewSchema = "my"
                });

            AssertSql(
                @"ALTER SCHEMA [my] TRANSFER [dbo].[EntityFrameworkHiLoSequence];
");
        }

        [ConditionalFact]
        public virtual void MoveTableOperation_legacy()
        {
            Generate(
                new RenameTableOperation
                {
                    Name = "People",
                    Schema = "dbo",
                    NewSchema = "hr"
                });

            AssertSql(
                @"ALTER SCHEMA [hr] TRANSFER [dbo].`People`;
");
        }

        [ConditionalFact]
        public virtual void RenameIndexOperations_throws_when_no_table()
        {
            var migrationBuilder = new MigrationBuilder("Jet");

            migrationBuilder.RenameIndex(
                name: "IX_OldIndex",
                newName: "IX_NewIndex");

            var ex = Assert.Throws<InvalidOperationException>(
                () => Generate(migrationBuilder.Operations.ToArray()));

            Assert.Equal(JetStrings.IndexTableRequired, ex.Message);
        }

        [ConditionalFact(Skip = "Jet does not support sequences")]
        public virtual void RenameSequenceOperation_legacy()
        {
            Generate(
                new RenameSequenceOperation
                {
                    Name = "EntityFrameworkHiLoSequence",
                    Schema = "dbo",
                    NewName = "MySequence"
                });

            AssertSql(
                @"EXEC sp_rename N'[dbo].[EntityFrameworkHiLoSequence]', N'MySequence';
");
        }

        [ConditionalFact]
        public override void RenameTableOperation_legacy()
        {
            base.RenameTableOperation_legacy();

            AssertSql(
                @"ALTER TABLE `People` RENAME TO `Person`;
");
        }

        public override void RenameTableOperation()
        {
            base.RenameTableOperation();

            AssertSql(
                @"ALTER TABLE `People` RENAME TO `Person`;
");
        }

        [ConditionalFact]
        public virtual void SqlOperation_handles_backslash()
        {
            Generate(
                new SqlOperation { Sql = @"-- Multiline \" + EOL + "comment" });

            AssertSql(
                @"-- Multiline comment
");
        }

        [ConditionalFact]
        public virtual void SqlOperation_ignores_sequential_gos()
        {
            Generate(
                new SqlOperation { Sql = "-- Ready set" + EOL + "GO" + EOL + "GO" });

            AssertSql(
                @"-- Ready set
");
        }

        [ConditionalFact]
        public virtual void SqlOperation_handles_go()
        {
            Generate(
                new SqlOperation { Sql = "-- I" + EOL + "go" + EOL + "-- Too" });

            AssertSql(
                @"-- I
GO

-- Too
");
        }

        [ConditionalFact]
        public virtual void SqlOperation_handles_go_with_count()
        {
            Generate(
                new SqlOperation { Sql = "-- I" + EOL + "GO 2" });

            AssertSql(
                @"-- I
GO

-- I
");
        }

        [ConditionalFact]
        public virtual void SqlOperation_ignores_non_go()
        {
            Generate(
                new SqlOperation { Sql = "-- I GO 2" });

            AssertSql(
                @"-- I GO 2
");
        }

        public override void SqlOperation()
        {
            base.SqlOperation();

            AssertSql(
                @"-- I <3 DDL
");
        }

        public override void InsertDataOperation_all_args_spatial()
        {
            base.InsertDataOperation_all_args_spatial();

            AssertSql(
                @"IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE `Name` IN (N'Id', N'Full Name', N'Geometry') AND [object_id] = OBJECT_ID(N'[dbo].`People`'))
    SET IDENTITY_INSERT [dbo].`People` ON;
INSERT INTO [dbo].`People` ([Id], [Full Name], [Geometry])
VALUES (0, NULL, NULL),
(1, 'Daenerys Targaryen', NULL),
(2, 'John Snow', NULL),
(3, 'Arya Stark', NULL),
(4, 'Harry Strickland', NULL),
(5, 'The Imp', NULL),
(6, 'The Kingslayer', NULL),
(7, 'Aemon Targaryen', geography::Parse('GEOMETRYCOLLECTION (LINESTRING (1.1 2.2 NULL, 2.2 2.2 NULL, 2.2 1.1 NULL, 7.1 7.2 NULL), LINESTRING (7.1 7.2 NULL, 20.2 20.2 NULL, 20.2 1.1 NULL, 70.1 70.2 NULL), MULTIPOINT ((1.1 2.2 NULL), (2.2 2.2 NULL), (2.2 1.1 NULL)), POLYGON ((1.1 2.2 NULL, 2.2 2.2 NULL, 2.2 1.1 NULL, 1.1 2.2 NULL)), POLYGON ((10.1 20.2 NULL, 20.2 20.2 NULL, 20.2 10.1 NULL, 10.1 20.2 NULL)), POINT (1.1 2.2 3.3), MULTILINESTRING ((1.1 2.2 NULL, 2.2 2.2 NULL, 2.2 1.1 NULL, 7.1 7.2 NULL), (7.1 7.2 NULL, 20.2 20.2 NULL, 20.2 1.1 NULL, 70.1 70.2 NULL)), MULTIPOLYGON (((10.1 20.2 NULL, 20.2 20.2 NULL, 20.2 10.1 NULL, 10.1 20.2 NULL)), ((1.1 2.2 NULL, 2.2 2.2 NULL, 2.2 1.1 NULL, 1.1 2.2 NULL))))'));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE `Name` IN (N'Id', N'Full Name', N'Geometry') AND [object_id] = OBJECT_ID(N'[dbo].`People`'))
    SET IDENTITY_INSERT [dbo].`People` OFF;
");
        }

        // The test data we're using is geographic but is represented in NTS as a GeometryCollection
        protected override string GetGeometryCollectionStoreType()
            => "geography";

        public override void InsertDataOperation_required_args()
        {
            base.InsertDataOperation_required_args();

            AssertSql(
                @"IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE `Name` IN (N'First Name') AND [object_id] = OBJECT_ID(N'`People`'))
    SET IDENTITY_INSERT `People` ON;
INSERT INTO `People` (`First Name`)
VALUES (N'John');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE `Name` IN (N'First Name') AND [object_id] = OBJECT_ID(N'`People`'))
    SET IDENTITY_INSERT `People` OFF;
");
        }

        public override void InsertDataOperation_required_args_composite()
        {
            base.InsertDataOperation_required_args_composite();

            AssertSql(
                @"IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE `Name` IN (N'First Name', N'Last Name') AND [object_id] = OBJECT_ID(N'`People`'))
    SET IDENTITY_INSERT `People` ON;
INSERT INTO `People` (`First Name`, `Last Name`)
VALUES (N'John', N'Snow');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE `Name` IN (N'First Name', N'Last Name') AND [object_id] = OBJECT_ID(N'`People`'))
    SET IDENTITY_INSERT `People` OFF;
");
        }

        public override void InsertDataOperation_required_args_multiple_rows()
        {
            base.InsertDataOperation_required_args_multiple_rows();

            AssertSql(
                @"IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE `Name` IN (N'First Name') AND [object_id] = OBJECT_ID(N'`People`'))
    SET IDENTITY_INSERT `People` ON;
INSERT INTO `People` (`First Name`)
VALUES (N'John'),
(N'Daenerys');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE `Name` IN (N'First Name') AND [object_id] = OBJECT_ID(N'`People`'))
    SET IDENTITY_INSERT `People` OFF;
");
        }

        public override void InsertDataOperation_throws_for_unsupported_column_types()
        {
            base.InsertDataOperation_throws_for_unsupported_column_types();
        }

        public override void DeleteDataOperation_all_args()
        {
            base.DeleteDataOperation_all_args();

            AssertSql(
                @"DELETE FROM `People`
WHERE `First Name` = 'Hodor';
SELECT @@ROWCOUNT;

DELETE FROM `People`
WHERE `First Name` = 'Daenerys';
SELECT @@ROWCOUNT;

DELETE FROM `People`
WHERE `First Name` = 'John';
SELECT @@ROWCOUNT;

DELETE FROM `People`
WHERE `First Name` = 'Arya';
SELECT @@ROWCOUNT;

DELETE FROM `People`
WHERE `First Name` = 'Harry';
SELECT @@ROWCOUNT;

");
        }

        public override void DeleteDataOperation_all_args_composite()
        {
            base.DeleteDataOperation_all_args_composite();

            AssertSql(
                @"DELETE FROM `People`
WHERE `First Name` = 'Hodor' AND `Last Name` IS NULL;
SELECT @@ROWCOUNT;

DELETE FROM `People`
WHERE `First Name` = 'Daenerys' AND `Last Name` = 'Targaryen';
SELECT @@ROWCOUNT;

DELETE FROM `People`
WHERE `First Name` = 'John' AND `Last Name` = 'Snow';
SELECT @@ROWCOUNT;

DELETE FROM `People`
WHERE `First Name` = 'Arya' AND `Last Name` = 'Stark';
SELECT @@ROWCOUNT;

DELETE FROM `People`
WHERE `First Name` = 'Harry' AND `Last Name` = 'Strickland';
SELECT @@ROWCOUNT;

");
        }

        public override void DeleteDataOperation_required_args()
        {
            base.DeleteDataOperation_required_args();

            AssertSql(
                @"DELETE FROM `People`
WHERE `Last Name` = 'Snow';
SELECT @@ROWCOUNT;

");
        }

        public override void DeleteDataOperation_required_args_composite()
        {
            base.DeleteDataOperation_required_args_composite();

            AssertSql(
                @"DELETE FROM `People`
WHERE `First Name` = 'John' AND `Last Name` = 'Snow';
SELECT @@ROWCOUNT;

");
        }

        public override void UpdateDataOperation_all_args()
        {
            base.UpdateDataOperation_all_args();

            AssertSql(
                @"UPDATE `People` SET `Birthplace` = 'Winterfell', `House Allegiance` = 'Stark', `Culture` = 'Northmen'
WHERE `First Name` = 'Hodor';
SELECT @@ROWCOUNT;

UPDATE `People` SET `Birthplace` = 'Dragonstone', `House Allegiance` = 'Targaryen', `Culture` = 'Valyrian'
WHERE `First Name` = 'Daenerys';
SELECT @@ROWCOUNT;

");
        }

        public override void UpdateDataOperation_all_args_composite()
        {
            base.UpdateDataOperation_all_args_composite();

            AssertSql(
                @"UPDATE `People` SET `House Allegiance` = 'Stark'
WHERE `First Name` = 'Hodor' AND `Last Name` IS NULL;
SELECT @@ROWCOUNT;

UPDATE `People` SET `House Allegiance` = 'Targaryen'
WHERE `First Name` = 'Daenerys' AND `Last Name` = 'Targaryen';
SELECT @@ROWCOUNT;

");
        }

        public override void UpdateDataOperation_all_args_composite_multi()
        {
            base.UpdateDataOperation_all_args_composite_multi();

            AssertSql(
                @"UPDATE `People` SET `Birthplace` = 'Winterfell', `House Allegiance` = 'Stark', `Culture` = 'Northmen'
WHERE `First Name` = 'Hodor' AND `Last Name` IS NULL;
SELECT @@ROWCOUNT;

UPDATE `People` SET `Birthplace` = 'Dragonstone', `House Allegiance` = 'Targaryen', `Culture` = 'Valyrian'
WHERE `First Name` = 'Daenerys' AND `Last Name` = 'Targaryen';
SELECT @@ROWCOUNT;

");
        }

        public override void UpdateDataOperation_all_args_multi()
        {
            base.UpdateDataOperation_all_args_multi();

            AssertSql(
                @"UPDATE `People` SET `Birthplace` = 'Dragonstone', `House Allegiance` = 'Targaryen', `Culture` = 'Valyrian'
WHERE `First Name` = 'Daenerys';
SELECT @@ROWCOUNT;

");
        }

        public override void UpdateDataOperation_required_args()
        {
            base.UpdateDataOperation_required_args();

            AssertSql(
                @"UPDATE `People` SET `House Allegiance` = 'Targaryen'
WHERE `First Name` = 'Daenerys';
SELECT @@ROWCOUNT;

");
        }

        public override void UpdateDataOperation_required_args_composite()
        {
            base.UpdateDataOperation_required_args_composite();

            AssertSql(
                @"UPDATE `People` SET `House Allegiance` = 'Targaryen'
WHERE `First Name` = 'Daenerys' AND `Last Name` = 'Targaryen';
SELECT @@ROWCOUNT;

");
        }

        public override void UpdateDataOperation_required_args_composite_multi()
        {
            base.UpdateDataOperation_required_args_composite_multi();

            AssertSql(
                @"UPDATE `People` SET `Birthplace` = 'Dragonstone', `House Allegiance` = 'Targaryen', `Culture` = 'Valyrian'
WHERE `First Name` = 'Daenerys' AND `Last Name` = 'Targaryen';
SELECT @@ROWCOUNT;

");
        }

        public override void UpdateDataOperation_required_args_multi()
        {
            base.UpdateDataOperation_required_args_multi();

            AssertSql(
                @"UPDATE `People` SET `Birthplace` = 'Dragonstone', `House Allegiance` = 'Targaryen', `Culture` = 'Valyrian'
WHERE `First Name` = 'Daenerys';
SELECT @@ROWCOUNT;

");
        }

        public override void UpdateDataOperation_required_args_multiple_rows()
        {
            base.UpdateDataOperation_required_args_multiple_rows();

            AssertSql(
                @"UPDATE `People` SET `House Allegiance` = 'Stark'
WHERE `First Name` = 'Hodor';
SELECT @@ROWCOUNT;

UPDATE `People` SET `House Allegiance` = 'Targaryen'
WHERE `First Name` = 'Daenerys';
SELECT @@ROWCOUNT;

");
        }

        public override void DefaultValue_with_line_breaks(bool isUnicode)
        {
            base.DefaultValue_with_line_breaks(isUnicode);

            var storeType = isUnicode ? "nvarchar(max)" : "varchar(max)";
            var unicodePrefix = isUnicode ? "N" : string.Empty;
            var expectedSql = @$"CREATE TABLE `TestLineBreaks` (
    [TestDefaultValue] {storeType} NOT NULL DEFAULT CONCAT({unicodePrefix}CHAR(13), {unicodePrefix}CHAR(10), {unicodePrefix}'Various Line', {unicodePrefix}CHAR(13), {unicodePrefix}'Breaks', {unicodePrefix}CHAR(10))
);
";
            AssertSql(expectedSql);
        }

        [ConditionalFact]
        public virtual void AddColumn_generates_exec_when_computed_and_idempotent()
        {
            Generate(
                modelBuilder => { },
                migrationBuilder => migrationBuilder.AddColumn<int>(
                    name: "Column2",
                    table: "Table1",
                    computedColumnSql: "[Column1] + 1"),
                MigrationsSqlGenerationOptions.Idempotent);

            AssertSql(
                @"EXEC(N'ALTER TABLE [Table1] ADD [Column2] AS [Column1] + 1');
");
        }

        [ConditionalFact]
        public virtual void AddCheckConstraint_generates_exec_when_idempotent()
        {
            Generate(
                modelBuilder => { },
                migrationBuilder => migrationBuilder.AddCheckConstraint(
                    name: "CK_Table1",
                    table: "Table1",
                    "[Column1] BETWEEN 0 AND 100"),
                MigrationsSqlGenerationOptions.Idempotent);

            AssertSql(
                @"ALTER TABLE `Table1` ADD CONSTRAINT `CK_Table1` CHECK ([Column1] BETWEEN 0 AND 100);
");
        }

        [ConditionalFact]
        public virtual void CreateIndex_generates_exec_when_filter_and_idempotent()
        {
            Generate(
                modelBuilder => { },
                migrationBuilder => migrationBuilder.CreateIndex(
                    name: "IX_Table1_Column1",
                    table: "Table1",
                    column: "Column1",
                    filter: "[Column1] IS NOT NULL"),
                MigrationsSqlGenerationOptions.Idempotent);

            AssertSql(
                @"EXEC(N'CREATE INDEX [IX_Table1_Column1] ON [Table1] ([Column1]) WHERE [Column1] IS NOT NULL');
");
        }

        [ConditionalFact]
        public virtual void CreateIndex_generates_exec_when_legacy_filter_and_idempotent()
        {
            Generate(
                modelBuilder =>
                {
                    modelBuilder
                        .HasAnnotation(CoreAnnotationNames.ProductVersion, "1.1.0")
                        .Entity("Table1").Property<int?>("Column1");
                },
                migrationBuilder => migrationBuilder.CreateIndex(
                    name: "IX_Table1_Column1",
                    table: "Table1",
                    column: "Column1",
                    unique: true),
                MigrationsSqlGenerationOptions.Idempotent);

            AssertSql(
                @"EXEC(N'CREATE UNIQUE INDEX [IX_Table1_Column1] ON [Table1] ([Column1]) WHERE [Column1] IS NOT NULL');
");
        }

        [ConditionalFact]
        public virtual void DeleteData_generates_exec_when_idempotent()
        {
            Generate(
                modelBuilder => { },
                migrationBuilder => migrationBuilder
                    .DeleteData(
                        table: "Table1",
                        keyColumn: "Id",
                        keyColumnType: "int",
                        keyValue: 1),
                MigrationsSqlGenerationOptions.Idempotent);

            AssertSql(
                @$"DELETE FROM `Table1`
WHERE `Id` = 1;
SELECT @@ROWCOUNT;

");
        }

        [ConditionalFact]
        public virtual void InsertData_generates_exec_when_idempotent()
        {
            Generate(
                modelBuilder => { },
                migrationBuilder => migrationBuilder
                    .InsertData(
                        table: "Table1",
                        column: "Id",
                        columnType: "int",
                        value: 1),
                MigrationsSqlGenerationOptions.Idempotent);

            AssertSql(
                @$"IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE `Name` IN (N'Id') AND [object_id] = OBJECT_ID(N'[Table1]'))
    SET IDENTITY_INSERT [Table1] ON;
EXEC(N'INSERT INTO [Table1] ([Id])
VALUES (1)');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE `Name` IN (N'Id') AND [object_id] = OBJECT_ID(N'[Table1]'))
    SET IDENTITY_INSERT [Table1] OFF;
");
        }

        [ConditionalFact]
        public virtual void UpdateData_generates_exec_when_idempotent()
        {
            Generate(
                modelBuilder => { },
                migrationBuilder =>
                {
                    var operation = migrationBuilder
                        .UpdateData(
                            table: "Table1",
                            keyColumnTypes: new[] { "int" },
                            keyColumns: new[] { "Id" },
                            keyValues: new object[] { 1 },
                            columns: new[] { "Column1" },
                            columnTypes: new[] { "int" },
                            values: new object[] { 2 });
                },
                MigrationsSqlGenerationOptions.Idempotent);

            AssertSql(
                @$"EXEC(N'UPDATE [Table1] SET [Column1] = 2
WHERE [Id] = 1;
SELECT @@ROWCOUNT');
");
        }

        [ConditionalFact]
        public virtual void AddColumnOperation_datetime_with_defaultValue_sql()
        {
            Generate(
                new AddColumnOperation
                {
                    Table = "People",
                    Schema = null,
                    Name = "Birthday",
                    ClrType = typeof(DateTime),
                    ColumnType = "datetime",
                    IsNullable = false,
                    DefaultValueSql = "#2019-01-01#"
                });

            AssertSql(
                $@"ALTER TABLE `People` ADD `Birthday` datetime NOT NULL DEFAULT #2019-01-01#;
            ");
        }
        
        public JetMigrationsSqlGeneratorTest()
            : base(
                JetTestHelpers.Instance,
                new ServiceCollection()/*.AddEntityFrameworkJetNetTopologySuite()*/,
                JetTestHelpers.Instance.AddProviderOptions(
                    ((IRelationalDbContextOptionsBuilderInfrastructure)
                        new JetDbContextOptionsBuilder(new DbContextOptionsBuilder())/*.UseNetTopologySuite()*/)
                    .OptionsBuilder).Options)
        {
        }
    }
}
