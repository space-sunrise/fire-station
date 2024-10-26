namespace Content.Server._Scp.Backrooms.ChokeOnRead;

[RegisterComponent]
public sealed partial class ChokeOnReadComponent : Component
{
    /// <summary>
    /// Список проклятых существ
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> Cursed = new ();
}
