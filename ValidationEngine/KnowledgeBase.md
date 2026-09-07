# ValidationEngine Knowledge Base

A running record of notable design decisions, rationale, and "why did we do it this way"
context for `ValidationEngine` that isn't captured by planning documents (future intent) or
governance standards (process rules). Entries are appended as decisions are made; existing
entries are not rewritten after the fact.

---

## Suppression vs. removing a rule from the run (Addendum handling)

**Date:** 2026-05 (see [issue #6](https://github.com/TroyCrowe-TCCoder/ValidationEngine/issues/6))

**Context:** `ExclusionAddendumReader` parses repository-local addenda (`Standards/*.md`) and
resolves which global rule markers should not be reported as violations for a given repository.
The question came up: why suppress the finding rather than simply dropping the rule from the set
of checks that run?

**Decision:** Every mechanical check in `MechanicalValidationRules` still executes. An addendum
only suppresses the *finding*, at the point it would be reported, via
`ExclusionAddendumReader.IsMarkerExcluded`. The rule itself is never removed from the run.

**Rationale:**

1. **Expiry enforcement.** Addenda carry a `Planned Review Date`. If it lapses, the exclusion is
   reported as its own finding (`ExpiredExclusion`) instead of silently expiring. This requires
   the underlying rule to still conceptually exist and run.
2. **Auditability.** Suppressing at the reporting boundary keeps the suppressed finding in memory
   during the run — the natural extension point for future reporting on what was suppressed and
   why, without re-architecting the rule engine.
3. **Forward compatibility with replacement addenda.** Not every addendum means "this rule
   doesn't apply here" — some mean "this rule applies in a modified form for this repository."
   Suppression is a point of adjudication for a finding, not a point of removal for a rule. That
   distinction is what will allow a future replacement-addendum type (see
   [issue #7](https://github.com/TroyCrowe-TCCoder/ValidationEngine/issues/7)) to substitute an
   alternate, still-enforced check instead of only turning validation off entirely.

**Also fixed in the same change:** the `MarkerIdPattern` regex previously required
`**Marker ID:**` at the exact start of a line, so bullet-style deviation entries
(`- **Marker ID:** ...`) were never matched and their overridden rules were never suppressed. The
regex now accepts an optional leading `-`/`*` bullet prefix.
