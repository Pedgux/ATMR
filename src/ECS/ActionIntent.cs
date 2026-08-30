namespace ATMR.Systems;

public enum ActionKind
{
    Move,
    Wait,
    Dig,
    Pickup,
    Drop,
    Teleport,
}

public readonly record struct ActionIntent
{
    public int PlayerId { get; }
    public ActionKind Kind { get; }
    public int Dx { get; }
    public int Dy { get; }
    public int Amount { get; }
    public int ItemIndex { get; }

    public ActionIntent(
        int playerId,
        ActionKind kind,
        int dx = 0,
        int dy = 0,
        int amount = -1,
        int itemIndex = -1
    )
    {
        PlayerId = playerId;
        Kind = kind;
        Dx = dx;
        Dy = dy;
        Amount = amount;
        ItemIndex = itemIndex;
    }
}