# ValidationEngine.Analyzers

Roslyn analyzer package family for the [ValidationEngine](https://github.com/TroyCrowe-TCCoder/ValidationEngine)
standards validation suite, providing compile-time enforcement of the same standards corpus the
runtime engine validates against.

## What's in this package

A set of `DiagnosticAnalyzer` implementations, organized by domain (coding, security, caching,
database, logging, performance, testing, etc.), that flag standards violations directly in the
IDE and on build — before code is ever committed or run through `validation-engine`.

## Installing

This is a development dependency, not a runtime library. Add it to any project you want analyzed:

```bash
dotnet add package ValidationEngine.Analyzers
```

or in the `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="ValidationEngine.Analyzers" Version="1.0.0" PrivateAssets="all" />
</ItemGroup>
```

`PrivateAssets="all"` is recommended so the analyzer doesn't flow as a transitive dependency to
consumers of your own package.

## How it works

- Packed with `IncludeBuildOutput=false` and `DevelopmentDependency=true` — the analyzer DLL is
  placed under `analyzers/dotnet/cs` in the package and only runs at compile time, it is never
  shipped in your application's output.
- Each analyzer corresponds to one or more rules in the shared `Docs/Standards` corpus, so the
  same rule can be enforced live in the IDE (via this package) and again at validation time (via
  `validation-engine`), keeping both enforcement paths consistent.
- Diagnostics are reported with `AnalyzerReleases.Shipped.md` / `AnalyzerReleases.Unshipped.md`
  tracking, following standard Roslyn analyzer release conventions.

## Relationship to the rest of the suite

This package has no dependency on and is not depended on by `ValidationEngine`,
`ValidationEngine.Agent`, `ValidationEngine.Link`, `ValidationEngine.Models`, or
`ValidationEngine.Reporting`. It is an independent, compile-time-only enforcement mechanism for
the same standards corpus the rest of the suite validates at "run" time.

## License

MIT — see [LICENSE](https://github.com/TroyCrowe-TCCoder/ValidationEngine/blob/main/LICENSE).
