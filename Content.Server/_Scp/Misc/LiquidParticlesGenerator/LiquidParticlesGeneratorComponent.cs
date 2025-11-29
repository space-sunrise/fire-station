namespace Content.Server._Scp.Misc.LiquidParticlesGenerator;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class LiquidParticlesGeneratorComponent : Component
{
    [DataField]
    public bool Enabled;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1f);

    [DataField]
    public TimeSpan CooldownVariation = TimeSpan.FromSeconds(2.5f);

    [ViewVariables, AutoPausedField]
    public TimeSpan? NextSpawn;

    [DataField]
    public bool EnableOnMapInit = true;

    [DataField]
    public float Angle = 360f;

    [DataField]
    public float Radians = (float) Math.PI * 2f;
}
