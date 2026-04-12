using Content.Shared._Scp.Scp933;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Server._Scp.Scp933;

/// <summary>
/// Server система для управления SCP-933-02.
/// Скрывает/показывает маску, обрабатывает превращения в босса.
/// </summary>
public sealed class Scp933MasterSystem : SharedScp933MasterSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Снять маску с жертвы - жертва становится новым боссом 933-02.
    /// </summary>
    public void RemoveTapeMask(EntityUid master, EntityUid victim)
    {
        if (!_net.IsServer)
            return;

        if (!HasComp<TapedFaceComponent>(victim))
            return;

        // Удалить маску
        RemComp<TapedFaceComponent>(victim);
        RemComp<MutedComponent>(victim);

        // Удалить из списка контролируемых у старого мастера
        if (TryComp<Scp933MasterComponent>(master, out var masterComp))
        {
            masterComp.Controlled.Remove(victim);
            Dirty(master, masterComp);
        }

        // Удалить компонент старого контроля
        RemComp<Scp933ControlledComponent>(victim);

        // Скрыть Head слой (когда маска снимается, лицо все равно скрыто)
        if (TryComp<HumanoidAppearanceComponent>(victim, out var humanoidComp))
        {
            var humanoidEnt = new Entity<HumanoidAppearanceComponent?>(victim, humanoidComp);
            _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.Head, false);
        }

        // Превратить в нового босса 933-02
        ConvertToMaster(victim);
    }

    /// <summary>
    /// Получить слой для визуализации маски.
    /// Используем "Ensnare" для маски ленты.
    /// </summary>
    private HumanoidVisualLayers GetTapeMaskLayer()
    {
        return HumanoidVisualLayers.Ensnare; // Используем Ensnare слой для маски
    }
}
