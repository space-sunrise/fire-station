using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Robust.Shared.Audio;

namespace Content.Server._Scp.Research.ReagentSynthesizer;

[RegisterComponent]
public sealed partial class ReagentSynthesizerComponent : Component
{
    [DataField(required: true)]
    public HashSet<ReagentId> Reagents = new();

    [DataField]
    public TimeSpan WorkTime = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Эффекты, которые будут происходить при синтезе реагента-ключа
    ///
    /// TODO: Разобраться как записать реагент айди в прототип
    /// </summary>
    /// <exception cref="ArgumentException">Ошибка, возникающая, когда в словаре есть эффект для реагента, который не находится в Reagents</exception>
    [DataField]
    public Dictionary<ReagentId, List<EntityEffect>> Effects
    {
        get => _effects;
        set
        {
            // Проверяем, что реагент есть в реагентах.
            // Не может быть эффекта для реагента, который не синтезируется
            foreach (var reagentId in value.Keys)
            {
                if (!Reagents.Contains(reagentId))
                {
                    throw new ArgumentException($"ReagentId '{reagentId}' отсутствует в списке Reagents.");
                }
            }
            _effects = value;
        }
    }
    private Dictionary<ReagentId, List<EntityEffect>> _effects = new();

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
