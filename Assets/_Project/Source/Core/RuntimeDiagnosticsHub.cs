using System.Collections.Generic;
using UnityEngine;

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
            BobTargetShortcuts,
            BobSkinHotkeys,
            LaneAssistScenarioShortcut,
            VoiceDebugWav,
            GamepadInputLogging
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

        private bool _hasCapturedLaneAssistDefaults;
        private KeyCode _laneAssistDefaultKey = KeyCode.None;
        private MyGamepadButton _laneAssistDefaultGamepadButton = MyGamepadButton.None;

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

        private void CaptureLaneAssistDefaults()
        {
            if (_hasCapturedLaneAssistDefaults || laneAssistScenario == null) return;

            _laneAssistDefaultKey = laneAssistScenario.triggerKey;
            _laneAssistDefaultGamepadButton = laneAssistScenario.triggerGamepadBtn;
            _hasCapturedLaneAssistDefaults = true;
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

        private static T ResolveSceneReference<T>(T current) where T : UnityEngine.Object
        {
            return current != null ? current : FindObjectOfType<T>(true);
        }
    }
}
