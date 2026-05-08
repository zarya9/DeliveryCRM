using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    public partial class FuelTypesCatalogExpansion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "FuelTypes" SET "Name" = 'Бензин АИ-95'
                WHERE lower(trim("Name")) = lower('Бензин');

                UPDATE "FuelTypes" SET "Name" = 'Газ (LPG/метан)'
                WHERE lower(trim("Name")) = lower('Газ');

                UPDATE "FuelTypes" SET "Name" = 'Гибрид (бензин/электро)'
                WHERE lower(trim("Name")) = lower('Гибрид');

                UPDATE "FuelTypes" SET "Name" = 'Электро'
                WHERE lower(trim("Name")) = lower('Электро');

                UPDATE "FuelTypes" SET "Name" = 'Дизель'
                WHERE lower(trim("Name")) = lower('Дизель');

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Бензин АИ-92'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Бензин АИ-92')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Бензин АИ-98'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Бензин АИ-98')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Бензин АИ-100'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Бензин АИ-100')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Газ (CNG/метан)'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Газ (CNG/метан)')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Электро (аккумулятор)'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Электро (аккумулятор)')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Плагин-гибрид (PHEV)'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Плагин-гибрид (PHEV)')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'MHEV (мягкий гибрид)'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('MHEV (мягкий гибрид)')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Водород (FCEV)'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Водород (FCEV)')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Биодизель'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Биодизель')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Этанол (E85)'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Этанол (E85)')
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Бензин АИ-92');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Бензин АИ-98');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Бензин АИ-100');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Газ (CNG/метан)');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Электро (аккумулятор)');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Плагин-гибрид (PHEV)');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('MHEV (мягкий гибрид)');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Водород (FCEV)');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Биодизель');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Этанол (E85)');

                UPDATE "FuelTypes" SET "Name" = 'Бензин'
                WHERE lower(trim("Name")) = lower('Бензин АИ-95');

                UPDATE "FuelTypes" SET "Name" = 'Газ'
                WHERE lower(trim("Name")) = lower('Газ (LPG/метан)');

                UPDATE "FuelTypes" SET "Name" = 'Гибрид'
                WHERE lower(trim("Name")) = lower('Гибрид (бензин/электро)');
                """);
        }
    }
}
