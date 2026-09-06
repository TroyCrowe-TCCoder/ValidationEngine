# CaptiveInnovations Rollout Packages

This folder contains rollout-group artifacts derived from the raw Azure source captures under `../source/`.

## Package Model

- `source/` contains raw per-resource captures and the full resource-group export.
- `packages/` groups those source artifacts into rollout units that deploy together.
- Package manifests describe deployment intent, dependencies, and the source artifacts that feed each rollout unit.
- ARM is the active format for this suite. Bicep and Terraform folders remain placeholders until the suite rollout model is parameterized for those formats.

## Deployment Order

1. Shared identities
2. Shared hosting
3. Shared monitoring
4. Shared storage and eventing
5. Shared key vault
6. Shared SQL and data
7. Application packages

## Notes

- These manifests are rollout planning artifacts, not fully parameterized deployable templates.
- Raw `az resource show` captures remain the authoritative source references for package composition.
- Package refinement is expected as application-level automation is introduced.
