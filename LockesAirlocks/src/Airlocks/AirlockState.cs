namespace IngameScript
{
    public enum AirlockState
    {
        Unknown,
        Neutral,
        InnerOpen,
        OuterOpen,
        Pressurizing,
        Depressurizing,
        AwaitingInnerLock,
        AwaitingOuterLock,
        AwaitingTotalLock,
    }
}
