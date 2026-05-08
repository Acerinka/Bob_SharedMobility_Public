using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bob.SharedMobility
{
    [DefaultExecutionOrder(-1000)]
    public sealed class RuntimeDiagnosticsHub : MonoBehaviour
    {
        public enum RuntimeProfile
        {
            Production,
            Development
        }

        public enum BackdoorKind
        {
            BobTargetShortcuts = 0,
            BobSkinHotkeys = 1,
            LaneAssistScenarioShortcut = 2,
            VoiceInputShortcut = 3,
            VoiceCommandInjection = 4,
            VoiceDebugWav = 5,
            GamepadInputLogging = 6
        }

        [System.Serializable]
        public class BackdoorControl
        {
            public BackdoorKind kind;
            public string label;
            public string owner;
            public bool enabledInDevelopment = true;
            public KeyCode keyboardShortcut = KeyCode.None;
            public MyGamepadButton gamepadShortcut = MyGamepadButton.None;
            [TextArea(2, 5)] public string inspectorNotes;
        }

        public static RuntimeDiagnosticsHub Instance { get; private set; }

        [Header("Profile")]
        [SerializeField] private RuntimeProfile runtimeProfile = RuntimeProfile.Development;
        [SerializeField] private bool applyAutomatically = true;
        [SerializeField] private bool logAppliedProfile = false;

        [Header("Backdoor Registry")]
        [SerializeField] private List<BackdoorControl> backdoorControls = new List<BackdoorControl>
        {
            new BackdoorControl
            {
                kind = BackdoorKind.BobTargetShortcuts,
                label = "Bob target shortcuts",
                owner = "BobInteractionDirector",
                enabledInDevelopment = true,
                inspectorNotes = "Development-only target jumps. Individual target keys live on BobInteractionDirector.registeredTargets."
            },
            new BackdoorControl
            {
                kind = BackdoorKind.BobSkinHotkeys,
                label = "Bob skin hotkeys",
                owner = "BobController",
                enabledInDevelopment = true,
                keyboardShortcut = KeyCode.Alpha1,
                inspectorNotes = "Alpha1/Alpha2/Alpha3 switch Bob skins for visual QA."
            },
            new BackdoorControl
            {
                kind = BackdoorKind.LaneAssistScenarioShortcut,
                label = "Lane assist scenario trigger",
                owner = "LaneAssistScenarioController",
                enabledInDevelopment = true,
                keyboardShortcut = KeyCode.S,
                inspectorNotes = "Manual trigger for the lane assist scenario while the map is full screen."
            },
            new BackdoorControl
            {
                kind = BackdoorKind.VoiceDebugWav,
                label = "Voice debug WAV capture",
                owner = "VoiceCommandRecognizer",
                enabledInDevelopment = false,
                inspectorNotes = "Writes captured voice audio to Application.persistentDataPath for debugging."
            },
            new BackdoorControl
            {
                kind = BackdoorKind.VoiceInputShortcut,
                label = "Voice input shortcut",
                owner = "VoiceCommandRecognizer",
                enabledInDevelopment = true,
                keyboardShortcut = KeyCode.F,
                gamepadShortcut = MyGamepadButton.ButtonSouth,
                inspectorNotes = "Hold this shortcut to record real microphone input for voice command transcription."
            },
            new BackdoorControl
            {
                kind = BackdoorKind.VoiceCommandInjection,
                label = "Voice command injection",
                owner = "RuntimeDiagnosticsHub",
                enabledInDevelopment = true,
                keyboardShortcut = KeyCode.None,
                inspectorNotes = "Injects the text in Voice Command Diagnostics without microphone or API access."
            },
            new BackdoorControl
            {
                kind = BackdoorKind.GamepadInputLogging,
                label = "Gamepad input logging",
                owner = "GamepadInputDebugger",
                enabledInDevelopment = false,
                inspectorNotes = "Logs every gamepad button press in editor/development builds."
            }
        };

        [Header("Scene References")]
        [SerializeField] private BobInteractionDirector bobInteractionDirector;
        [SerializeField] private BobController bobController;
        [SerializeField] private LaneAssistScenarioController laneAssistScenario;
        [SerializeField] private VoiceCommandRecognizer voiceCommandRecognizer;
        [SerializeField] private GamepadInputDebugger gamepadInputDebugger;

        [Header("Voice Command Diagnostics")]
        [SerializeField] private string injectedVoiceCommandText = "open map";

        private bool _hasCapturedLaneAssistDefaults;
        private KeyCode _laneAssistDefaultKey = KeyCode.None;
        private MyGamepadButton _laneAssistDefaultGamepadButton = MyGamepadButton.None;
        private bool _hasCapturedVoiceInputDefaults;
        private Key _voiceDefaultKeyboardKey = Key.None;
        private VoiceCommandButton _voiceDefaultGamepadButton = VoiceCommandButton.None;

        public bool IsDevelopmentProfile => runtimeProfile == RuntimeProfile.Development;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                ProjectLog.Warning("Multiple RuntimeDiagnosticsHub instances detected; the latest instance will be used.", this);
            }

            Instance = this;
            ResolveReferences();
            CaptureLaneAssistDefaults();
            CaptureVoiceInputDefaults();

            if (applyAutomatically)
            {
                ApplyCurrentProfile();
            }
        }

        private void Start()
        {
            if (applyAutomatically)
            {
                ApplyCurrentProfile();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) return;

            ResolveReferences();
            CaptureLaneAssistDefaults();
            CaptureVoiceInputDefaults();
            ApplyCurrentProfile();
        }
#endif

        [ContextMenu("Apply Current Profile")]
        public void ApplyCurrentProfile()
        {
            bool developmentMode = IsDevelopmentProfile;

            if (bobInteractionDirector)
            {
                bobInteractionDirector.enableDebugShortcuts = IsBackdoorEnabled(BackdoorKind.BobTargetShortcuts, developmentMode);
            }

            if (bobController)
            {
                bobController.enableDebugSkinHotkeys = IsBackdoorEnabled(BackdoorKind.BobSkinHotkeys, developmentMode);
            }

            if (laneAssistScenario)
            {
                BackdoorControl laneAssistBackdoor = FindBackdoor(BackdoorKind.LaneAssistScenarioShortcut);
                bool enableScenarioShortcut = IsBackdoorEnabled(laneAssistBackdoor, developmentMode);

                laneAssistScenario.triggerKey = enableScenarioShortcut
                    ? ResolveKeyboardShortcut(laneAssistBackdoor, _laneAssistDefaultKey)
                    : KeyCode.None;

                laneAssistScenario.triggerGamepadBtn = enableScenarioShortcut
                    ? ResolveGamepadShortcut(laneAssistBackdoor, _laneAssistDefaultGamepadButton)
                    : MyGamepadButton.None;
            }

            if (voiceCommandRecognizer)
            {
                BackdoorControl voiceInputBackdoor = FindBackdoor(BackdoorKind.VoiceInputShortcut);
                bool enableVoiceInputShortcut = IsBackdoorEnabled(voiceInputBackdoor, developmentMode);

                voiceCommandRecognizer.ConfigureInputBackdoor(
                    enableVoiceInputShortcut,
                    enableVoiceInputShortcut
                        ? ResolveVoiceKeyboardShortcut(voiceInputBackdoor, _voiceDefaultKeyboardKey)
                        : Key.None,
                    enableVoiceInputShortcut
                        ? ResolveVoiceGamepadShortcut(voiceInputBackdoor, _voiceDefaultGamepadButton)
                        : VoiceCommandButton.None);

                voiceCommandRecognizer.saveDebugWav = IsBackdoorEnabled(BackdoorKind.VoiceDebugWav, developmentMode);
            }

            if (gamepadInputDebugger)
            {
                gamepadInputDebugger.enabled = IsBackdoorEnabled(BackdoorKind.GamepadInputLogging, developmentMode);
            }

            if (logAppliedProfile)
            {
                ProjectLog.Info($"Applied runtime diagnostics profile: {runtimeProfile}", this);
            }
        }

        [ContextMenu("Resolve Scene References")]
        public void ResolveReferences()
        {
            bobInteractionDirector = ResolveSceneReference(bobInteractionDirector);
            bobController = ResolveSceneReference(bobController);
            laneAssistScenario = ResolveSceneReference(laneAssistScenario);
            voiceCommandRecognizer = ResolveSceneReference(voiceCommandRecognizer);
            gamepadInputDebugger = ResolveSceneReference(gamepadInputDebugger);
        }

        [ContextMenu("Backdoor/Inject Voice Command")]
        public void InjectVoiceCommandBackdoor()
        {
            if (!IsDevelopmentProfile || !IsBackdoorEnabled(BackdoorKind.VoiceCommandInjection, true))
            {
                ProjectLog.Warning("Voice command injection is disabled by RuntimeDiagnosticsHub.", this);
                return;
            }

            ResolveReferences();
            if (!voiceCommandRecognizer)
            {
                ProjectLog.Warning("Cannot inject voice command because VoiceCommandRecognizer is missing.", this);
                return;
            }

            voiceCommandRecognizer.InjectRecognizedCommand(injectedVoiceCommandText);
        }

        [ContextMenu("Backdoor/Log Registry")]
        public void LogBackdoorRegistry()
        {
            if (backdoorControls == null)
            {
                ProjectLog.Info("Backdoor registry is empty.", this);
                return;
            }

            foreach (BackdoorControl backdoor in backdoorControls)
            {
                if (backdoor == null) continue;

                string enabledText = backdoor.enabledInDevelopment ? "enabled in Development" : "disabled by default";
                ProjectLog.Info(
                    $"{backdoor.kind}: {backdoor.label} ({enabledText}); owner={backdoor.owner}; key={backdoor.keyboardShortcut}; gamepad={backdoor.gamepadShortcut}",
                    this);
            }
        }

        private void Update()
        {
            if (!IsDevelopmentProfile) return;

            BackdoorControl injectionBackdoor = FindBackdoor(BackdoorKind.VoiceCommandInjection);
            if (!IsBackdoorEnabled(injectionBackdoor, true)) return;

            if (WasBackdoorPressed(injectionBackdoor))
            {
                InjectVoiceCommandBackdoor();
            }
        }

        private void CaptureLaneAssistDefaults()
        {
            if (_hasCapturedLaneAssistDefaults || laneAssistScenario == null) return;

            _laneAssistDefaultKey = laneAssistScenario.triggerKey;
            _laneAssistDefaultGamepadButton = laneAssistScenario.triggerGamepadBtn;
            _hasCapturedLaneAssistDefaults = true;
        }

        private void CaptureVoiceInputDefaults()
        {
            if (_hasCapturedVoiceInputDefaults || voiceCommandRecognizer == null) return;

            _voiceDefaultKeyboardKey = voiceCommandRecognizer.keyboardKey;
            _voiceDefaultGamepadButton = voiceCommandRecognizer.gamepadButton;
            _hasCapturedVoiceInputDefaults = true;
        }

        private bool IsBackdoorEnabled(BackdoorKind kind, bool developmentMode)
        {
            return IsBackdoorEnabled(FindBackdoor(kind), developmentMode);
        }

        private static bool IsBackdoorEnabled(BackdoorControl backdoor, bool developmentMode)
        {
            return developmentMode && backdoor != null && backdoor.enabledInDevelopment;
        }

        private BackdoorControl FindBackdoor(BackdoorKind kind)
        {
            if (backdoorControls == null) return null;

            foreach (BackdoorControl backdoor in backdoorControls)
            {
                if (backdoor != null && backdoor.kind == kind)
                {
                    return backdoor;
                }
            }

            return null;
        }

        private static KeyCode ResolveKeyboardShortcut(BackdoorControl backdoor, KeyCode fallback)
        {
            if (backdoor == null || backdoor.keyboardShortcut == KeyCode.None)
            {
                return fallback;
            }

            return backdoor.keyboardShortcut;
        }

        private static MyGamepadButton ResolveGamepadShortcut(BackdoorControl backdoor, MyGamepadButton fallback)
        {
            if (backdoor == null || backdoor.gamepadShortcut == MyGamepadButton.None)
            {
                return fallback;
            }

            return backdoor.gamepadShortcut;
        }

        private static Key ResolveVoiceKeyboardShortcut(BackdoorControl backdoor, Key fallback)
        {
            if (backdoor == null || backdoor.keyboardShortcut == KeyCode.None)
            {
                return fallback;
            }

            if (TryConvertKeyCode(backdoor.keyboardShortcut, out Key key))
            {
                return key;
            }

            ProjectLog.Warning($"Unsupported voice keyboard shortcut: {backdoor.keyboardShortcut}");
            return fallback;
        }

        private static VoiceCommandButton ResolveVoiceGamepadShortcut(BackdoorControl backdoor, VoiceCommandButton fallback)
        {
            if (backdoor == null || backdoor.gamepadShortcut == MyGamepadButton.None)
            {
                return fallback;
            }

            switch (backdoor.gamepadShortcut)
            {
                case MyGamepadButton.ButtonSouth: return VoiceCommandButton.ButtonSouth;
                case MyGamepadButton.ButtonEast: return VoiceCommandButton.ButtonEast;
                case MyGamepadButton.ButtonWest: return VoiceCommandButton.ButtonWest;
                case MyGamepadButton.ButtonNorth: return VoiceCommandButton.ButtonNorth;
                case MyGamepadButton.LeftShoulder: return VoiceCommandButton.LeftShoulder;
                case MyGamepadButton.RightShoulder: return VoiceCommandButton.RightShoulder;
                default:
                    ProjectLog.Warning($"Unsupported voice gamepad shortcut: {backdoor.gamepadShortcut}");
                    return fallback;
            }
        }

        private static bool WasBackdoorPressed(BackdoorControl backdoor)
        {
            if (backdoor == null) return false;

            return ProjectInput.WasKeyPressed(backdoor.keyboardShortcut)
                || GamepadButtonReader.WasPressedThisFrame(backdoor.gamepadShortcut);
        }

        private static bool TryConvertKeyCode(KeyCode keyCode, out Key key)
        {
            switch (keyCode)
            {
                case KeyCode.A: key = Key.A; return true;
                case KeyCode.B: key = Key.B; return true;
                case KeyCode.C: key = Key.C; return true;
                case KeyCode.D: key = Key.D; return true;
                case KeyCode.E: key = Key.E; return true;
                case KeyCode.F: key = Key.F; return true;
                case KeyCode.G: key = Key.G; return true;
                case KeyCode.H: key = Key.H; return true;
                case KeyCode.I: key = Key.I; return true;
                case KeyCode.J: key = Key.J; return true;
                case KeyCode.K: key = Key.K; return true;
                case KeyCode.L: key = Key.L; return true;
                case KeyCode.M: key = Key.M; return true;
                case KeyCode.N: key = Key.N; return true;
                case KeyCode.O: key = Key.O; return true;
                case KeyCode.P: key = Key.P; return true;
                case KeyCode.Q: key = Key.Q; return true;
                case KeyCode.R: key = Key.R; return true;
                case KeyCode.S: key = Key.S; return true;
                case KeyCode.T: key = Key.T; return true;
                case KeyCode.U: key = Key.U; return true;
                case KeyCode.V: key = Key.V; return true;
                case KeyCode.W: key = Key.W; return true;
                case KeyCode.X: key = Key.X; return true;
                case KeyCode.Y: key = Key.Y; return true;
                case KeyCode.Z: key = Key.Z; return true;
                case KeyCode.Alpha0: key = Key.Digit0; return true;
                case KeyCode.Alpha1: key = Key.Digit1; return true;
                case KeyCode.Alpha2: key = Key.Digit2; return true;
                case KeyCode.Alpha3: key = Key.Digit3; return true;
                case KeyCode.Alpha4: key = Key.Digit4; return true;
                case KeyCode.Alpha5: key = Key.Digit5; return true;
                case KeyCode.Alpha6: key = Key.Digit6; return true;
                case KeyCode.Alpha7: key = Key.Digit7; return true;
                case KeyCode.Alpha8: key = Key.Digit8; return true;
                case KeyCode.Alpha9: key = Key.Digit9; return true;
                case KeyCode.Space: key = Key.Space; return true;
                case KeyCode.Return: key = Key.Enter; return true;
                case KeyCode.Escape: key = Key.Escape; return true;
                case KeyCode.Tab: key = Key.Tab; return true;
                case KeyCode.Backspace: key = Key.Backspace; return true;
                default:
                    key = Key.None;
                    return false;
            }
        }

        private static T ResolveSceneReference<T>(T current) where T : UnityEngine.Object
        {
            return current != null ? current : FindObjectOfType<T>(true);
        }
    }
}
