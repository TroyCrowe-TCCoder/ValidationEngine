# Database Standards

**Version:** 1.7.0
**Status:** Active
**Applies To:** All repositories under the GlobalStandards governance model
**Audience:** AI models and human developers
**Created:** 2026-07-12
**Last Modified:** 2026-08-14

---
<!-- STD-MARKER: database.file -->


## 1. Purpose
<!-- STD-MARKER: database.1 -->

This standard defines the rules for database access, schema design, stored procedure authoring, connection management, and deployment conventions. Both human developers and AI models must apply every rule in this file when writing, reviewing, or generating any code that reads from or writes to a database. Compliance must be verified during pull request review and the pre-merge compliance process. A work slice that introduces a violation must not be marked complete and a PR must not be opened until the violation is resolved.

---

## 2. Technology Baseline
<!-- STD-MARKER: database.2 -->

[`GlobalCodingStandards.md`](../standards/GlobalCodingStandards.md#28-data-access)
- **Query execution:** All database operations must be executed through stored procedures. Inline SQL of any form — including parameterized inline SQL — is prohibited. The stored procedure is the contract between the application and the database.
- **Database platforms:** Two database stores are recognised under this standard:
  - **Transactional store — Azure SQL (SQL Server compatibility).** This is the primary store for all active, transactional data. No alternative relational database providers are permitted without an approved deviation documented in the repository's `copilot-instructions.md` addendum.
  - **Archive store — document database (platform TBD).** A document database will serve as the archive destination for aged, non-transactional records that have been migrated out of the transactional store. The specific platform has not yet been selected. Rules for the archive store are defined in [Section 3 — Archive Database](#3-archive-database). All rules in this standard that do not explicitly reference the archive store apply to the transactional Azure SQL store only.
database. See [`GlobalSecurityStandards.md`](../standards/GlobalSecurityStandards.md#2-secrets-management) for secret management rules.
- **Database account permissions:** The application database account must be granted execute permission on stored procedures only. DDL permissions — `CREATE`, `ALTER`, `DROP` — must not be granted to the application account. Schema changes are made through the deployment pipeline by a dedicated pipeline identity, not by the application at runtime.

---

## 3. Archive Database
<!-- STD-MARKER: database.3 -->

> **Status: Planned — platform not yet selected.** The rules in this section describe the intended design. Implementation rules that depend on the chosen platform are explicitly marked as deferred. No archive store may be built until the platform decision has been recorded in `Working/ProjectBacklog.md` and the deferred rules in this section have been completed.

### 3.1 Purpose and Scope
<!-- STD-MARKER: database.3.1 -->

The archive database stores records that have aged past a defined retention threshold and are no longer needed in the transactional Azure SQL store. Archived records must remain queryable by the application but are no longer subject to transactional writes.

- The archive store is a document (non-relational) database. A flat, schema-flexible document model is chosen specifically to avoid the overhead of relational schema migrations as data shapes evolve over time.
- Archived records are read-only after migration. No update or delete operations are permitted against archived records by the application. Deletion for compliance purposes (e.g., right-to-erasure) is a privileged pipeline operation outside the normal application data path.
- The archive store must not hold any record that is still subject to active transactional writes in Azure SQL.

### 3.2 Migration Job
<!-- STD-MARKER: database.3.2 -->

- Archival is performed by a timer-triggered Azure Function. The function runs on a defined schedule and is not triggered by application activity.
- The function must:
  1. Query the transactional database for records that meet the age or status threshold defined in its configuration.
  2. Write each qualifying record to the archive document store.
  3. Verify that each record was written successfully before deleting it from the transactional database.
  4. Log the count of migrated records, any write failures, and any records skipped per run.
- Steps 2 and 3 must be treated as an atomic unit per record. If the archive write fails, the record must not be deleted from the transactional database. The function must log the failure and continue with the next record — it must not abort the entire batch on a single failure.
- The age or status threshold must be read from the function's application configuration — it must not be hardcoded.
- The migration function must be idempotent. Re-running it against already-migrated records must not produce duplicate archive entries or data loss.
- After all record migrations in a run complete, the function must trigger an index refresh on both the transactional database and the archive store to ensure query performance is maintained after the deletions and the newly migrated records are immediately queryable. The index refresh must run after the full batch — not after each individual record. If either refresh fails, the failure must be logged but must not roll back or invalidate the migrations already committed in that run.

### 3.3 Application Read Routing
<!-- STD-MARKER: database.3.3 -->

- The application must determine at query time
- Routing logic must be encapsulated behind the repository interface. Callers must not need to know which store is the source of a record.
- The routing signal for records that were explicitly migrated is the presence of both `IsArchived` (set to `1`) and `ArchivedDate` (the UTC timestamp of migration) on the record. Both fields must be present and consistent — a record with `IsArchived = 1` but no `ArchivedDate` value, or vice versa, is a data integrity error and must be logged as such.
- Not all migrated records will carry `IsArchived` and `ArchivedDate`. A record may be identified as a migration candidate based solely on inactivity — a record whose `UpdatedDate` has not changed within the configured threshold — without having been explicitly flagged as archived.
- The preferred routing approach is date-based: because the migration rule is deterministic, the application can calculate at query time which store a record resides in without any marker left in the transactional database. This keeps entity tables clean and avoids residual row growth in the transactional store. Any deferred implementation decisions for this routing logic must be tracked in `Working/ProjectBacklog.md` before implementation begins.
- For queries that may span the archive threshold, the application must provide a mechanism for the user to declare that historical data may be needed so the repository layer can extend the query scope to the archive store and merge results transparently. Any deferred UI treatment and merge-strategy decisions must be tracked in `Working/ProjectBacklog.md` before implementation begins.
- The inactivity threshold is a configuration value that has not yet been finalised. It must be agreed and recorded in `Working/ProjectBacklog.md` before the migration job is implemented. The standard does not hardcode the threshold value.

### 3.4 Access Technology — Platform SDK Pending
<!-- STD-MARKER: database.3.4 -->

The archive store SDK and connection management approach will be determined once the document database platform is selected. At that point this section must be completed before any archive store code is written. The following question must be answered:

- Which SDK or client library is used?

The following rules apply regardless of the platform chosen:

- Archive store connection strings and credentials must be retrieved from Azure Key Vault at runtime, consistent with Section 8.1. No exceptions.
- All archive store operations must be executed through the repository pattern consistent with Section 5. The SDK or client library is an implementation detail behind the repository interface — callers must not interact with the archive store directly.
- Transient failure handling must use Polly. Retry policies must follow the same exponential back-off configuration defined for the transactional store in Section 8.2 — maximum 3 attempts, starting at 1 second, maximum 10 seconds. The archive store repository must not swallow exceptions on final failure.

### 3.5 Document Structure
<!-- STD-MARKER: database.3.5 -->

Archived documents must mirror the structure of the originating relational record with one deliberate difference: relational foreign key fields must not be stored as IDs. Instead, the actual related data they reference must be resolved at migration time and embedded directly in the document. Because archived records are read-only after migration, the embedded data represents a point-in-time snapshot and cannot go stale.

**Example:** A relational record that stores `ClientId` as a foreign key would become a document that embeds the full client name, identifier, and any other display or routing fields needed — the `ClientId` integer is not carried forward.

This makes each archived document fully self-contained. No joins, no cross-document lookups, and no dependency on the transactional database to render or query an archived record.

In addition to the mirrored fields, every archived document must carry the following archive metadata:

- The original primary key from the transactional table.
- The tenant identifier.
- The client identifier.
- The `ArchivedDate` timestamp (UTC).
- The name of the migration job run that produced the record.

Field naming conventions for archived documents will be defined once the platform is selected and must be consistent with the originating relational schema naming — PascalCase, consistent with Section 4.1.

---

## 4. Schema Design Rules
<!-- STD-MARKER: database.4 -->

Both human developers and AI models must apply every rule in this section to all new and changed schema work. A violation found during a PR review must be resolved before the PR is approved.

### 4.1 Naming Conventions
<!-- STD-MARKER: database.4.1 -->

**Tables:**
- Table names must be PascalCase singular nouns — `Client`, `Document`, `AuditEntry`.
- Junction and bridge tables must be named by joining both entity names — `ClientDocument`, `UserRole`.
- No abbreviations except universally understood ones — `Id`, `URL`, `API`.

**Columns:**
- Column names must be PascalCase — `ClientId`, `CreatedDate`, `UpdatedBy`.
- Foreign key columns must follow the pattern `{ReferencedTableName}Id` — `ClientId`, `DocumentId`.

**Stored procedures:**
- Stored procedure names must follow the pattern `{Entity}_{Verb}` — `Client_Get`, `Document_Upsert`, `AuditEntry_Delete`. When the procedure filters by a specific qualifier, the qualifier is included before the verb — `ClientByID_Get`, `DocumentsByClientId_Get`.
- The verb must reflect the operation precisely. `Upsert` is the preferred verb for all write operations and must be backed by a SQL `MERGE` statement. `Insert` and `Update` are permitted when the operation is explicitly insert-only or update-only and a merge is not appropriate. `Get`, `Delete`, and `Archive` are also approved verbs. Generic verbs such as `Process` or `Handle` are not permitted.
- Stored procedure names must correspond to the repository method name they serve — `GetClientsAsync` calls `Client_Get`, `GetClientByIdAsync` calls `ClientByID_Get`.

### 4.2 Primary Keys
<!-- STD-MARKER: database.4.2 -->

- Every table must have a primary key.
- The primary key must be `INT IDENTITY(1,1)`. This is the clustered index column and the field used for all internal joins and repository lookups. `INT` is chosen over `BIGINT` to keep index size minimal; promote to `BIGINT` only when the row volume genuinely requires it and document the reason in the schema change script.
- Every table must also carry a `UNIQUEIDENTIFIER` column with a default of `NEWID()` as a secondary unique field. This field is used for external references — API responses, cross-service identifiers, and any context where exposing a sequential integer would leak row count information. It must have a unique index applied.
- The `INT` identity column is the primary key and the clustered index. The `UNIQUEIDENTIFIER` column must use a non-clustered unique index — never make the `UNIQUEIDENTIFIER` the clustered index as random GUIDs cause index fragmentation.
- Composite primary keys on application entity tables are not permitted. Apply a unique index for composite uniqueness constraints and use a surrogate key as the primary key.

### 4.3 Audit Columns
<!-- STD-MARKER: database.4.3 -->

Every application entity table must include the following audit columns:

```sql
CreatedDate    DATETIME      NOT NULL
CreatedBy      VARCHAR(100)  NOT NULL
UpdatedDate    DATETIME      NULL
UpdatedBy      VARCHAR(100)  NULL
```

- These columns must be populated by the service layer using the authenticated principal's identity — not by database defaults alone.
- Stored procedures that perform inserts or updates must accept `@CreatedBy` / `@UpdatedBy` parameters and write them explicitly.
- Database defaults serve only as a safety net — the application must not rely on them as the primary population mechanism.

### 4.4 Soft Deletes
<!-- STD-MARKER: database.4.4 -->

For entities that may need to be recovered, restored, or audited after deletion, soft deletes must be used:

```sql
IsArchived   TINYINT       NOT NULL  CONSTRAINT [DF_{Table}_IsArchived]    DEFAULT ((0))
ArchivedDate DATETIME      NULL
ArchivedBy   VARCHAR(100)  NULL
```

- Stored procedures that query soft-deletable entities must include a `WHERE IsArchived = 0` filter unless explicitly retrieving archived records.
- Hard deletes are permitted only for transient, non-audited records — for example, session data or job queue entries — and must be documented in the PR.

### 4.5 Nullable Columns
<!-- STD-MARKER: database.4.5 -->

- Nullable columns must only be used when `NULL` has a distinct business meaning separate from a zero-value or empty string.
- Nullable columns must not be used as a substitute for a default value where the column is always populated in practice.
- Nullable columns must be mapped to nullable C# types — `string?`, `DateTime?`, `int?` — in the corresponding model or DTO.

### 4.6 Indexing
<!-- STD-MARKER: database.4.6 -->

- An index must be added for every foreign key column.
- A unique index must be added for every business-key column that enforces identity constraints beyond the primary key.
- Speculative indexes must not be added. Indexes must be added only to resolve a measured query performance problem or to support a known high-frequency query pattern.
- Every composite index must include a comment in the schema change script explaining which query it supports.

### 4.7 Data Type Selection
<!-- STD-MARKER: database.4.7 -->

Column and parameter data types must be chosen to match the actual domain requirement as precisely as possible. Over-allocating types wastes memory and degrades index efficiency; under-allocating causes truncation or overflow errors.

**Integer types** — the smallest integer type that covers the full expected value range must be used:

| Type | Range | Use when |
|---|---|---|
| `TINYINT` | 0–255 | Status codes, flags, small enumerations |
| `SMALLINT` | -32,768–32,767 | Small bounded numeric values |
| `INT` | -2.1B–2.1B | Standard surrogate keys and counts |
| `BIGINT` | -9.2Q–9.2Q | High-volume identifiers, large counters |

**Boolean flags** — `TINYINT` must be used for all boolean columns. `BIT` must not be used. SQL Server's handling of `TINYINT` is more efficient than `BIT` in indexed and filtered operations, and `TINYINT` is consistent with the existing schema where `IsArchived`, `HasNotification`, and similar flags are already `TINYINT`. All `TINYINT` boolean columns must define a default constraint of `((0))`. C# models expose these columns as `bool`. Conversion between the two must use an explicit ternary — do not rely on implicit casting, `Convert.ToBoolean`, or `GetBoolean`.

Reading from the database (`TINYINT` → `bool`):
```csharp
IsArchived = data == 1 ? true : false;
```

Writing to the database (`bool` → `Int16` for the SQL parameter):
```csharp
isArchived = data == true ? (Int16)1 : (Int16)0;
```

**String types** — `VARCHAR` is the required string type. `NVARCHAR` must not be used. `NVARCHAR` stores Unicode (UTF-16) at 2 bytes per character — double the storage cost of `VARCHAR` for the same content. No current data domain in this schema requires Unicode storage, and the additional overhead has no benefit. If a future requirement genuinely demands Unicode storage it must be justified and approved as a documented deviation before any `NVARCHAR` column is created.

`VARCHAR` length must reflect the maximum realistic value for the column's domain. Common reference lengths used in this schema:

| Length | Typical use |
|---|---|
| `VARCHAR(20)` | Phone numbers, short codes |
| `VARCHAR(50)` | Passwords, short identifiers |
| `VARCHAR(100)` | Person names, usernames, audit identity fields |
| `VARCHAR(200)` | Account numbers, addresses, general descriptive fields |
| `VARCHAR(500)` | URLs, long descriptive fields |

`VARCHAR(MAX)` and oversized fixed lengths such as `VARCHAR(500)` must not be used when the domain clearly fits a smaller size. Length decisions must be documented in the schema change script comment when they are not self-evident from the column name.

**Date types** — `DATETIME` is the standard date column type. `DATETIME2` must not be used. `DATE` is permitted when no time component is needed. `SMALLDATETIME` is permitted for columns where minute-level precision is sufficient and storage efficiency is a priority.

---

## 5. Repository Pattern
<!-- STD-MARKER: database.5 -->

Both human developers and AI models must apply every rule in this section to all new and changed data access code. A violation found during a PR review must be resolved before the PR is approved.

### 5.1 Structure
<!-- STD-MARKER: database.5.1 -->
- Repository interfaces must be defined in the application core layer. Implementations belong in the infrastructure layer.
- Controllers and services must not take a database connection or command dependency directly — they must depend on repository interfaces only.
- Each repository must be responsible for one clearly bounded entity area. A repository that reaches across unrelated entity boundaries must be split.

### 5.2 ADO.NET Implementation Pattern
<!-- STD-MARKER: database.5.2 -->

Every repository method must follow this pattern without exception:

```csharp
public async Task<IReadOnlyList<ClientListItem>> GetClientsAsync(int? clientId, CancellationToken cancellationToken)
{
    var results = new List<ClientListItem>();

    await using var connection = new SqlConnection(_connectionString);
    await using var command = new SqlCommand("Client_Get", connection)
    {
        CommandType = CommandType.StoredProcedure,
        CommandTimeout = _options.DatabaseCommandTimeoutSeconds
    };

    command.Parameters.AddWithValue("@ClientId", (object?)clientId ?? DBNull.Value);

    await connection.OpenAsync(cancellationToken);

    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    while (await reader.ReadAsync(cancellationToken))
    {
        results.Add(new ClientListItem(reader));
    }

    return results;
}
```

Rules derived from this pattern:

- `CommandType` must always be set to `CommandType.StoredProcedure`. It must never be `CommandType.Text`.
- `CommandTimeout` must be read from application configuration — it must not be hardcoded.
- `CancellationToken` must be passed to every async ADO.NET method — `OpenAsync`, `ExecuteReaderAsync`, `ExecuteNonQueryAsync`, `ExecuteScalarAsync`, `ReadAsync`.
- `null` parameters must be passed as `DBNull.Value` — never as C# `null`.
- `await using` must be used for all `SqlConnection`, `SqlCommand`, and `SqlDataReader` instances.
- Repository methods must return materialized results — `IReadOnlyList<T>`, `T?`, `int`, `bool` — never a reader or connection.

### 5.3 Model Construction from Reader
<!-- STD-MARKER: database.5.3 -->

Model and DTO construction from a `SqlDataReader` must be performed in the constructor of the target type. The constructor must accept the reader as a parameter and read its own properties. This is a direct application of the object mapping rule in [`GlobalCodingStandards.md`](../standards/GlobalCodingStandards.md#36-object-mapping).

```csharp
public class ClientListItem
{
    public int    Id       { get; set; }
    public string Name     { get; set; }
    public bool   IsActive { get; set; }
}
```

- `GetOrdinal` must be used to resolve column positions by name — positional index access is not permitted.
- `reader.GetOrdinal` calls must not be made inside a read loop — ordinals must be resolved once before the loop if performance sensitivity requires it.

### 5.4 Tenant and Client Scope Parameters
<!-- STD-MARKER: database.5.4 -->

[`GlobalSecurityStandards.md`](../standards/GlobalSecurityStandards.md#34-tenant-and-client-isolation--defense-in-depth).

---

## 6. Stored Procedure Rules
<!-- STD-MARKER: database.6 -->

Both human developers and AI models must apply every rule in this section when writing or reviewing stored procedures. A violation found during a PR review must be resolved before the PR is approved.

### 6.1 Structure
<!-- STD-MARKER: database.6.1 -->

Every stored procedure must be wrapped in a `BEGIN` / `END` block. `SET NOCOUNT ON` must be the first statement inside the block in all procedures. Beyond that, the required structure differs by operation type.

**Read procedures (`_Get`):**

- `SET NOCOUNT ON` only. No transaction and no `TRY/CATCH` are required for read-only operations.
- Use temp tables (`#TempTable`) for complex multi-source queries that require filtering, sorting, or paging across the assembled result. Drop the temp table explicitly at the end of the procedure.
- Paging is required when a procedure may return a large recordset. Returning unbounded result sets is not permitted. The most relevant example is any "get all" query — a procedure that retrieves all records for an entity without a limiting predicate must use paging. Paged procedures must return two result sets: the total record count first (`SELECT COUNT(...) FROM #Temp`), then the paged data using `OFFSET (@PageNum - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY`.

**Write procedures (`_Upsert`, `_Archive`, `_Delete`):**

- `SET NOCOUNT ON` and `SET XACT_ABORT ON` must both be present. `SET XACT_ABORT ON` ensures that when multiple write operations must succeed together, any single failure automatically rolls back all operations in the transaction — achieving true atomicity across multiple statements without nested transactions. Nested transactions are not used; `SET XACT_ABORT ON` combined with a single `BEGIN TRANSACTION` / `COMMIT` / `ROLLBACK` block is the correct and cleaner pattern.
- Input validation using `THROW` must appear before the transaction opens. Each required parameter must be validated individually with a unique error number. This is the fail-fast principle applied to the database layer — invalid input is rejected immediately before any transaction is opened, any lock is acquired, or any I/O is performed. Failing fast on bad input is both a correctness requirement and a performance requirement: a transaction that is never opened cannot be rolled back, cannot hold locks, and cannot consume log space.
- The transaction wraps only the write operation — open it after validation, commit immediately after the write.
- `OUTPUT inserted.{PrimaryKey}` must be used to return the affected row's identity after a write. For operations that do not produce an inserted row (e.g. archive/soft-delete), `SELECT 1` must be returned to confirm success.
- The `TRY/CATCH` block must capture the full error context and log it to `dbo.SqlErrorTracking_Insert` before re-raising with `THROW`:

```sql
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    DECLARE @ERROR_NUMBER    INT          = ERROR_NUMBER();
    DECLARE @ERROR_SEVERITY  INT          = ERROR_SEVERITY();
    DECLARE @ERROR_STATE     INT          = ERROR_STATE();
    DECLARE @ERROR_PROCEDURE VARCHAR(50)  = ERROR_PROCEDURE();
    DECLARE @ERROR_LINE      INT          = ERROR_LINE();
    DECLARE @ERROR_MESSAGE   VARCHAR(100) = LEFT(ERROR_MESSAGE(), 100);

    EXEC dbo.SqlErrorTracking_Insert
        @ERROR_NUMBER,
        @ERROR_SEVERITY,
        @ERROR_STATE,
        @ERROR_PROCEDURE,
        @ERROR_LINE,
        @ERROR_MESSAGE;

    THROW;
END CATCH;
```

```sql
CREATE PROCEDURE Client_Get
    @TenantId  INT = NULL,
    @ClientId  INT = NULL
AS
SET NOCOUNT ON;

SELECT
    c.ClientId,
    c.ClientName,
    c.IsActive
FROM
    Client c
WHERE
    (@TenantId IS NULL OR c.TenantId = @TenantId)
    AND (@ClientId IS NULL OR c.ClientId = @ClientId)
    AND c.IsDeleted = 0;
```

### 6.2 Parameters
<!-- STD-MARKER: database.6.2 -->

- All parameters that filter by tenant or client must be nullable and must appear first in the parameter list — `@TenantId INT = NULL`, `@ClientId INT = NULL`.
- Parameters must use the minimum data type that satisfies the domain requirement — do not use `VARCHAR(MAX)` when `VARCHAR(100)` or `VARCHAR(200)` is sufficient.
- Output parameters must not be used where a result set or scalar return value is sufficient.

**Input validation** — all write procedures must validate required parameters before opening a transaction. Each validation must use `THROW` with a unique error number:

- Required integer ID parameters must be validated as non-null and greater than zero: `IF @AccountId IS NULL OR @AccountId <= 0`
- Required string parameters must be validated using `NULLIF(LTRIM(RTRIM(ISNULL(@Param, ''))), '')` to reject null, empty, and whitespace-only values
- `TINYINT` boolean parameters must be validated as `IN (0, 1)`

**Input sanitization** — string values written to the database must be sanitized at the point of write using `LEFT(LTRIM(RTRIM(ISNULL(@Param, default))), maxLength)` to enforce length limits and strip leading/trailing whitespace before persistence.

**Duplicate prevention** — upsert procedures that match on a name or description field must normalize the comparison value using `LOWER(LTRIM(RTRIM(...)))` to prevent case and whitespace variants from producing duplicate rows.

**Existence check for list object upserts** — upsert procedures for list objects used to populate dropdowns (e.g. `ServiceProvider`, item types, categories) must perform an existence check by name before opening a transaction when no ID is supplied. If `@EntityId` is null or zero, the procedure must query the table by normalized name to resolve an existing `@EntityId` before proceeding to the `MERGE`. This prevents duplicate list entries from being created when the caller does not hold the ID. The pattern established in `ServiceProvider_Upsert` is the reference implementation:

```sql
DECLARE @EffectiveServiceProviderId INT = NULLIF(@ServiceProviderId, 0);

IF @EffectiveServiceProviderId IS NULL
BEGIN
    SELECT TOP (1)
        @EffectiveServiceProviderId = sp.ServiceProviderId
    FROM dbo.ServiceProvider AS sp
    WHERE LTRIM(RTRIM(LOWER(sp.ServiceProvider))) = LTRIM(RTRIM(LOWER(@NormalizedServiceProvider)));
END;
```

### 6.3 Dynamic SQL
<!-- STD-MARKER: database.6.3 -->

Static SQL must always be preferred. All query logic must be expressed as static SQL within the procedure body unless a runtime-resolved identifier makes static SQL structurally impossible.

When dynamic SQL is required, `sp_executesql` is always used. `EXEC(@sql)` with a concatenated string is prohibited without exception, even when the statement appears safe. This is a decided standard, not a conditional preference.

Dynamic SQL is permitted only when the table name cannot be known at procedure-authoring time — for example, when the target table is derived from a client name or other runtime value (see the client-partitioned `InvoiceProcessingRecords` pattern). The following rules apply without exception:

- The dynamic object name **must** be wrapped in `QUOTENAME()` before concatenation. Never concatenate a raw runtime value as an identifier.
- All variable values passed into the query **must** be passed as parameters to `sp_executesql`, not concatenated into the SQL string.
- The existence of the target object must be validated with `OBJECT_ID()` before the dynamic statement is built, and the procedure must return a defined failure indicator if the object does not exist.
- A comment must explain why a static alternative is not possible.

This rule is consistent with the inline SQL prohibition in [`GlobalSecurityStandards.md`](../standards/GlobalSecurityStandards.md#4-inline-sql-prohibition), which governs application-layer SQL. The controls above achieve equivalent injection protection at the database layer.

See `design-decisions.md` (DD-003; not yet created, tracked in `Working/ProjectBacklog.md`) and `Working/ProjectBacklog.md`. The controls in this section apply to all dynamic SQL until those supplementary rules are published.

### 6.4 Cursor Usage
<!-- STD-MARKER: database.6.4 -->

A cursor must not be written when a set-based alternative — `MERGE`, `UPDATE ... FROM`, `INSERT ... SELECT`, a derived table, a CTE, or a `WHILE` loop — can achieve the same result.

A cursor must only be used when all of the following conditions are met:

- The operation cannot be expressed as a set-based statement.
- The requirement has been reviewed and the cursor approach explicitly approved in the PR.
- The cursor is declared with the most restrictive options appropriate — `FAST_FORWARD` and `READ_ONLY` where the data is not being modified during iteration.
- The cursor must be explicitly closed and deallocated in all exit paths, including error paths.

Cursor usage must be documented with a comment in the stored procedure explaining why a set-based alternative was not possible.

### 6.5 NOLOCK Prohibition
<!-- STD-MARKER: database.6.5 -->

`NOLOCK` (and the equivalent `READ UNCOMMITTED` isolation hint) is prohibited in all stored procedures and must not be added to ad-hoc queries run against any environment, including production.

The stated purpose of `NOLOCK` is typically to avoid blocking and improve read performance. That problem is already solved correctly on Azure SQL: `READ_COMMITTED_SNAPSHOT` isolation (RCSI) is enabled by default, which means readers never block writers and writers never block readers using row versioning. `NOLOCK` provides no performance benefit over RCSI and introduces risks that are especially harmful in a diagnostic context:

- **Dirty reads** — data from an uncommitted and potentially rolled-back transaction is returned. When troubleshooting an application error, this means the record state you are reading may not reflect what the application or database actually committed — the opposite of what a diagnostic query needs.
- **Row skipping and double reads** — SQL Server can skip rows entirely or return the same row twice in a single query when a page split occurs under load. A result set with missing or duplicated rows is unreliable for any purpose.
- **Phantom rows** — rows that have since been deleted, or that do not yet exist, may appear in the result set.

For production troubleshooting queries where read performance is a concern, the correct approach is to rely on RCSI, which provides a consistent read of committed data with no blocking. If a point-in-time consistent snapshot is needed across a longer diagnostic session, `SNAPSHOT` isolation is the appropriate tool.

`NOLOCK` must not be introduced to solve a locking or performance problem. If blocking is observed on production reads, the root cause — missing indexes, long-running transactions, poorly scoped locks — must be diagnosed and resolved rather than masked.

---

## 7. Transactions
<!-- STD-MARKER: database.7 -->

- Transactions must be scoped to the smallest possible set of operations.
- Transactions must not be held open across external HTTP calls or user interactions.
- When a repository method performs multiple writes that must be atomic, the transaction must be managed inside the stored procedure using `BEGIN TRANSACTION` / `COMMIT` / `ROLLBACK` with `SET XACT_ABORT ON`.
- Application-layer transaction coordination using `SqlTransaction` is permitted only when atomicity must span multiple repository calls. In that case the transaction must be opened, passed to each repository call explicitly, and committed or rolled back in a `try/finally` block that guarantees the connection is closed.

---

## 8. Connection Management
<!-- STD-MARKER: database.8 -->

### 8.1 Connection Strings
<!-- STD-MARKER: database.8.1 -->

- Connection strings must be retrieved from Azure Key Vault at runtime. They must not be stored in application configuration files, environment variables committed to source, or any database table.
- For dedicated per-client databases (multi-tenant Scenario 2), the Key Vault secret reference is resolved at request time from the master catalog. The connection string itself is never written to the catalog.
- Connection string retrieval and Key Vault integration rules are defined in [`GlobalSecurityStandards.md`](../standards/GlobalSecurityStandards.md#2-secrets-management).

### 8.2 Connection Resiliency
<!-- STD-MARKER: database.8.2 -->

ADO.NET does not provide built-in retry logic. Transient Azure SQL errors must be handled explicitly. A retry helper must be used for all connection open and command execution operations that may encounter transient failures:

- Maximum retry attempts: 3
- Retry delay: exponential back-off starting at 1 second, maximum 10 seconds
- Transient error codes to retry: 40613 (database unavailable), 40197 (service error), 40501 (service busy), 49918 (insufficient resources), 4221 (login failed — transient)

The retry implementation must not swallow exceptions on final failure — the original exception must be rethrown after all retry attempts are exhausted.

### 8.3 Connection Pooling
<!-- STD-MARKER: database.8.3 -->

ADO.NET connection pooling is enabled by default and must not be disabled. Connection strings must not include `Pooling=false`. Each repository method must open a connection, execute its command, and dispose the connection within the same method scope using `await using` — connections must not be held open across method boundaries or stored as instance fields.

---

## 9. Bulk Operations
<!-- STD-MARKER: database.9 -->

Batch processing operations — file uploads, bulk imports — must follow the rules in this section. Performance thresholds for bulk operations are defined in [`GlobalPerformanceStandards.md`](../standards/GlobalPerformanceStandards.md#24-batch-processing-operations).

### 9.1 Chunking
<!-- STD-MARKER: database.9.1 -->

Bulk data sets must be processed in chunks of 100 records. No single operation may attempt to write an entire unbounded data set in one transaction.

### 9.2 SqlBulkCopy
<!-- STD-MARKER: database.9.2 -->

For insert-only bulk operations, `SqlBulkCopy` is the required mechanism. It must be configured with:

- `SqlBulkCopyOptions.TableLock` — this option must be set to reduce lock contention during the bulk insert window.
- `BatchSize` must be set to the chunk size (100 records).
- `BulkCopyTimeout` must be read from application configuration.
- Column mappings must be defined explicitly — positional mapping must not be used.

```csharp
using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, transaction)
{
    DestinationTableName = "Document",
    BatchSize            = 100,
    BulkCopyTimeout      = _options.BulkCopyTimeoutSeconds
};

bulkCopy.ColumnMappings.Add("DocumentName", "DocumentName");
bulkCopy.ColumnMappings.Add("ClientId",     "ClientId");
bulkCopy.ColumnMappings.Add("CreatedBy",    "CreatedBy");

await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
```

### 9.3 Parallel Chunk Processing
<!-- STD-MARKER: database.9.3 -->

When chunks are independent, they must be processed in parallel using `Parallel.ForEachAsync` with `MaxDegreeOfParallelism` set to prevent connection pool exhaustion. The degree of parallelism must be read from application configuration, not hardcoded. Parallel execution rules are defined in [`GlobalPerformanceStandards.md`](../standards/GlobalPerformanceStandards.md#34-parallel-execution-for-independent-operations).

---

## 10. Schema Change Deployment
<!-- STD-MARKER: database.10 -->

All schema changes must be deployed via DACPAC through the release pipeline. Manual schema changes against any non-local environment must not be applied without exception.

> **Pipeline behavior by environment** (dev, QA, production) is governed by [`GlobalAzureDevOpsPipelineStandards.md`](GlobalAzureDevOpsPipelineStandards.md) Section 2. That section is the authoritative source of truth for what each environment's pipeline must do. The rules in this section describe database-specific requirements that layer on top of those shared behaviors. Where both sources apply, both must be satisfied.

### 10.1 DACPAC Rules
<!-- STD-MARKER: database.10.1 -->

- The DACPAC is the authoritative definition of the schema. All structural changes — table additions, column additions or modifications, index changes, constraint changes — must be made in the database project and deployed via DACPAC. Ad-hoc DDL scripts must not be applied directly to a non-local environment.
- Local development environments may be updated directly during active development. Once a change is ready for promotion it must be committed to the database project and deployed through the pipeline.
- The following operations carry additional risk during a DACPAC deploy and must be reviewed carefully before the PR is approved:
  - Dropping a column or table — DACPAC deploy will generate a destructive `DROP` statement; the reviewer must confirm the column or table is no longer referenced anywhere before merging.
  - Renaming a column or table — DACPAC treats a rename as a drop and add, which is destructive to data. A pre-deploy script must handle the rename explicitly if data must be preserved.
  - Changing column type or nullability on a populated table — a pre-deploy or post-deploy script must be included to handle existing data.
  - Adding a non-nullable column without a default to a populated table — a pre-deploy or post-deploy script must supply values for existing rows.
- **Destructive-change detection is a mandatory pipeline gate.** The pipeline must generate the DACPAC diff as a SQL script before the actual deploy runs. The generated script must be scanned for destructive operations. If destructive changes are present, the pipeline must verify that a pre-deploy script exists in the project's pre-deploy script location before the deploy stage is allowed to continue. If the pre-deploy script is missing, the pipeline must fail and notify. Generating the script and immediately deploying without this check is not permitted.
- **`BlockOnPossibleDataLoss` must never be set to `false`** in any pipeline configuration. DACPAC will refuse to deploy when data loss is possible. That refusal is a safety mechanism — it must not be bypassed. If a deploy is blocked, the correct response is to write a pre-deploy script that handles the data concern, not to override the flag.
- **`DropObjectsNotInSource` must be set to `false`** in all pipeline configurations. Objects not present in the source project must not be automatically dropped by the deploy.
- **`GenerateSmartDefaults` must be set to `true`** in all pipeline configurations. This ensures non-nullable column additions do not fail on existing rows.
- **Pre-deploy scripts** are the correct mechanism for any operation that must happen before the schema diff is applied — safely renaming a column, migrating data to a new structure, or handling existing data that would otherwise block the diff from applying cleanly.
- **Post-deploy scripts** are the correct mechanism for seed data, reference data inserts, and any cleanup that must happen after the schema diff is applied.
- PRs containing any of the risk operations listed above must be labelled `schema-risk` for additional reviewer attention.

### 10.2 Pipeline File Structure
<!-- STD-MARKER: database.10.2 -->

Database deployment pipelines follow a two-file layout. Each file has a single responsibility.

| File | Responsibility |
|---|---|
| `{repo}-main.yml` | Entrypoint — declares triggers, PR gates, and path filters. Contains only an `extends:` reference to the deploy template. |
| `{repo}-main-deploy.yml` | Deploy template — contains all stages, jobs, and steps. Not triggered directly. |

Both files must reside in `.azure-pipelines/workflows/` within the repository root.

File naming must follow the pattern `{repo}-main.yml` and `{repo}-main-deploy.yml` where `{repo}` is the lowercase repository name. Legacy `master`-suffixed variants (`{repo}-master.yml`, `{repo}-master-deploy.yml`) must not be used.

Root-level `azure-pipelines.yml` and `azure-pipelines-dev-pr-validation.yml` files must not exist when the pipeline definition is registered to a `.azure-pipelines/workflows/` path. Unregistered root-level YAML files must be removed.

### 10.2.1 Database release bundle
<!-- STD-MARKER: database.10.2.1 -->

The `feature/* → dev` validation path must produce a database release bundle for later promotion and delivery.

The database release bundle must include:

- the DACPAC produced by the validation run
- the schema diff report or the equivalent reviewed deployment report when it is generated before delivery
- all approved pre-deploy scripts required for the release
- all approved post-deploy scripts required for the release
- traceability metadata linking the bundle to the originating commit SHA, source pull request, and validation build run

Production delivery must deploy from this reviewed release bundle rather than reconstructing release content from unrelated sources.

### 10.3 Pipeline Stages
<!-- STD-MARKER: database.10.3 -->

The deploy template must define stages in this order:

1. **Build** — builds the SQL database project via `VSBuild`, validates SQL source for anti-patterns, locates and stages the DACPAC artifact, and publishes test script artifacts. Must not run on pull requests.
2. **Test** — downloads and inspects test script artifacts. Must not run on pull requests.
3. **AnalyzeSchema** — runs only on `refs/heads/main` and non-pull-request triggers. Contains the deployment window check, DACPAC artifact download, diff/script generation, destructive-change detection, and pre-deploy-script enforcement.
4. **DeployAzure** — runs only on `refs/heads/main` and non-pull-request triggers after `AnalyzeSchema` succeeds. Contains the pre-deploy backup, DACPAC deploy, post-deploy corruption check, auto-recovery on failure, and backup cleanup.
5. **PostDeploymentTests** — smoke validation confirming deployment outcome. Runs only after `DeployAzure` succeeds.

### 10.4 Deployment Window
<!-- STD-MARKER: database.10.4 -->

Automated deployments to non-local environments must only run within the configured CST delivery window. The window check, the manual-run bypass behavior, the `skipDeployment` variable pattern, and the required implementation script are defined authoritatively in [`GlobalAzureDevOpsPipelineStandards.md`](GlobalAzureDevOpsPipelineStandards.md) Section 2.3.1. Database pipelines must conform to that section without deviation.

### 10.5 Pre-Deploy Backup and Auto-Recovery
<!-- STD-MARKER: database.10.5 -->

When `enableAutoCorruptionRecovery` is `true`, the pipeline must create a point-in-time database copy before applying the DACPAC and restore it automatically on deploy failure.

**Pre-deploy backup:**

- The backup step runs before the DACPAC deploy step, conditioned on `ne(variables['skipDeployment'], 'true')` and `eq(variables['enableAutoCorruptionRecovery'], 'true')`.
- The backup database name must follow the pattern `{databaseName}_predeploy_{Build.BuildId}_{timestamp}`.
- The backup must be created using `az sql db copy`. The resource group must be resolved dynamically from the SQL Server name via `az resource list`.
- The backup database name and resolved resource group must be saved as pipeline variables (`PreDeployBackupDatabaseName`, `SqlResourceGroup`) for use by subsequent recovery and cleanup steps.

The purpose of the pre-deploy backup is to provide a rollback resource if post-deploy smoke validation or deployment execution indicates that rollback is required.

**Auto-recovery on failure:**

- The auto-recovery step runs with `condition: and(failed(), eq(variables['enableAutoCorruptionRecovery'], 'true'), ne(variables['PreDeployBackupDatabaseName'], ''))`.
- On failure, the current (potentially corrupted) database is renamed with a `_corrupt_{timestamp}` suffix.
- The pre-deploy backup copy is then copied back to the original database name using `az sql db copy`.
- The step must write a pipeline summary and exit with an error to surface the recovery event in the run log.

**Backup cleanup:**

- The cleanup step runs with `condition: and(always(), ne(variables['skipDeployment'], 'true'), eq(variables['enableAutoCorruptionRecovery'], 'true'))` to ensure the temporary backup is deleted regardless of whether the deploy succeeded or the auto-recovery ran.
- If the backup database variable is empty or the database no longer exists, the step must exit cleanly without error.

### 10.6 ADO Pipeline Registration
<!-- STD-MARKER: database.10.6 -->

The Azure DevOps pipeline definition registered for each database repository must have its default branch set to `refs/heads/main`.

### 10.6.1 Manual-Run Gate Bypass
<!-- STD-MARKER: database.10.6.1 -->

Every pipeline stage condition gated on `skipDeployment` or an equivalent window-check variable must still evaluate to true for manual runs. A manual run (`Build.Reason` equal to `Manual`) must never be blocked by a gate that exists only to pause automated, time-window-restricted delivery.

### 10.7 Column Rename and Destructive Change Handling
<!-- STD-MARKER: database.10.7 -->

DACPAC does not recognize a column rename as a rename. It interprets it as a `DROP` of the old column followed by an `ADD` of the new column. This destroys the data in the renamed column unless a pre-deploy script handles the operation explicitly. This section defines the required approach.

Prefer to generate and review the DACPAC schema diff before the delivery pipeline begins. That diff is the decision gate for whether destructive-change handling, pre-deploy scripts, and any companion post-deploy scripts are required. When pre-delivery diff generation is not possible, the delivery pipeline must generate the diff before the deploy step begins.

#### Column Renames

- A column rename must never be performed by simply changing the column name in the database project and relying on DACPAC to apply the change. Doing so will produce a destructive `DROP` and `ADD` pair that deletes column data.
- The correct approach is to add a pre-deploy script that uses `sp_rename` to rename the column before the DACPAC diff is applied. The database project must then reflect the new column name so the DACPAC diff sees no change and generates no `DROP` statement.
- The pre-deploy script must follow this pattern:

```sql
IF COL_LENGTH('dbo.TableName', 'OldColumnName') IS NOT NULL
    AND COL_LENGTH('dbo.TableName', 'NewColumnName') IS NULL
BEGIN
    EXEC sp_rename 'dbo.TableName.OldColumnName', 'NewColumnName', 'COLUMN';
END
```

- The `IF` guard is required. It ensures the rename is idempotent — re-running the pre-deploy script after the rename has already been applied must not error.
- PRs containing a column rename must be labelled `schema-risk`.

#### Destructive Changes

The following schema operations are destructive and must be treated as requiring explicit pipeline enforcement and repository review:

- `DROP TABLE`
- `DROP COLUMN`
- `DROP INDEX` when it removes a required uniqueness or access path that the release depends on
- `ALTER COLUMN ... NOT NULL` on a populated table
- Column or table rename patterns, including `sp_rename` and DACPAC-generated rename-equivalent `DROP` + `ADD` output

When a destructive change is present:

- the pipeline must detect it from the generated DACPAC script before deploy
- the repository must provide a pre-deploy script in the project's pre-deploy script location if data preservation or transition handling is required
- if the required pre-deploy script is missing, the pipeline must fail and the deploy must not continue
- the PR must document the data impact and the intended handling

Destructive changes must never rely on manual pipeline approval as the enforcement mechanism.

---

## 11. Compliance Verification
<!-- STD-MARKER: database.11 -->

**Transactional store (Azure SQL)**

- [ ] No ORM framework is used — all data access is ADO.NET through the repository pattern.
- [ ] No inline SQL of any form appears in any service, repository, or infrastructure class.
- [ ] All database operations execute through stored procedures with `CommandType.StoredProcedure`.
- [ ] All stored procedures follow the `{Entity}_{Verb}` naming convention.
- [ ] `CommandTimeout` is read from application configuration — no hardcoded timeout values.
- [ ] `CancellationToken` is passed to every async ADO.NET method — `OpenAsync`, `ExecuteReaderAsync`, `ExecuteNonQueryAsync`, `ReadAsync`.
- [ ] `null` parameters are passed as `DBNull.Value` — never as C# `null`.
- [ ] `await using` is used for all `SqlConnection`, `SqlCommand`, and `SqlDataReader` instances.
- [ ] Repository methods return materialized types — never a reader or connection.
- [ ] Model construction from `SqlDataReader` is performed in the target type's constructor.
- [ ] `GetOrdinal` is used for all column access — positional index access is not permitted.
- [ ] All new repository methods declare `@TenantId` and `@ClientId` as nullable leading parameters.
- [ ] All boolean columns use `TINYINT` with a default constraint of `((0))` — `BIT` is not used.
- [ ] All string columns use `VARCHAR` — `NVARCHAR` is not used.
- [ ] Column and parameter data types are sized to the domain — no defaulting to `VARCHAR(MAX)`, oversized integers, or `DATETIME2`.
- [ ] Every table has an `INT IDENTITY(1,1)` primary key and a `UNIQUEIDENTIFIER` secondary unique field with a non-clustered unique index.
- [ ] All tables include `CreatedDate`, `CreatedBy`, `UpdatedDate`, `UpdatedBy` audit columns.
- [ ] Soft-deletable tables include `IsArchived TINYINT NOT NULL DEFAULT ((0))`, `ArchivedDate DATETIME NULL`, and `ArchivedBy VARCHAR(100) NULL`.
- [ ] All stored procedures are wrapped in `BEGIN` / `END` and have `SET NOCOUNT ON` as the first statement.
- [ ] All write procedures validate required integer ID parameters as non-null and greater than zero before opening a transaction.
- [ ] All write procedures validate required string parameters using `NULLIF(LTRIM(RTRIM(ISNULL(...))), '')` before opening a transaction.
- [ ] All write procedures validate `TINYINT` boolean parameters as `IN (0, 1)` before opening a transaction.
- [ ] String values written to the database are sanitized with `LEFT(LTRIM(RTRIM(ISNULL(...))), maxLength)` at the point of write.
- [ ] Upsert procedures that match on name or description fields normalize the comparison using `LOWER(LTRIM(RTRIM(...)))` to prevent duplicate rows from case or whitespace variants.
- [ ] Upsert procedures for list objects (dropdowns) resolve an existing ID by normalized name before the `MERGE` when no ID is supplied by the caller.
- [ ] All write procedures log full error context to `dbo.SqlErrorTracking_Insert` in the `CATCH` block and re-raise with `THROW`.
- [ ] Write procedures that produce an inserted row use `OUTPUT inserted.{PrimaryKey}` to return the identity. Archive and soft-delete procedures return `SELECT 1` to confirm success.
- [ ] Read procedures do not use transactions or `TRY/CATCH` unless a write side-effect is present.
- [ ] Write procedures use `SET XACT_ABORT ON` and a single `BEGIN TRANSACTION` / `COMMIT` / `ROLLBACK` block — no nested transactions.
- [ ] Any procedure that may return a large recordset uses paging — unbounded result sets are not permitted.
- [ ] Paged read procedures return the total count as the first result set followed by the paged data using `OFFSET` / `FETCH NEXT`.
- [ ] Dynamic SQL, where present, uses `sp_executesql` with parameterized inputs, wraps all runtime identifiers in `QUOTENAME()`, validates the target object exists with `OBJECT_ID()` before execution, and includes a comment explaining why static SQL is not possible. `EXEC(@sql)` with a concatenated string is not used.
- [ ] Cursors are used only when a set-based alternative is not possible, are declared `FAST_FORWARD READ_ONLY` where applicable, are explicitly closed and deallocated in all exit paths, and include a comment explaining why a set-based approach was not used.
- [ ] `NOLOCK` and `READ UNCOMMITTED` do not appear anywhere in the procedure.
- [ ] Soft-deletable entity queries include `WHERE IsArchived = 0` unless intentionally retrieving archived records.
- [ ] Connection strings are not hardcoded and are not committed to source — retrieved from Key Vault at runtime.
- [ ] Connection pooling is not disabled — `Pooling=false` does not appear in any connection string.
- [ ] Bulk operations use `SqlBulkCopy` with `BatchSize = 100` and explicit column mappings.
- [ ] Bulk chunk processing uses `Parallel.ForEachAsync` with `MaxDegreeOfParallelism` from configuration.
- [ ] All schema changes are committed to the database project and deployed via DACPAC through the release pipeline — no manual DDL applied to non-local environments.
- [ ] The `feature/* → dev` validation path produces a database release bundle containing the DACPAC, reviewed deployment artifacts, approved pre/post scripts, and traceability metadata.
- [ ] The pipeline generates the DACPAC diff script before the deploy stage runs and enforces destructive-change detection plus pre-deploy-script validation before any schema change is applied.
- [ ] `BlockOnPossibleDataLoss` is not set to `false` in any pipeline configuration.
- [ ] `DropObjectsNotInSource` is set to `false` in all pipeline configurations.
- [ ] `GenerateSmartDefaults` is set to `true` in all pipeline configurations.
- [ ] PRs containing destructive schema operations (drop, rename, type or nullability change on populated tables) must include a review of data impact and a pre-deploy or post-deploy script where data must be preserved.
- [ ] Pipeline entrypoint file is named `{repo}-main.yml` and resides in `.azure-pipelines/workflows/`. No `master`-suffixed or root-level pipeline YAML files exist.
- [ ] Pipeline deploy template file is named `{repo}-main-deploy.yml` and resides in `.azure-pipelines/workflows/`. The entrypoint references it via `extends:`.
- [ ] The deployment window check is the first step in the `DeployAzure` job and exits cleanly (green, no error) when outside 8 PM – 5 AM CST on CI-triggered runs. `SucceededWithIssues`, `throw`, and `exit 1` must not be used. See `GlobalAzureDevOpsPipelineStandards.md` Section 2.3.1.
- [ ] Manual pipeline runs bypass the deployment window check and deploy at any time. A CI-triggered run that stopped at the window gate resumes through a manual rerun or the next repository-defined automated pickup when the allowed window opens.
- [ ] All steps following the deployment window check in the `DeployAzure` job include `condition: and(succeeded(), ne(variables['skipDeployment'], 'true'))`.
- [ ] When `enableAutoCorruptionRecovery` is `true`, a pre-deploy backup is created using `az sql db copy` before the DACPAC deploy step runs.
- [ ] The auto-recovery step is conditioned on `failed()` and restores from the pre-deploy backup by renaming the corrupt database and copying the backup back to the original name.
- [ ] The backup cleanup step is conditioned on `always()` and deletes the pre-deploy backup database after the deploy job completes.
- [ ] The ADO pipeline definition references `.azure-pipelines/workflows/{repo}-main.yml` and has its default branch set to `refs/heads/main`.
- [ ] The delivery pipeline preserves traceability from the promoted change set to the database release bundle created during the originating validation run.
- [ ] Column renames are implemented using a `sp_rename` pre-deploy script — the old column name is not simply changed in the database project.
- [ ] The `sp_rename` pre-deploy script includes an idempotent `IF COL_LENGTH` guard so re-running the script does not error.
- [ ] PRs containing column renames are labelled `schema-risk`.
- [ ] The pipeline scans the generated DACPAC script for destructive changes before deploy and fails if a required pre-deploy script is missing.
- [ ] PRs containing `DROP COLUMN` or `DROP TABLE` operations include documentation in the PR description confirming the column or table is not referenced anywhere and the data impact has been assessed.

**Archive store**

- [ ] Archive store platform decision is recorded in `Working/ProjectBacklog.md` before any archive code is written.
- [ ] Section 3.4 (Access Technology) and Section 3.5 (Document Structure) are completed before any archive store code is written.
- [ ] The migration Azure Function reads its age/status threshold from application configuration — no hardcoded thresholds.
- [ ] Migration writes are verified before the source record is deleted from the transactional database — write-then-delete order is enforced.
- [ ] Migration failures do not abort the batch — failed records are logged and skipped; the function continues.
- [ ] The migration function is idempotent — re-running it does not produce duplicate archive entries or data loss.
- [ ] Every archived document carries the original primary key, tenant identifier, client identifier, `ArchivedDate` timestamp, and migration job name.
- [ ] The migration function triggers an index refresh on both the transactional database and the archive store after each batch completes — not per record — and logs but does not roll back on index refresh failure.
- [ ] Read routing implementation is documented in `Working/ProjectBacklog.md` and agreed before the migration job or routing layer is built.
- [ ] Archive read routing is encapsulated in the repository layer — callers are not aware of which store serves the record.

---

## 12. Governance
<!-- STD-MARKER: database.12 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted.
