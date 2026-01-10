namespace Content.Shared._Scp.GameRule.Sl;

//Component for marking SCP in SCP:SL gamemode
[RegisterComponent]
public sealed partial class ScpSlScpMarkerComponent : Component
{
    public bool Contained { get; set; }
}

[RegisterComponent]
public sealed partial class ScpSlHumanoidMarkerComponent : Component
{
    [DataField(required: true)]
    public ScpSlHumanoidType HumanoidType { get; set; }
}

[Serializable]
public enum ScpSlHumanoidType : byte
{
    Mog = 0,
    Chaos = 1,
    Scientist = 2,
    ClassD = 4,
    Security = 8
}
