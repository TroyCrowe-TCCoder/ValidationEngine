# ValidationEngine

Automated repository standards validation suite: deterministic mechanical rule checks,
AI-assisted manual-only rule evaluation, markdown link validation, and Roslyn compile-time
analyzers, all driven by a shared, editable standards corpus.

## Purpose

ValidationEngine enforces the rules defined in a companion standards repository (see
[Standards Source](#standards-source) below) across a codebase: at compile time via analyzers,
at validation time via deterministic and AI-assisted rule engines, and for documentation via
link validation.

## Projects

| Project | Description |
|---|---|
| `ValidationEngine` | Core deterministic mechanical rule engine and orchestration. |
| `ValidationEngine.Agent` | AI-assisted evaluation for manual-only / judgment-based rules. |
| `ValidationEngine.Link` | Markdown link validation (internal + external links). |
| `ValidationEngine.Analyzers` | Roslyn analyzer package family (compile-time enforcement), organized by domain folders/namespaces. |

## Standards Source

This repository does not itself contain the standards corpus. It expects a sibling clone of
the `GlobalStandards` repository, referenced via relative path:

```
../GlobalStandards/Docs/Standards
```

Clone both repositories side by side:

```
repos/
  GlobalStandards/
  ValidationEngine/
```

Standards rules remain the authoritative "fuel source" — human-readable and agent-readable
markdown files that can be added to, edited, or removed independently of this tooling.

## License

MIT — see [LICENSE](LICENSE). Contributions are welcome; all changes are gated on maintainer
approval.
