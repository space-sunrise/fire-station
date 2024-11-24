using Content.Shared.EntityEffects;
using Robust.Shared.Audio;

namespace Content.Server._Scp.Research.ReagentSynthesizer;

[RegisterComponent]
public sealed partial class ReagentSynthesizerComponent : Component
{
    [DataField(required: true)]
    public HashSet<string> Reagents = new();

    [DataField]
    public TimeSpan WorkTime = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Эффекты, которые будут происходить при синтезе реагента-ключа
    /// </summary>
    [DataField]
    public Dictionary<string, List<EntityEffect>> Effects = new();

    #region Sounds

    [DataField]
    public SoundSpecifier ActiveSound = new SoundPathSpecifier("/Audio/Machines/blender.ogg");

    #endregion

    public EntityUid? AudioStream;
}

[RegisterComponent]
public sealed partial class ActiveReagentSynthesizerComponent : Component
{
    [ViewVariables]
    public TimeSpan EndTime;

    [ViewVariables]
    public TimeSpan TimeWithoutEnergy;
}
