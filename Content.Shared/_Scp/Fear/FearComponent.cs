using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Fear;

// TODO: Сделать разные уровни страха при разном приближении
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FearComponent : Component
{
    [AutoNetworkedField, ViewVariables]
    public FearState State = FearState.None;

    [ViewVariables]
    public TimeSpan TimeToDecreaseFearLevel = TimeSpan.FromMinutes(3f);
}

public enum FearState
{
    None,
    Anxiety,
    Fear,
    Terror,
}
