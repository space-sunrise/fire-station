
using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Other.ScpRememberPhrase;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpRememberPhraseComponent : Component
{
    [DataField]
    public EntProtoId<ActionComponent> ActionProto = "RememberPhraseAction";

    [DataField]
    public int MaxRememberedMessages = 20;

    /// <summary>
    /// Запомненые объектом слова. Ключ - сказанная фраза, значение - пара, в которой ключ имя сказавшего и значение прототип его ттса
    /// </summary>
    [ViewVariables]
    public Dictionary<string, KeyValuePair<string, string?>> RememberedMessages = new();
}
