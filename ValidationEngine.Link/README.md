# ValidationEngine.Link

Markdown link validation add-on for the
[ValidationEngine](https://github.com/TroyCrowe-TCCoder/ValidationEngine) standards validation
suite, distributed as the `validation-engine-link` .NET global tool.

## What It Is

A standalone .NET global tool that can run entirely on its own, or as an optional add-on invoked
by `validation-engine`. It has no dependency on the core rule engine's models or rendering
pipeline.

## What It Does

Scans markdown files for internal (relative file/anchor) and external (`http`/`https`) links and
reports broken or unreachable ones. It can scope its scan to files changed versus a base branch
(fast, PR-friendly) or run a full repository audit.

## Its Modularity

`ValidationEngine.Link` references `ValidationEngine` for shared repository/config resolution
helpers, but has no dependency on `ValidationEngine.Models` or `ValidationEngine.Reporting` — it
runs and reports independently of the core rule engine. It can be installed and run standalone, or
invoked automatically by `validation-engine` as a sibling process.

## How It Works

1. Resolves the repository root and determines the file scope: either files changed versus a
   base branch, or every markdown file under `--full-audit`.
2. Scans each file for internal links (validated against the local file system/anchors) and
   external links (validated with an HTTP request, subject to `--timeout`).
3. Runs checks concurrently, up to `--concurrency`.
4. Prints each issue to the console and, if `--output` is given, writes them as JSON.
5. Returns an exit code reflecting the outcome.

## Installing

```bash
dotnet tool install --global ValidationEngine.Link
```

## Usage

```bash
validation-engine-link --full-audit
```

| Argument | Description |
|---|---|
| `-RepositoryRoot <path>` | Repository to scan. Defaults to the current directory. |
| `--full-audit` | Scan every markdown file instead of only files changed vs. a base branch. |
| `--base-branch <branch>` | Base branch to diff against when not doing a full audit. |
| `--app <name>` | Application name attached to reported issues; defaults to `validationengine.config.json`'s `SolutionName` or the repository directory name. |
| `--output <path>` | Write issues as JSON to the given path. |
| `--concurrency <n>` | Max concurrent link checks. Defaults to processor count. |
| `--timeout <seconds>` | Per-external-link HTTP timeout, in seconds. Defaults to 10. |

Output is printed to the console as `[LinkType] SourceFile: Link — Issue` for each broken link,
followed by a summary line. The process exits `0` when no issues are found, `1` when issues are
found, and `2` on an unrecoverable I/O or invocation error.

## License

MIT — see [LICENSE](https://github.com/TroyCrowe-TCCoder/ValidationEngine/blob/main/LICENSE).

MIT — see [LICENSE](https://github.com/TroyCrowe-TCCoder/ValidationEngine/blob/main/LICENSE).
