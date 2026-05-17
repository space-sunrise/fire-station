using Content.Shared.Humanoid;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// SCP-933: носитель ленты и жертвы после ритуала. Жертвы — живые игроки с дебаффами, без ИИ и без «рабства».
/// </summary>
public abstract class SharedScp933MasterSystem : EntitySystem
{
    [Dependency] protected readonly SharedHumanoidAppearanceSystem HumanoidAppearance = default!;

    /// <summary>
    /// Спрятать лицо гуманоида (SCP-933): носитель и жертвы после ритуала.
    /// </summary>
    public void EraseFaceFor933(EntityUid uid, Scp933RitualSettingsComponent? ritualComp = null)
    {
        if (!TryComp<Scp933PossibleTargetComponent>(uid, out var targetComp) || !targetComp.CanBeFaceTorn)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidComp))
            return;

        if (ritualComp == null)
            TryComp<Scp933RitualSettingsComponent>(uid, out ritualComp);

        var humanoidEnt = new Entity<HumanoidAppearanceComponent?>(uid, humanoidComp);
        var layers = GetFaceErasureLayers(ritualComp);
        HumanoidAppearance.SetLayersVisibility(humanoidEnt, layers, false);
    }

    private static List<HumanoidVisualLayers> GetFaceErasureLayers(Scp933RitualSettingsComponent? ritualComp)
    {
        var layers = new List<HumanoidVisualLayers>();

        if (ritualComp?.HideEyes == true)
            layers.Add(HumanoidVisualLayers.Eyes);

        if (ritualComp?.HideSnout == true)
            layers.Add(HumanoidVisualLayers.Snout);

        if (ritualComp?.AdditionalHiddenLayers != null)
            layers.AddRange(ritualComp.AdditionalHiddenLayers);

        if (layers.Count == 0)
        {
            layers.AddRange(new[]
            {
                HumanoidVisualLayers.Eyes,
                HumanoidVisualLayers.Snout,
                HumanoidVisualLayers.SnoutCover,
                HumanoidVisualLayers.Hair,
                HumanoidVisualLayers.FacialHair,
                HumanoidVisualLayers.HeadTop,
                HumanoidVisualLayers.HeadSide,
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
