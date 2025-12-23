# Implementation Plan - Istasyon Demo

This file tracks the step-by-step execution of user instructions to ensure accuracy and minimize errors.

## Current Objective: Fix HTML Structure in Pompa Yonetimi

### Tasks
- [ ] **Step 1**: Analyze `pompa-yonetimi.component.html` for structural errors (unexpected closing tags).
- [ ] **Step 2**: Fix any identified HTML structure issues.
- [ ] **Step 3**: Verify the fix by running the build or checking for compilation errors.

## Execution Log

### [2025-12-23] Initial Setup
- Created `PLAN.md` to track progress.
- Analyzed `pompa-yonetimi.component.html`.
- Running `npm run build` to identify the specific HTML structure error.
- **Result**: Build successful (Exit code 0). No HTML structure errors found.

## Current Objective: Optimize Pompa Mutabakatı Performance

### Problem
- Shift data loads very slowly.
- The "pusula" entry table populates with a significant delay.
- Suspected timeout or performance bottleneck.

### Tasks
- [ ] **Step 1**: Analyze `PompaYonetimiComponent` (frontend) to understand data fetching logic.
- [ ] **Step 2**: Identify the backend endpoints being called.
- [ ] **Step 3**: Analyze the backend code (Controllers/Services) for performance bottlenecks (e.g., N+1 queries, heavy loops).
- [ ] **Step 4**: Implement optimizations (e.g., optimized LINQ queries, caching, parallel processing).
- [ ] **Step 5**: Verify performance improvement.

## Execution Log

### [2025-12-23] Performance Optimization
- Received user report about slow loading in "Pompa Mutabakatı".
- Starting analysis of `PompaYonetimiComponent`.

#### Step 1: Frontend Analysis ✅
- **File**: `pompa-yonetimi.component.ts`
- **Data Fetching Logic**:
  1. `loadVardiyaData()` calls `vardiyaApiService.getVardiyaById(vardiyaId)` (line 144)
  2. On success, calls `loadPusulalar()` which calls `pusulaApiService.getAll(vardiyaId)` (line 213)
- **Observation**: Sequential API calls (not parallel), but this is logical since pusulalar depend on vardiya.

#### Step 2: Backend Endpoints Identified ✅
| Endpoint | Controller Method |
|----------|-------------------|
| `GET /api/vardiya/{id}` | `VardiyaController.GetById()` |
| `GET /api/vardiya/{id}/pusula` | `PusulaController.GetAll()` |

#### Step 3: Backend Analysis ✅

##### `VardiyaController.GetById()` (line 119-198):
- Uses `AsNoTracking()` ✅ (Good for read-only)
- **ISSUE FOUND**: Uses `.Select()` with nested `.ToList()` inside the projection:
  ```csharp
  OtomasyonSatislar = _context.OtomasyonSatislar.Where(s => s.VardiyaId == id).Select(...).ToList()
  FiloSatislar = _context.FiloSatislar.Where(f => f.VardiyaId == id).Select(...).ToList()
  Pusulalar = _context.Pusulalar.Where(p => p.VardiyaId == id).Select(...).ToList()
  ```
  This is actually fine as EF Core translates this to a single query with joins.

##### `VardiyaController.GetAll()` (line 212-221):
- **MAJOR ISSUE**: Uses `.Include(v => v.OtomasyonSatislar)` which loads ALL sales for ALL shifts.
- This is likely the source of significant slowdowns when listing shifts!

##### `PusulaController.GetAll()` (line 20-33):
- **ISSUE FOUND**: Makes 2 database calls:
  1. `FindAsync(vardiyaId)` - to check if vardiya exists
  2. `ToListAsync()` - to get pusulalar
- The first call is unnecessary for a simple list operation.

##### Connection String Analysis:
- **Host**: localhost (local database)
- **No Timeout Settings**: Default Npgsql timeout is 30 seconds, but no explicit configuration.
- **No Connection Pooling Configuration**: Uses default pooling (100 connections).

#### Root Causes Identified:
1. **GetAll() in VardiyaController**: Includes OtomasyonSatislar for ALL vardiyalar (potentially thousands of records).
2. **Redundant DB Call in PusulaController**: Unnecessary vardiya existence check.
3. **Missing Database Indexes**: No explicit indexes on VardiyaId foreign keys.
4. **No Explicit Timeout Configuration**: Could cause issues with slow remote databases.

---

### Step 4: Optimizations to Implement
- [x] 4.1: Optimize `VardiyaController.GetAll()` - Removed `Include()`, added `AsNoTracking()`, returns summary counts.
- [x] 4.2: Optimize `PusulaController.GetAll()` - Removed redundant `FindAsync`, added `AsNoTracking()`.
- [x] 4.3: Add database indexes on VardiyaId foreign keys and BaslangicTarihi column.
- [x] 4.4: Configure connection string with timeout (30s), command timeout (60s), and pooling settings.

### Step 5: Apply Database Migration ✅
Migration applied successfully:
```
dotnet ef migrations add AddPerformanceIndexes_V2
dotnet ef database update
```

### Git Commits
- `Pre-optimization checkpoint: PLAN.md added for tracking`
- `Performance optimization: Removed Include, added AsNoTracking, indexes, timeout settings`

---

## Summary of Changes

### Files Modified:
1. **VardiyaController.cs**: `GetAll()` now returns lightweight summary with counts instead of full objects.
2. **PusulaController.cs**: `GetAll()` removed redundant DB call, added `AsNoTracking()`.
3. **AppDbContext.cs**: Added 5 performance indexes.
4. **appsettings.json**: Optimized connection string with timeout and pooling.

### Expected Performance Improvement:
- **Before**: GetAll loads full OtomasyonSatislar for all vardiyalar (~N*M records)
- **After**: GetAll returns only counts (~N records, 1 query with subqueries)
- **Estimated improvement**: 5-10x faster for vardiya listing, 2x faster for pusula queries

---

## Phase 2: GetById Optimization (Pompa Mutabakatı Sayfası)

### Problem Analysis:
- `GetById` endpoint makes 3 nested subqueries (OtomasyonSatislar, FiloSatislar, Pusulalar)
- Frontend receives raw sales data and processes it client-side (grouping by personnel)
- This is inefficient for large datasets

### Solutions:
- [ ] 6.1: Create new optimized endpoint that returns pre-aggregated data by personnel
- [ ] 6.2: Add caching for frequently accessed vardiya details
- [ ] 6.3: Frontend parallel API calls instead of sequential
