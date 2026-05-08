using UnityEngine;
using UnityEngine.UI;

namespace Bob.SharedMobility
{
    [RequireComponent(typeof(Button))]
    public sealed class AppNavigationButton : MonoBehaviour
    {
        [Header("Navigation Command")]
        [SerializeField] private AppNavigationCommand command = AppNavigationCommand.OpenScreen;
        [SerializeField] private AppScreenId targetScreen = AppScreenId.None;
        [SerializeField] private CanvasGroup targetSubPanel;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(ExecuteNavigationCommand);
        }

        private void OnDestroy()
        {
            if (_button)
            {
                _button.onClick.RemoveListener(ExecuteNavigationCommand);
            }
        }

        public void ExecuteNavigationCommand()
        {
            AppNavigationService navigationService = AppNavigationService.Instance;
            if (!navigationService)
            {
                ProjectLog.Warning("Navigation button cannot execute because AppNavigationService is missing.", this);
                return;
            }

            switch (command)
            {
                case AppNavigationCommand.OpenScreen:
                    navigationService.OpenScreen(targetScreen, targetSubPanel);
                    break;
                case AppNavigationCommand.OpenHome:
                    navigationService.OpenHome();
                    break;
                case AppNavigationCommand.OpenModal:
                    navigationService.OpenModal(targetScreen);
                    break;
                case AppNavigationCommand.CloseCurrentScreen:
                    navigationService.CloseCurrentScreen();
                    break;
                case AppNavigationCommand.CloseTopModal:
                    navigationService.CloseTopModal();
                    break;
                case AppNavigationCommand.CloseAllModals:
                    navigationService.CloseAllModals();
                    break;
            }
        }
    }
}
