# Security Policy

## Supported Branch

Security fixes target `main`.

## Reporting A Vulnerability

Do not open a public issue for a suspected vulnerability. Contact the repository owner privately through GitHub with:

- A clear description of the issue.
- Steps to reproduce.
- Affected files, scenes, or assets.
- Any known workaround.

The maintainer will review the report, confirm impact, and coordinate a fix before public disclosure where appropriate.

## Project-Specific Risk Areas

- Voice input handling and microphone device selection.
- Local file paths and media playback.
- OpenAI API keys used by optional voice transcription.
- Wit/Meta Voice SDK `serverToken` values in `ProjectSettings/wit.config`.
- Third-party Unity plugins and package updates.
- Large binary assets and imported media metadata.

## Secret Handling

- Do not commit API keys, server tokens, Unity license files, or `.env` files.
- Use local environment variables for reviewer credentials, especially `OPENAI_API_KEY`.
- Keep `ProjectSettings/wit.config` token fields empty unless a private fork deliberately manages them.
- Rotate any credential that has been pushed to a public repository.

See `docs/SETUP_AND_SECRETS.md` for the exact setup locations.
