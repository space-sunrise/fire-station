using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Robust.Shared.Localization;
using Robust.Shared.Network;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Система для управления SCP-933-02 - портабщиком ленты.
/// Управляет контролем жертв, применением ленты и поведением.
/// </summary>
public abstract class SharedScp933MasterSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp933MasterComponent, ComponentStartup>(OnMasterStartup);
        SubscribeLocalEvent<Scp933MasterComponent, ComponentShutdown>(OnMasterShutdown);

        SubscribeLocalEvent<Scp933ControlledComponent, ComponentStartup>(OnControlledStartup);
        SubscribeLocalEvent<Scp933ControlledComponent, ComponentShutdown>(OnControlledShutdown);

        SubscribeLocalEvent<Scp933ControlledComponent, MobStateChangedEvent>(OnControlledMobStateChanged);
    }

    private void OnMasterStartup(Entity<Scp933MasterComponent> ent, ref ComponentStartup args)
    {
        if (!_net.IsServer)
            return;

        EraseFaceFor933(ent);

        _popup.PopupEntity(Loc.GetString("scp933-master-awakened"), ent, ent, PopupType.LargeCaution);
    }

    private void OnMasterShutdown(Entity<Scp933MasterComponent> ent, ref ComponentShutdown args)
    {
        if (!_net.IsServer)
            return;

        // Освободить всех контролируемых
        foreach (var controlled in ent.Comp.Controlled)
        {
            if (TryComp<Scp933ControlledComponent>(controlled, out var controlledComp))
                controlledComp.Master = null;

            RemComp<Scp933ControlledComponent>(controlled);
        }
    }

    private void OnControlledStartup(Entity<Scp933ControlledComponent> ent, ref ComponentStartup args)
    {
        if (!_net.IsServer)
            return;

        // Миньоны уже получают компоненты при контроле, ничего не делаем
    }

    private void OnControlledShutdown(Entity<Scp933ControlledComponent> ent, ref ComponentShutdown args)
    {
        if (!_net.IsServer)
            return;

        // Удалить muzzle компонент
        RemComp<MutedComponent>(ent);

        // Удалить из списка контролируемых у мастера
        if (ent.Comp.Master.HasValue && TryComp<Scp933MasterComponent>(ent.Comp.Master, out var master))
        {
            master.Controlled.Remove(ent);
        }
    }

    private void OnControlledMobStateChanged(Entity<Scp933ControlledComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        RemComp<Scp933ControlledComponent>(ent);
    }

    /// <summary>
    /// Спрятать лицо гуманоида (SCP-933): хост и порабощённые жертвы.
    /// </summary>
    public void EraseFaceFor933(EntityUid uid)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidComp))
            return;

        var humanoidEnt = new Entity<HumanoidAppearanceComponent?>(uid, humanoidComp);
        _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.Eyes, false);
        _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.Snout, false);
        _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.Head, false);
        _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.Hair, false);
        _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.FacialHair, false);
        _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.HeadTop, false);
        _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.HeadSide, false);
        _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.SnoutCover, false);
    }

    /// <summary>
    /// Превратить сущность в нового носителя SCP-933-02 (отдельно от ритуала ленты).
    /// </summary>
    public void ConvertToMaster(EntityUid victim)
    {
        if (!_net.IsServer)
            return;

        if (HasComp<Scp933MasterComponent>(victim))
            return;

        // Добавить компонент босса
        var masterComp = new Scp933MasterComponent();
        AddComp(victim, masterComp);

        EraseFaceFor933(victim);

        _popup.PopupEntity(Loc.GetString("scp933-master-awakened"), victim, victim, PopupType.LargeCaution);
    }

    /// <summary>
    /// Поработить жертву после ритуала: без лица, немая, связь с мастером.
    /// </summary>
    public void DominateVictim(EntityUid master, EntityUid victim)
    {
        if (!_net.IsServer)
            return;

        if (!TryComp<Scp933MasterComponent>(master, out var masterComp))
            return;

        if (!TryComp<HumanoidAppearanceComponent>(victim, out _))
            return;

        if (HasComp<Scp933ControlledComponent>(victim) || HasComp<Scp933MasterComponent>(victim))
            return;

        if (masterComp.Controlled.Count >= masterComp.MaxControlled)
            return;

        var controlComp = new Scp933ControlledComponent { Master = master };
        AddComp(victim, controlComp);

        if (!HasComp<MutedComponent>(victim))
            AddComp(victim, new MutedComponent());

        EraseFaceFor933(victim);

        masterComp.Controlled.Add(victim);
        Dirty(master, masterComp);

        _popup.PopupEntity(Loc.GetString("scp933-victim-dominated"), victim, victim, PopupType.LargeCaution);
    }
}
