using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    public partial class FuelTypesCatalogExpansion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "FuelTypes" SET "Name" = 'Р‘РµРЅР·РёРЅ РђР-95'
                WHERE lower(trim("Name")) = lower('Р‘РµРЅР·РёРЅ');

                UPDATE "FuelTypes" SET "Name" = 'Р“Р°Р· (LPG/РјРµС‚Р°РЅ)'
                WHERE lower(trim("Name")) = lower('Р“Р°Р·');

                UPDATE "FuelTypes" SET "Name" = 'Р“РёР±СЂРёРґ (Р±РµРЅР·РёРЅ/СЌР»РµРєС‚СЂРѕ)'
                WHERE lower(trim("Name")) = lower('Р“РёР±СЂРёРґ');

                UPDATE "FuelTypes" SET "Name" = 'Р­Р»РµРєС‚СЂРѕ'
                WHERE lower(trim("Name")) IN (lower('Р­Р»РµРєС‚...'), lower('Р­Р»РµРєС‚СЂРѕ'));

                UPDATE "FuelTypes" SET "Name" = 'Р”РёР·РµР»СЊ'
                WHERE lower(trim("Name")) = lower('Р”РёР·РµР»СЊ');

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Р‘РµРЅР·РёРЅ РђР-92'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р‘РµРЅР·РёРЅ РђР-92')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Р‘РµРЅР·РёРЅ РђР-98'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р‘РµРЅР·РёРЅ РђР-98')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Р‘РµРЅР·РёРЅ РђР-100'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р‘РµРЅР·РёРЅ РђР-100')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Р“Р°Р· (CNG/РјРµС‚Р°РЅ)'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р“Р°Р· (CNG/РјРµС‚Р°РЅ)')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Р­Р»РµРєС‚СЂРѕ (Р°РєРєСѓРјСѓР»СЏС‚РѕСЂ)'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р­Р»РµРєС‚СЂРѕ (Р°РєРєСѓРјСѓР»СЏС‚РѕСЂ)')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'РџР»Р°РіРёРЅ-РіРёР±СЂРёРґ (PHEV)'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('РџР»Р°РіРёРЅ-РіРёР±СЂРёРґ (PHEV)')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'MHEV (РјСЏРіРєРёР№ РіРёР±СЂРёРґ)'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('MHEV (РјСЏРіРєРёР№ РіРёР±СЂРёРґ)')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Р’РѕРґРѕСЂРѕРґ (FCEV)'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р’РѕРґРѕСЂРѕРґ (FCEV)')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Р‘РёРѕРґРёР·РµР»СЊ'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р‘РёРѕРґРёР·РµР»СЊ')
                );

                INSERT INTO "FuelTypes" ("Name")
                SELECT 'Р­С‚Р°РЅРѕР» (E85)'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р­С‚Р°РЅРѕР» (E85)')
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р‘РµРЅР·РёРЅ РђР-92');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р‘РµРЅР·РёРЅ РђР-98');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р‘РµРЅР·РёРЅ РђР-100');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р“Р°Р· (CNG/РјРµС‚Р°РЅ)');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р­Р»РµРєС‚СЂРѕ (Р°РєРєСѓРјСѓР»СЏС‚РѕСЂ)');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('РџР»Р°РіРёРЅ-РіРёР±СЂРёРґ (PHEV)');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('MHEV (РјСЏРіРєРёР№ РіРёР±СЂРёРґ)');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р’РѕРґРѕСЂРѕРґ (FCEV)');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р‘РёРѕРґРёР·РµР»СЊ');
                DELETE FROM "FuelTypes" WHERE lower(trim("Name")) = lower('Р­С‚Р°РЅРѕР» (E85)');

                UPDATE "FuelTypes" SET "Name" = 'Р‘РµРЅР·РёРЅ'
                WHERE lower(trim("Name")) = lower('Р‘РµРЅР·РёРЅ РђР-95');

                UPDATE "FuelTypes" SET "Name" = 'Р“Р°Р·'
                WHERE lower(trim("Name")) = lower('Р“Р°Р· (LPG/РјРµС‚Р°РЅ)');

                UPDATE "FuelTypes" SET "Name" = 'Р“РёР±СЂРёРґ'
                WHERE lower(trim("Name")) = lower('Р“РёР±СЂРёРґ (Р±РµРЅР·РёРЅ/СЌР»РµРєС‚СЂРѕ)');
                """);
        }
    }
}
