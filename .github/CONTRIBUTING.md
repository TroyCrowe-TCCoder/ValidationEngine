# Contributing to ValidationEngine

Thanks for your interest in ValidationEngine! This project accepts community contributions
through a fork-and-pull-request model.

## How to contribute

1. **Fork** this repository to your own GitHub account.
2. **Branch** from `main` in your fork (e.g. `feature/my-change` or `fix/my-bug`).
3. Make your change, following the coding conventions already used in the codebase.
4. Add or update tests for any behavior change.
5. Open a **pull request** back to `TroyCrowe-TCCoder/ValidationEngine` `main`, describing what
   changed and why.

Direct pushes to this repository are not available to external contributors — all changes must
go through a pull request from a fork, and every pull request requires maintainer review and
approval before merge.

## Reporting bugs / requesting features

Use the issue templates:

- **Bug report** — for something that isn't working as documented.
- **Feature request** — for new capabilities or improvements.

Please search existing issues first to avoid duplicates.

## Pull request expectations

- Keep pull requests focused on a single change/topic.
- Ensure the solution builds and all tests pass locally before opening the PR
  (`dotnet build ValidationEngine.slnx` and `dotnet test ValidationEngine.slnx`).
- Follow the existing project structure and dependency boundaries — see the project table in
  [README.md](README.md) for how `ValidationEngine`, `ValidationEngine.Agent`,
  `ValidationEngine.Link`, `ValidationEngine.Models`, `ValidationEngine.Reporting`, and
  `ValidationEngine.Analyzers` relate to each other.
- CI (GitHub Actions) runs build and test automatically on pull requests; a PR
  cannot be merged if CI fails.

## Code of Conduct

This project follows the [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you agree to
abide by it.

## Repository administration (maintainer reference)

The following GitHub repository settings enforce the contribution model described above and must
be configured once in **Settings** on the GitHub repository (not expressible in a committed file):

- **Branch protection on `main`**: require pull request reviews before merging, require status
  checks to pass, and disable direct pushes (including for maintainers, if desired).
- **Repository access**: no direct write access granted to non-maintainers; all external
  contributions arrive as pull requests from forks.
- **Issues**: enabled, with the bug/feature templates in `.github/ISSUE_TEMPLATE/`.
