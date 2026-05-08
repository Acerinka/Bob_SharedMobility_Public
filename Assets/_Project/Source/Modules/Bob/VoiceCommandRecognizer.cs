using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem;

namespace Bob.SharedMobility
{
    public enum VoiceCommandButton
    {
        None,
        ButtonSouth,
        ButtonEast,
        ButtonWest,
        ButtonNorth,
        LeftShoulder,
        RightShoulder
    }

    public class VoiceCommandRecognizer : MonoBehaviour
    {
        private const string TranscriptionUrl = "https://api.openai.com/v1/audio/transcriptions";
        private const string PlaceholderApiKey = "sk-xxxxxxxxxxxxxxxxxxxxxxxx";

        [Header("OpenAI")]
        public string apiKey = PlaceholderApiKey;
        [TextArea] public string prompt = "Bob, Weather, Map, Music, Home, Cancel, Setting, open map, weather, music, settings";

        [Header("Diagnostics")]
        public bool saveDebugWav = false;

        [Header("References")]
        public BobController bob;
        public BobInteractionDirector globalDirector;

        [Header("Input")]
        public bool inputEnabled = true;
        public Key keyboardKey = Key.F;
        public VoiceCommandButton gamepadButton = VoiceCommandButton.ButtonSouth;
        public float holdDuration = 2.0f;

        [Header("Feedback")]
        [Range(0, 1)] public float warmupIntensity = 0.4f;

        private string _defaultDevice;
        private AudioClip _recordingClip;
        private bool _isRecording;
        private bool _isPressing;
        private float _pressTimer;

        [System.Serializable]
        private class OpenAIResponse
        {
            public string text;
        }

        private void Start()
        {
            if (Microphone.devices.Length == 0)
            {
                ProjectLog.Error("No recording device was found.", this);
                return;
            }

            _defaultDevice = Microphone.devices[0];
            ProjectLog.Info($"Using microphone device: {_defaultDevice}", this);
        }

        private void Update()
        {
            if (!inputEnabled)
            {
                CancelPendingInput();
                return;
            }

            bool isInputActive = ProjectInput.IsVoiceCommandPressed(keyboardKey, gamepadButton);

            if (isInputActive)
            {
                HandleInputHeld();
                return;
            }

            HandleInputReleased();
        }

        private void HandleInputHeld()
        {
            if (!_isPressing)
            {
                _isPressing = true;
                _pressTimer = 0f;
                SetBobVolume(0.1f);
            }

            if (_isRecording) return;

            _pressTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(_pressTimer / Mathf.Max(0.01f, holdDuration));
            SetBobVolume(Mathf.Lerp(0.1f, warmupIntensity, progress));

            if (_pressTimer >= holdDuration)
            {
                StartRecording();
            }
        }

        private void HandleInputReleased()
        {
            if (!_isPressing) return;

            _isPressing = false;

            if (_isRecording)
            {
                StopAndSend();
            }
            else
            {
                SetBobVolume(0f);
            }

            _pressTimer = 0f;
        }

        private void StartRecording()
        {
            if (string.IsNullOrEmpty(_defaultDevice)) return;

            _isRecording = true;
            SetBobVolume(Mathf.Min(warmupIntensity + 0.2f, 1.0f));

            if (bob)
            {
                bob.TriggerAction(BobController.BobActionType.PulseLight);
            }

            _recordingClip = Microphone.Start(_defaultDevice, false, 10, 44100);
            ProjectLog.Info("Voice recording started.", this);
        }

        private void StopAndSend()
        {
            _isRecording = false;

            int position = Microphone.GetPosition(_defaultDevice);
            Microphone.End(_defaultDevice);

            if (position < 1000)
            {
                SetBobVolume(0f);
                return;
            }

            SetBobVolume(warmupIntensity);
            StartCoroutine(SendToOpenAI(_recordingClip, position));
        }

        private IEnumerator SendToOpenAI(AudioClip clip, int endPosition)
        {
            byte[] wavData = WavUtilityNative.ConvertToWavAndBoost(clip, endPosition, saveDebugWav);
            if (wavData == null)
            {
                ProjectLog.Warning("Voice recording was too quiet or invalid.", this);
                SetBobVolume(0f);
                yield break;
            }

            if (!HasConfiguredApiKey())
            {
                ProjectLog.Warning("Voice transcription is skipped because no OpenAI API key is configured.", this);
                SetBobVolume(0f);
                yield break;
            }

            List<IMultipartFormSection> formData = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("file", wavData, "audio.wav", "audio/wav"),
                new MultipartFormDataSection("model", "whisper-1"),
                new MultipartFormDataSection("prompt", prompt)
            };

            using (UnityWebRequest request = UnityWebRequest.Post(TranscriptionUrl, formData))
            {
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    HandleTranscriptionResponse(request.downloadHandler.text);
                }
                else
                {
                    ProjectLog.Error($"Voice transcription request failed: {request.error}", this);
                }
            }

            SetBobVolume(0f);
        }

        public void ConfigureInputBackdoor(bool enabled, Key keyboardShortcut, VoiceCommandButton gamepadShortcut)
        {
            inputEnabled = enabled;
            keyboardKey = keyboardShortcut;
            gamepadButton = gamepadShortcut;

            if (!inputEnabled)
            {
                CancelPendingInput();
            }
        }

        public void InjectRecognizedCommand(string text)
        {
            string cleanText = NormalizeRecognizedText(text);
            if (cleanText.Length < 2)
            {
                ProjectLog.Warning("Ignored empty injected voice command.", this);
                return;
            }

            ProjectLog.Info($"Injected voice command: {cleanText}", this);

            if (globalDirector)
            {
                globalDirector.ProcessVoiceCommand(cleanText);
            }

            if (bob)
            {
                bob.TriggerAction(BobController.BobActionType.JumpHigh);
            }
        }

        private void CancelPendingInput()
        {
            if (_isRecording && !string.IsNullOrEmpty(_defaultDevice))
            {
                Microphone.End(_defaultDevice);
            }

            _isRecording = false;
            _isPressing = false;
            _pressTimer = 0f;
            SetBobVolume(0f);
        }

        private void HandleTranscriptionResponse(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || !json.Contains("text")) return;

            OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(json);
            string cleanText = NormalizeRecognizedText(response.text);

            if (cleanText.Length < 2)
            {
                ProjectLog.Warning($"Ignored short voice transcription: {response.text}", this);
                return;
            }

            ProjectLog.Info($"Voice transcription recognized: {cleanText}", this);

            if (globalDirector)
            {
                globalDirector.ProcessVoiceCommand(cleanText);
            }

            if (bob)
            {
                bob.TriggerAction(BobController.BobActionType.JumpHigh);
            }
        }

        private static string NormalizeRecognizedText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            return text.ToLowerInvariant()
                .Trim()
                .Replace(".", "")
                .Replace(",", "")
                .Replace("!", "")
                .Replace("?", "");
        }

        private bool HasConfiguredApiKey()
        {
            return !string.IsNullOrWhiteSpace(apiKey)
                && apiKey != PlaceholderApiKey;
        }

        private void SetBobVolume(float value)
        {
            if (bob)
            {
                bob.masterVolume = Mathf.Clamp01(value);
            }
        }
    }

    public static class WavUtilityNative
    {
        public static byte[] ConvertToWavAndBoost(AudioClip clip, int sampleCount, bool saveDebug)
        {
            if (clip == null || sampleCount <= 0) return null;

            float[] samples = new float[sampleCount * clip.channels];
            clip.GetData(samples, 0);

            float maxVolume = 0f;
            foreach (float sample in samples)
            {
                maxVolume = Mathf.Max(maxVolume, Mathf.Abs(sample));
            }

            if (maxVolume < 0.02f) return null;

            float multiplier = 0.95f / maxVolume;
            const float gateThreshold = 0.05f;

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
                writer.Write(36 + samples.Length * 2);
                writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)clip.channels);
                writer.Write(clip.frequency);
                writer.Write(clip.frequency * clip.channels * 2);
                writer.Write((ushort)(clip.channels * 2));
                writer.Write((ushort)16);
                writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
                writer.Write(samples.Length * 2);

                foreach (float sample in samples)
                {
                    float amplified = sample * multiplier;
                    if (Mathf.Abs(amplified) < gateThreshold)
                    {
                        amplified = 0f;
                    }

                    short intData = (short)(Mathf.Clamp(amplified, -1f, 1f) * 32767);
                    writer.Write(intData);
                }

                byte[] wavBytes = stream.ToArray();
                if (saveDebug)
                {
                    string path = Path.Combine(Application.persistentDataPath, "voice-command-debug.wav");
                    File.WriteAllBytes(path, wavBytes);
                }

                return wavBytes;
            }
        }
    }
}
