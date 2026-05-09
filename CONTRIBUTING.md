# Contributing

Thanks for helping keep Bob Shared Mobility reviewable and production-minded.

## Workflow

1. Branch from `main`.
2. Keep changes scoped to one behavior, asset batch, or documentation task.
3. Preserve Unity `.meta` files whenever assets move or change.
4. Run the relevant Unity scene checks before opening a pull request.
5. Fill out the pull request template and include regression notes.

## Project Rules

- Project-owned assets live under `Assets/_Project`.
- Third-party and package content stays outside `Assets/_Project`.
- C# scripts should remain focused and below the project line-count guardrail.
- Input access should flow through project input wrappers.
- Scene-wide discovery should stay isolated to documented compatibility or diagnostic code.
- UI navigation should prefer route tables, services, and explicit component references over persistent scene calls.
- Large media assets must follow `docs/MEDIA_ASSET_GOVERNANCE.md`.

## Validation

Before asking for review, use:

- `docs/DELIVERY_REGRESSION_CHECKLIST.md`
- `docs/PROJECT_INDUSTRIALIZATION_AUDIT.md`
- Unity Play Mode validation of `Assets/_Project/Scenes/BobSharedMobility.unity`

GitHub Actions also runs static governance checks for repository hygiene.
