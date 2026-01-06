# Implementation Plan - Dynamic Fuel Type Mapping

The user wants to manage fuel type mappings (XML IDs -> Fuel Names) via the database instead of hardcoded values in the code. This allows for easier updates and maintenance.

## 1. Database Schema Updates

### `Yakit` Table
- Add `TurpakUrunKodu` (string) column to store comma-separated XML IDs (e.g., "4,5").

### `OtomasyonSatis` Table
- Add `YakitId` (int, nullable) column.
- Add Foreign Key to `Yakit` table.

### `FiloSatis` Table
- Add `YakitId` (int, nullable) column.
- Add Foreign Key to `Yakit` table.

## 2. Migration
- Create a new migration `AddTurpakCodesToYakitAndRelation` to apply these schema changes.
- Update `DefinitionsService` to seed default Turpak codes for existing fuel types.

## 3. Service Logic Updates (`VardiyaService.cs`)

### `ProcessXmlZipAsync` Method
- Fetch all `Yakit` records from the database at the start of the method.
- Create a mapping dictionary: `Dictionary<string, Yakit>` where the key is the XML ID.
- When parsing `Txn` (Transaction) elements:
    - Look up the `FuelType` ID in the dictionary.
    - If found, assign `YakitId` to the `OtomasyonSatis` or `FiloSatis` object.
    - Also assign `YakitTuru` (string) for backward compatibility (using `Yakit.Ad` or `Yakit.OtomasyonUrunAdi`).

### `MapFuelType` Method
- Refactor or remove this method to use the database-driven mapping instead of the switch-case.

## 4. Verification
- Upload a sample XML/ZIP file.
- Verify that `OtomasyonSatis` and `FiloSatis` records have the correct `YakitId` and `YakitTuru` populated based on the database configuration.
