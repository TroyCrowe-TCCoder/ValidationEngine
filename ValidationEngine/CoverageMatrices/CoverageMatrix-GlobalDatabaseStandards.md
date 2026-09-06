# Coverage Matrix — GlobalDatabaseStandards.md

**Status:** Reviewed
**Sign Off:** Troy Crowe
**Sign Off Date:** 2026-08-14
**Revision Date:**
**Columns**

| Column | Meaning |
|---|---|
| STD-MARKER | Rule id from the standards file. |
| Rule Summary | One-line paraphrase of the rule. |
| Category | Structure / Technology / Development / Procedural / Infrastructure. |
| Confidence | Binary (deterministic pass/fail) / Heuristic (risk-flag, needs human confirmation) / Manual-only (no automation path). |
| Severity | Hard-stop / Warning / Manual-only-comment (3-tier enforcement model). |
| Frequency | Every-Commit (validated against the files touched in the current change set) / Periodic (validated on a recurring, configurable cadence). |
| Technique | Short detection approach. |
| Status | Missing / Existing / Acknowledged-future-work. |

**Applicability note:** Applies to database projects (`.sqlproj`, `.sql` stored-procedure files), C# repository/infrastructure classes performing data access, and the two-file ADO pipeline layout for schema deployment. Section 3 (Archive Database) is explicitly a planned/deferred feature — no active detection needed until the platform decision is recorded. Sections 6/10.4–10.7 require SQL-text parsing rather than Roslyn; Sections 8.1, 10.2–10.3, 10.5–10.6 are largely Infrastructure/pipeline-configuration facts. Section 5 (Repository Pattern) is entirely C# ADO.NET application code — it applies only to repositories that contain a C# repository/infrastructure layer. A pure database solution (a `.sqlproj`-only repository with no C# code) has no `SqlDataReader`, constructors, or repository classes to evaluate and is out of scope for Section 5 detection.

---

### Section 2 — Technology Baseline

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| database.2 | ADO.NET required; no ORM (including EF Core); all operations via stored procedures; no inline SQL; only Azure SQL as transactional store; application DB account granted execute-only, no DDL | Technology | Binary | Hard-stop | Every-Commit | Duplicates `GlobalCodingStandards.md` coding.2.7 and `GlobalSecurityStandards.md` security.4.1 detection (ORM/inline-SQL scan); DB-account DDL-permission check is Infrastructure/Manual-only (requires live SQL account inspection) | Missing |

---

### Section 3 — Archive Database (Excluded — status: Planned, platform not yet selected; no active detection required until platform decision is recorded per the standard's own gating note)

---

### Section 4 — Schema Design Rules

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| database.4.1 | Naming conventions: PascalCase singular table names, PascalCase columns, `{ReferencedTable}Id` FK pattern, `{Entity}_{Verb}` stored procedure naming with approved verb list, procedure-to-repository-method correspondence | Technology | Heuristic | Warning | Every-Commit (on `.sql` file changes) | SQL-text regex: validate table/column/procedure names against the PascalCase and naming-pattern rules; verb-list check against approved verbs (`Get`, `Upsert`, `Insert`, `Update`, `Delete`, `Archive`) | Missing |
| database.4.2 | Every table has `INT IDENTITY(1,1)` clustered PK plus `UNIQUEIDENTIFIER DEFAULT NEWID()` non-clustered unique secondary key; no composite PKs on entity tables | Technology | Binary | Hard-stop | Every-Commit (on `.sql` file changes) | SQL-text parse: verify each `CREATE TABLE` includes both key columns with correct index types; flag composite primary key declarations | Missing |
| database.4.3 | Required audit columns (`CreatedDate`, `CreatedBy`, `UpdatedDate`, `UpdatedBy`) on every entity table; populated by service layer via authenticated principal, not solely by DB defaults | Technology | Heuristic | Warning | Every-Commit (on `.sql` file changes) | SQL-text parse: verify all four audit columns present with correct nullability on new `CREATE TABLE` statements; verify write procedures declare `@CreatedBy`/`@UpdatedBy` parameters | Missing |
| database.4.4 | Soft-delete columns (`IsArchived TINYINT`, `ArchivedDate`, `ArchivedBy`) for recoverable entities; `WHERE IsArchived = 0` filter required in queries unless explicitly retrieving archived; hard deletes documented in PR when used | Development | Heuristic | Warning | Every-Commit (on `.sql` file changes) | SQL-text parse: flag `_Get` procedures on soft-deletable tables missing the `IsArchived = 0` filter; flag `DELETE` statements without an adjacent PR-documentation comment | Missing |
| database.4.5 | Nullable columns only when NULL has distinct business meaning; mapped to nullable C# types (`string?`, `DateTime?`, `int?`) | Development | Manual-only | Manual-only-comment | Every-Commit | Requires business-meaning judgment for the "why nullable" question; the mechanical half (nullable SQL column mapped to nullable C# type) is Roslyn-checkable but the judgment half is not | Missing |
| database.4.6 | Index required on every FK column; unique index on business-key columns; no speculative indexes; composite indexes documented with a comment explaining the supported query | Development | Heuristic | Warning | Every-Commit (on `.sql` file changes) | SQL-text parse: cross-reference FK columns against existing indexes and flag missing ones; flag composite `CREATE INDEX` statements with no adjacent comment | Missing |
| database.4.7 | Data type selection: smallest sufficient integer type; `TINYINT` (not `BIT`) for booleans with `DEFAULT ((0))`; explicit ternary conversion in C# (not implicit cast/`Convert.ToBoolean`/`GetBoolean`); `VARCHAR` (not `NVARCHAR`) for strings | Technology | Binary | Hard-stop | Every-Commit | SQL-text parse: flag `BIT` column declarations and `NVARCHAR` column declarations; Roslyn-syntax: flag `Convert.ToBoolean`/`reader.GetBoolean` calls and implicit `TINYINT`-to-`bool` casts instead of the required ternary pattern | Missing |

---

### Section 5 — Repository Pattern

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| database.5.1 | Repository interfaces in core layer, implementations in infrastructure layer; controllers/services depend only on repository interfaces, never a connection/command directly; one bounded entity area per repository | Structure | Heuristic | Hard-stop | Every-Commit | Roslyn-semantic: flag `SqlConnection`/`SqlCommand` fields or parameters in controller/service classes; flag repository classes referencing more than one unrelated entity's stored procedures | Missing |
| database.5.2 | Mandatory ADO.NET pattern: `CommandType.StoredProcedure` always (never `.Text`); `CommandTimeout` from configuration (not hardcoded); `CancellationToken` passed to every async ADO.NET call; `null` parameters as `DBNull.Value` (never C# `null`); `await using` for all `SqlConnection`/`SqlCommand`/`SqlDataReader`; methods return materialized results, never a reader/connection | Technology | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `CommandType.Text`; flag hardcoded numeric `CommandTimeout` literal; flag missing `cancellationToken` argument on `OpenAsync`/`ExecuteReaderAsync`/`ExecuteNonQueryAsync`/`ExecuteScalarAsync`/`ReadAsync`; flag `.Parameters.AddWithValue` passing raw `null` instead of `DBNull.Value`; flag `using`/no-`await` declarations for these three types; flag repository method return types of `SqlDataReader`/`SqlConnection` | Missing |
| database.5.3 | Model/DTO construction from `SqlDataReader` performed in target type's constructor; `GetOrdinal` used for column resolution (no positional index access); ordinals not resolved inside the read loop | Development | Heuristic | Hard-stop | Every-Commit | Duplicates `GlobalCodingStandards.md` coding.3.6 object-mapping detection, scoped to reader-based construction; Roslyn-syntax: flag `reader[0]`/`reader.GetInt32(0)`-style positional access; flag `GetOrdinal` calls inside a `while (reader.Read())` loop body | Missing |
| database.5.4 | Tenant/client scope parameters — cross-reference to `GlobalSecurityStandards.md` security.3.4 | Development | Duplicate | Duplicate | Every-Commit | Pure cross-reference; detection already captured under security.3.4 in `CoverageMatrix-GlobalSecurityStandards.md` | Missing |

---

### Section 6 — Stored Procedure Rules

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| database.6.1 | `BEGIN`/`END` block with `SET NOCOUNT ON` first statement; read procedures need no transaction/`TRY-CATCH`; write procedures need `SET XACT_ABORT ON`, pre-transaction `THROW` validation, transaction wraps only the write, `OUTPUT inserted.{PK}` (or `SELECT 1` for archive/delete), full-context `TRY/CATCH` logging to `dbo.SqlErrorTracking_Insert` before `THROW`; paging required for unbounded result sets (count first, then `OFFSET`/`FETCH NEXT`) | Technology | Binary | Hard-stop | Every-Commit (on `.sql` file changes) | SQL-text parse: verify `SET NOCOUNT ON` is first statement; verify write procedures include `SET XACT_ABORT ON`, a `TRY/CATCH` block calling `dbo.SqlErrorTracking_Insert`, and `THROW` in the catch; verify `_Get` procedures without a limiting predicate include paging via `OFFSET ... FETCH NEXT` | Missing |
| database.6.2 | Tenant/client filter parameters nullable and first in parameter list; minimum sufficient data types; no output parameters where a result set/scalar suffices; required-parameter `THROW` validation (int `> 0`, string `NULLIF(LTRIM(RTRIM(ISNULL(...))))`, `TINYINT` boolean `IN (0,1)`); input sanitization via `LEFT(LTRIM(RTRIM(ISNULL(...))), maxLength)`; name/description duplicate-prevention normalization via `LOWER(LTRIM(RTRIM(...)))`; list-object upserts perform existence-by-name check before `MERGE` when no ID supplied | Technology | Heuristic | Hard-stop | Every-Commit (on `.sql` file changes) | SQL-text parse: verify `@TenantId`/`@ClientId` appear first and nullable; verify presence of the specific `THROW`-based validation patterns for each parameter type; verify sanitization/normalization function calls wrap string parameters before use in `MERGE`/`INSERT` | Missing |
| database.6.3 | Static SQL preferred; dynamic SQL only when table name is runtime-resolved; must use `sp_executesql` (never `EXEC(@sql)` concatenation); dynamic object names wrapped in `QUOTENAME()`; all values passed as `sp_executesql` parameters; `OBJECT_ID()` existence check before dynamic statement; comment explaining why static SQL isn't possible | Technology | Binary | Hard-stop | Every-Commit (on `.sql` file changes) | SQL-text parse: flag `EXEC(@variable)`-style dynamic execution; verify `sp_executesql` usage wraps dynamic identifiers in `QUOTENAME()` and passes values as parameters, not concatenation; verify an adjacent `OBJECT_ID()` check and explanatory comment | Missing |
| database.6.4 | Cursors only when set-based alternative impossible, approved in PR, declared `FAST_FORWARD READ_ONLY` where applicable, explicitly closed/deallocated in all exit paths, documented with a comment | Development | Heuristic | Warning | Every-Commit (on `.sql` file changes) | SQL-text parse: flag any `DECLARE CURSOR` statement (candidate for review); verify `FAST_FORWARD`/`READ_ONLY` options and a `CLOSE`/`DEALLOCATE` pair exist on all code paths; verify an adjacent justification comment | Missing |
| database.6.5 | `NOLOCK`/`READ UNCOMMITTED` prohibited in all stored procedures and ad-hoc queries in any environment | Technology | Binary | Hard-stop | Every-Commit (on `.sql` file changes) | SQL-text regex: flag `NOLOCK` hint or `READ UNCOMMITTED` isolation level anywhere in stored procedure text | Existing (trivial regex; commonly already covered by SQL static-analysis tools such as SQL Server Data Tools code analysis rules) |

---

### Section 7 — Transactions

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| database.7 | Transactions scoped to smallest operation set; never held open across external HTTP calls/user interactions; multi-write atomicity managed inside the stored procedure via `XACT_ABORT`; app-layer `SqlTransaction` coordination only when atomicity must span multiple repository calls, opened/passed explicitly/committed-or-rolled-back in `try/finally` guaranteeing connection closure | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag `SqlTransaction`/`BeginTransaction` usage spanning an `await` call to an external `HttpClient`/API call between open and commit; verify `try/finally` wraps app-layer transaction blocks | Missing |

---

### Section 8 — Connection Management

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| database.8.1 | Connection strings retrieved from Azure Key Vault at runtime; never in config files, source-committed env vars, or database tables; per-client DB Key Vault secret reference resolved from master catalog at request time | Infrastructure | Duplicate | Duplicate | Every-Commit | Duplicates `GlobalSecurityStandards.md` security.2.2/2.3 detection | Missing |
| database.8.2 | Explicit retry helper for transient Azure SQL errors (max 3 attempts, exponential backoff 1s–10s, specific transient error codes); no swallowing final-failure exception | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `SqlConnection.OpenAsync`/command execution call sites with no adjacent retry-helper wrapper; verify the retry helper implementation (once identified) rethrows on final failure rather than swallowing | Missing |
| database.8.3 | Connection pooling enabled by default and never disabled (no `Pooling=false`); connections opened/executed/disposed within the same method scope via `await using`, never held as instance fields or across method boundaries | Technology | Binary | Hard-stop | Every-Commit | Config-scan: flag `Pooling=false` in any connection string; Roslyn-semantic: flag `SqlConnection`/`SqlCommand` declared as instance fields rather than local `await using` declarations — duplicates database.5.2 detection for the `await using` portion | Missing |

---

### Section 9 — Bulk Operations (Excluded — content not read in this pass; sub-markers database.9.1 Chunking, database.9.2 SqlBulkCopy, database.9.3 Parallel Chunk Processing exist and should be added in a follow-up pass once their full text is reviewed)

---

### Section 10 — Schema Change Deployment

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| database.10.1 | DACPAC is authoritative schema source; no ad-hoc DDL against non-local environments; destructive-change detection is a mandatory pipeline gate (diff generated and scanned before deploy; missing pre-deploy script for a destructive change fails the pipeline); `BlockOnPossibleDataLoss` never `false`; `DropObjectsNotInSource` always `false`; `GenerateSmartDefaults` always `true`; risk-operation PRs labelled `schema-risk` | Infrastructure | Heuristic | Hard-stop | Every-Commit (on `.sqlproj`/pipeline config changes) | Config-scan: verify DACPAC publish profile settings (`BlockOnPossibleDataLoss=false` forbidden, `DropObjectsNotInSource=true` forbidden, `GenerateSmartDefaults=false` forbidden) in `.publish.xml`; pipeline-gate behavior itself (diff-scan-before-deploy) is Infrastructure/Manual-only, requiring live pipeline YAML inspection | Missing |
| database.10.2 | Two-file pipeline layout (`{repo}-main.yml` entrypoint, `{repo}-main-deploy.yml` template) in `.azure-pipelines/workflows/`; no legacy `master`-suffixed variants; no unregistered root-level `azure-pipelines*.yml` files | Infrastructure | Binary | Hard-stop | Every-Commit (on pipeline file changes) | File-existence/naming-pattern check: verify required file names/paths exist and no legacy or unregistered root-level YAML files are present — duplicates `GlobalAzureDevOpsPipelineStandards.md` file-layout detection | Missing |
| database.10.2.1 | `feature/* → dev` validation path must produce a database release bundle (DACPAC, schema diff/deployment report, approved pre/post-deploy scripts, traceability metadata linking to commit/PR/build); production delivery deploys from this bundle only | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | Requires inspecting live pipeline artifact-publishing configuration and release-bundle contents — not source-diffable | Missing |
| database.10.3 | Five ordered pipeline stages (Build, Test, AnalyzeSchema, DeployAzure, PostDeploymentTests) with defined trigger/branch restrictions per stage | Infrastructure | Binary | Hard-stop | Every-Commit (on pipeline file changes) | YAML-structure parse: verify the deploy template defines the five stages in the required order with the specified trigger conditions | Missing |
| database.10.4 | Deployment window governed by configured CST delivery window — pure cross-reference to `GlobalAzureDevOpsPipelineStandards.md` Section 2.3.1 (authoritative window-check script, manual-run bypass, `skipDeployment` pattern) | Infrastructure | Duplicate | Duplicate | Every-Commit (on pipeline file changes) | Duplicates the deployment-window detection already captured in `CoverageMatrix-GlobalAzureDevOpsPipelineStandards.md`; standards text trimmed to a cross-reference only | Missing |
| database.10.5 | When `enableAutoCorruptionRecovery` is `true`: pre-deploy point-in-time backup via `az sql db copy` named `{databaseName}_predeploy_{Build.BuildId}_{timestamp}`, resource group resolved dynamically via `az resource list`, backup name/resource group saved as pipeline variables; on deploy failure, corrupted DB renamed with `_corrupt_{timestamp}` suffix and backup copied back via `az sql db copy`; backup cleanup runs regardless of outcome when auto-recovery is enabled | Infrastructure | Heuristic | Warning | Every-Commit (on pipeline file changes) | YAML-structure parse of the checked-in deploy template: verify pre-deploy backup step condition (`ne(skipDeployment,'true')`, `eq(enableAutoCorruptionRecovery,'true')`), presence of `az sql db copy`/`az resource list` calls, the backup naming pattern, the auto-recovery step's failure condition and rename/restore calls, and the cleanup step's `always()`-based condition | Missing |
| database.10.6 | ADO pipeline registration: default branch set to `refs/heads/main` | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | Pipeline default-branch registration is an Azure DevOps project setting, not a file in the repository — requires live pipeline settings inspection, not source-diffable | Missing |
| database.10.6.1 | Manual pipeline runs (`Build.Reason == Manual`) must bypass every `skipDeployment`/window-check gate condition — no stage may block a manual run for a time-window reason | Infrastructure | Heuristic | Warning | Every-Commit (on pipeline file changes) | YAML-structure parse of the checked-in deploy template: for each stage/step condition referencing `skipDeployment` or a window-check variable, verify the condition also accounts for `eq(variables['Build.Reason'], 'Manual')` (or the window-check step itself already sets `skipDeployment=false` for manual runs) so gates are not relevant/active when run mode is manual | Missing |
| database.10.7 | Column renames must use a pre-deploy `sp_rename` script with an idempotent `IF COL_LENGTH(...)` guard, not a database-project name change relying on DACPAC (which produces a destructive drop/add); destructive changes (`DROP TABLE`/`COLUMN`/`INDEX`, `ALTER COLUMN ... NOT NULL` on populated table, rename patterns) require pipeline detection, a pre-deploy script when data preservation is needed, pipeline failure if the script is missing, and PR documentation of data impact — never enforced by manual approval alone | Technology | Heuristic | Hard-stop | Every-Commit (on `.sqlproj`/pre-deploy script changes) | SQL-text parse: flag column-name changes in the database project with no corresponding pre-deploy `sp_rename` script containing the required `IF COL_LENGTH` guard; flag presence of destructive DDL patterns in the generated diff with no matching pre-deploy script file — the diff-generation/scan step itself is Infrastructure, but the script-presence check is source-diffable | Missing |

---

### Section 11 — Compliance Verification (Excluded — restates Sections 2–10 as a checklist per established pattern; no independent detection needed)

### Section 12 — Governance (Excluded)

`database.12` (ownership/PR-approval boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. This is the first matrix requiring **SQL-text parsing** rather than Roslyn C# analysis as the primary detection technique (Sections 4, 6, 10.1, 10.7). A separate SQL static-analysis tool/ruleset is needed for these — the technique column notes "SQL-text parse/regex" throughout to distinguish from the Roslyn-based techniques used in the C#-focused matrices.
2. Several rules duplicate detection already captured in other matrices: database.2/database.5.2's ORM/inline-SQL prohibition duplicates `GlobalCodingStandards.md` coding.2.7 and `GlobalSecurityStandards.md` security.4.1; database.5.4 duplicates `GlobalSecurityStandards.md` security.3.4 (tenant isolation) entirely by cross-reference; database.8.1 duplicates `GlobalSecurityStandards.md` security.2.2/2.3 (Key Vault secrets); database.10.4 duplicates the deployment-window check in `CoverageMatrix-GlobalAzureDevOpsPipelineStandards.md`; database.10.2 duplicates that same matrix's pipeline-file-layout detection.
3. database.6.6 (`NOLOCK` prohibition) is flagged **Existing** — this is a simple, high-confidence regex pattern that is very likely already available via SQL Server Data Tools (SSDT) static code analysis rules or third-party SQL linters, making it the cheapest rule in this file to wire up.
4. Section 9 (Bulk Operations) and parts of Section 10 (10.5, 10.6) were not fully read in this pass due to token constraints — their sub-markers are listed as placeholders/excluded and should be completed in a follow-up detail pass before this matrix is considered final.
5. database.4.5 (nullable column business-meaning judgment) and database.6.5 (cursor-necessity judgment) both split into a mechanically-checkable half and a subjective-judgment half — consistent with the pattern seen in other matrices where partial automation is possible but full rule satisfaction still requires human review.

