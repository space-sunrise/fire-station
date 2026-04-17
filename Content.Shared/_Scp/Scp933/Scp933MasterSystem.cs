using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// SCP-933: носитель ленты и жертвы после ритуала. Жертвы — живые игроки с дебаффами, без ИИ и без «рабства».
/// </summary>
public abstract class SharedScp933MasterSystem : EntitySystem
{
    [Dependency] protected readonly SharedHumanoidAppearanceSystem _humanoid = default!;

    /// <summary>
    /// Спрятать лицо гуманоида (SCP-933): носитель и жертвы после ритуала.
    /// </summary>
    public void EraseFaceFor933(EntityUid uid, Scp933VisualEffectsComponent? visualComp = null)
    {
        if (!TryComp<Scp933TargetComponent>(uid, out var targetComp) || !targetComp.CanBeFaceTorn)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidComp))
            return;

        // Get visual effects from component or use defaults
        if (visualComp == null)
            TryComp<Scp933VisualEffectsComponent>(uid, out visualComp);

        var humanoidEnt = new Entity<HumanoidAppearanceComponent?>(uid, humanoidComp);
        var layers = GetFaceErasureLayers(visualComp);
        _humanoid.SetLayersVisibility(humanoidEnt, layers, false);
    }

    private static List<HumanoidVisualLayers> GetFaceErasureLayers(Scp933VisualEffectsComponent? visualComp)
    {
        var layers = new List<HumanoidVisualLayers>();

        if (visualComp?.HideEyes == true)
            layers.Add(HumanoidVisualLayers.Eyes);

        if (visualComp?.HideSnout == true)
            layers.Add(HumanoidVisualLayers.Snout);

        if (visualComp?.AdditionalHiddenLayers != null)
            layers.AddRange(visualComp.AdditionalHiddenLayers);

        if (layers.Count == 0)
        {
            layers.AddRange(new[]
            {
                HumanoidVisualLayers.Eyes,
                HumanoidVisualLayers.Snout,
                HumanoidVisualLayers.SnoutCover,
            });
        }

        return layers;
    }

    /// <summary>
    /// Пытается вылечить жертву после срыва ленты. Переопределяется на сервере.
    /// </summary>
    protected virtual void TryHealVictim(EntityUid tapeBearer, EntityUid victim)
    {
        // Серверная реализация переопределит это
    }
}

[Serializable, NetSerializable]
public sealed partial class Scp933PeelTapeDoAfterEvent : SimpleDoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return new Scp933PeelTapeDoAfterEvent();
    }
}

[Serializable, NetSerializable]
public sealed partial class Scp933ApplyTapeDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return new Scp933ApplyTapeDoAfterEvent();
    }
}

[Serializable, NetSerializable]
public sealed partial class Scp933RipTapeDoAfterEvent : DoAfterEvent
{
    public NetEntity ExpectedMask;
    public bool EmergencyMode;

    public override DoAfterEvent Clone()
    {
        return new Scp933RipTapeDoAfterEvent
        {
            ExpectedMask = ExpectedMask,
            EmergencyMode = EmergencyMode,
        };
    }
}
