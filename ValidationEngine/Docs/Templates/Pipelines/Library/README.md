# Library Pipeline Template

This template family is mirrored from the `HttpClientManager` repository's `.azure-pipelines/` tree, which is the canonical Azure DevOps pipeline standard for .NET class library repositories (NuGet packages published to an Azure Artifacts feed).

## Folder Layout

```
Library/
  Dev/
	change-detection.yml   Detects code vs. documentation-only changes
	build.yml              Restores, builds, and packs the NuGet package
	test.yml                Runs unit tests against the build output
	carry-forward.yml       Opens/updates a dev -> main promotion PR
	dev.yml                 PR-gate entrypoint (trigger + pr on `dev`)
  Main/
	change-detection.yml   Detects code vs. documentation-only changes (main mode)
	publish.yml             Packs and publishes the NuGet package to Azure Artifacts
	main.yml                Production entrypoint (trigger on `main`, `pr: none`)
  Variables/
	variables.yml           Shared variables consumed by all Dev/Main templates
```

Unlike the WebApiWebApp and Database template families, the Library family has **no deployment stage**. Publishing a NuGet package to Azure Artifacts is the terminal action of the `Main` pipeline — there is no App Service, slot, or SQL target to deploy to.

## How to Use This Template

1. Copy `Library/` into the target repo's `.azure-pipelines/` folder (create `Dev/`, `Main/`, `Variables/` subfolders as shown above).
2. Replace tokens in `Variables/variables.yml`:
   - `<projectRelativePath>` — path from repo root to the library `.csproj`
   - `<testProjectRelativePath>` — path from repo root to the test project `.csproj`
   - `<artifactsFeedName>` — the Azure Artifacts feed name
   - `<artifactsFeedUrl>` — the full NuGet v3 feed URL for the Azure Artifacts feed
3. Replace `<dotNetSdkVersion>` and `<includePreviewVersions>` in `Dev/build.yml`, `Dev/test.yml`, and `Main/publish.yml` to match the target .NET SDK (e.g., `10.0.x` / `true`).
4. Ensure the Azure Artifacts feed already exists and the pipeline's build service has **Feed Contributor** permission — `NuGetAuthenticate@1` relies on the pipeline's own identity and does not require a separate service connection.

> **Verification:** After resolving tokens, confirm no `<` or `>` characters remain in any file you write. If any do, the file is incomplete — fix it before proceeding.
