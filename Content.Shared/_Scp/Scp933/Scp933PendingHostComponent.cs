namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Ожидание «отпадания» ленты после самонаклеивания; по истечении времени выдаётся Scp933Master (только сервер).
/// </summary>
[RegisterComponent]
public sealed partial class Scp933PendingHostComponent : Component
{
    [DataField]
    public float RemainingSeconds = 8f;
}
