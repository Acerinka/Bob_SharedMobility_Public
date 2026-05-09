namespace Bob.SharedMobility
{
    public enum BobCommandPhase
    {
        Idle,
        RemoteDelay,
        FlightSequence,
        WaitingForFeatureReturn,
        BobLandingRecovery,
        Queued
    }

    public enum BobCommandResult
    {
        Accepted,
        Interrupted,
        Queued,
        ToggledClosed,
        IgnoredInvalidTarget,
        IgnoredAlreadyOpen,
        IgnoredDuplicate,
        IgnoredDuplicateQueued
    }

    public static class BobCommandResultExtensions
    {
        public static bool WasAccepted(this BobCommandResult result)
        {
            return result == BobCommandResult.Accepted
                || result == BobCommandResult.Interrupted
                || result == BobCommandResult.Queued
                || result == BobCommandResult.ToggledClosed;
        }

        public static bool WasExecutedImmediately(this BobCommandResult result)
        {
            return result == BobCommandResult.Accepted
                || result == BobCommandResult.Interrupted
                || result == BobCommandResult.ToggledClosed;
        }
    }
}
