# Setup And Secrets

This project should open and run its baseline demo without private credentials. Optional online services must be configured locally by each developer or reviewer.

## Required Local Setup

- Unity `2022.3.53f1`.
- Internet access for Unity Package Manager to restore packages from `https://packages.unity.com`.
- A local microphone device only if testing real voice transcription.
- Normal Unity editor platform permissions for microphone and video playback.

## OpenAI Voice Transcription

Real voice transcription is optional. The main scene can still be reviewed through keyboard shortcuts and the voice command injection backdoor without an OpenAI API key.

Runtime owner:

- Scene: `Assets/_Project/Scenes/BobSharedMobility.unity`
- Hierarchy object: `System_VoiceCommand`
- Component: `VoiceCommandRecognizer`
- Script: `Assets/_Project/Source/Modules/Bob/VoiceCommandRecognizer.cs`
- Endpoint: `https://api.openai.com/v1/audio/transcriptions`
- Model field currently sent by code: `whisper-1`

Recommended local setup:

1. Create an OpenAI API key in the OpenAI dashboard.
2. Set it as a local environment variable named `OPENAI_API_KEY`.
3. Restart Unity so the editor process can read the environment variable.
4. Press and hold the configured voice input shortcut in Play Mode.

PowerShell session example:

```powershell
$env:OPENAI_API_KEY = "your-local-key"
```

Windows persistent user example:

```powershell
setx OPENAI_API_KEY "your-local-key"
```

macOS/Linux shell example:

```bash
export OPENAI_API_KEY="your-local-key"
```

Fallback for one-off local testing:

1. Select `System_VoiceCommand` in the scene.
2. Paste the key into `VoiceCommandRecognizer > OpenAI > Api Key`.
3. Test locally.
4. Revert that scene change before committing.

Do not commit a real API key into a Unity scene, prefab, ScriptableObject, source file, `.env` file, or GitHub workflow.

## Voice Command Injection Without API Access

Use this when reviewing Bob command behavior without microphone or network access:

- Select `Project_Runtime`.
- Use `RuntimeDiagnosticsHub > Voice Command Diagnostics`.
- Run the component context menu `Backdoor/Inject Voice Command`.

This path bypasses microphone recording and does not need an OpenAI API key.

## Microphone Notes

`VoiceCommandRecognizer` selects the first device from `Microphone.devices` and logs it through `ProjectLog`. If the wrong device is selected, change the OS default device or extend the component before delivery to expose an explicit device selector.

Keep `saveDebugWav` disabled for normal review. When enabled, the debug WAV is written to `Application.persistentDataPath` and may contain private speech.

## Wit / Meta Voice SDK Settings

`ProjectSettings/wit.config` is kept with empty `serverToken` values in this repository. The current production path uses the project-owned `VoiceCommandRecognizer` OpenAI transcription flow, not committed Wit tokens.

If a future branch enables Wit or Meta Voice SDK:

1. Create a project-owned Wit/Meta app in the service dashboard.
2. Add credentials locally through the provider's Unity tooling.
3. Do not commit non-empty `serverToken` values.
4. Document the new runtime owner and reviewer setup steps in this file.

Any token that has ever been pushed to a public repository should be considered exposed and rotated in the provider dashboard.

## GitHub Secrets

The current `.github/workflows/static-governance.yml` workflow does not require secrets and does not require a Unity license.

If Unity build or Play Mode automation is added later, configure secrets in GitHub repository settings, not in files:

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

If automated online transcription tests are added later, use a GitHub secret such as `OPENAI_API_KEY` and gate that job so it never runs on untrusted pull requests.

## Reviewer Checklist

- `VoiceCommandRecognizer.apiKey` is blank or a placeholder before commit.
- `ProjectSettings/wit.config` has empty `serverToken` values.
- No `.env`, local credential file, Unity license file, or debug WAV is tracked.
- Optional service credentials are documented here before use.
