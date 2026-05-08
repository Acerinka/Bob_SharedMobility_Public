namespace Bob.SharedMobility
{
    public enum AppScreenId
    {
        None = 0,
        Home = 10,
        Map = 20,
        Apps = 30,
        Climate = 40,
        Volume = 50,
        Settings = 60,
        Profile = 70,
        Support = 80,
        Seat = 90,
        Mirror = 100,
        LaneAssist = 110
    }

    public enum AppNavigationLayer
    {
        Shell = 0,
        DockPanel = 10,
        SubPanel = 20,
        Modal = 30,
        Overlay = 40
    }

    public enum AppNavigationCommand
    {
        OpenScreen = 0,
        OpenHome = 10,
        OpenModal = 20,
        CloseCurrentScreen = 30,
        CloseTopModal = 40,
        CloseAllModals = 50
    }

    [System.Serializable]
    public readonly struct AppNavigationState
    {
        public readonly AppScreenId screenId;
        public readonly AppNavigationLayer layer;
        public readonly AppScreenId modalScreenId;
        public readonly bool blocksWorldInput;

        public AppNavigationState(
            AppScreenId screenId,
            AppNavigationLayer layer,
            AppScreenId modalScreenId,
            bool blocksWorldInput)
        {
            this.screenId = screenId;
            this.layer = layer;
            this.modalScreenId = modalScreenId;
            this.blocksWorldInput = blocksWorldInput;
        }
    }
}
