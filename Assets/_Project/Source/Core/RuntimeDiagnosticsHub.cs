using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bob.SharedMobility
{
    [DefaultExecutionOrder(-1000)]
    public sealed partial class RuntimeDiagnosticsHub : MonoBehaviour
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
        [SerializeField] private AppNavigationService navigationService;
        [SerializeField] private MapViewController mapController;
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

    }
}
