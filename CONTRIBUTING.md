# Contributing

Thank you for your interest in contributing to RealTimeDashboard. Contributions that improve clarity, testing, and recruiter-facing polish are especially welcome.

## How to contribute
1. Fork the repository and create a feature branch from `main`.
2. Make small, focused changes with clear commit messages.
3. Open a pull request describing the change and its motivation.

## Branch naming
- Feature: `feature/<short-descriptive-name>`
- Fix: `fix/<short-descriptive-name>`
- Chore/docs: `chore/<short>` or `docs/<short>`

Examples:
- `feature/csv-validation`
- `fix/upload-null-file`
- `docs/api-spec-update`

## Commit conventions
- Use imperative, present-tense messages (e.g., "Add CSV validation").
- Prefix commits lightly (optional): `feat:`, `fix:`, `docs:`, `chore:`.

## Pull request expectations
- Base branch: `main`
- Include a short description and a screenshot or GIF for UI changes.
- Link related issues if applicable.
- Keep PRs focused and small; large refactors should be discussed first.

## Testing expectations
- New features and bug fixes must include unit tests where applicable.
- Tests use xUnit; run locally via:
  - `dotnet test`
- CI validates build and test passing using GitHub Actions.

## Code style
- Follow existing project style (C# conventions, consistent naming).
- Keep code readable and well-documented.

## Reporting issues
- Use GitHub Issues with a clear title and reproduction steps.
- Tag with labels (bug, enhancement, question) where appropriate.
