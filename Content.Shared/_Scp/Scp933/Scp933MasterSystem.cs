using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// SCP-933: носитель ленты и жертвы после ритуала. Жертвы — живые игроки с дебаффами, без ИИ и без «рабства».
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

        SubscribeLocalEvent<Scp933FaceTornComponent, ComponentShutdown>(OnFaceTornShutdown);
        SubscribeLocalEvent<Scp933FaceTornComponent, MobStateChangedEvent>(OnFaceTornMobStateChanged);
    }

    private void OnMasterStartup(Entity<Scp933MasterComponent> ent, ref ComponentStartup args)
    {
        if (!_net.IsServer)
            return;

        EraseFaceFor933(ent);

        _popup.PopupEntity(Loc.GetString("scp933-host-emerged"), ent, ent, PopupType.LargeCaution);
    }

    private void OnMasterShutdown(Entity<Scp933MasterComponent> ent, ref ComponentShutdown args)
    {
        if (!_net.IsServer)
            return;

        var victims = ent.Comp.FaceTornVictims.ToArray();
        ent.Comp.FaceTornVictims.Clear();
        foreach (var victim in victims)
            RemComp<Scp933FaceTornComponent>(victim);
    }

    private void OnFaceTornShutdown(Entity<Scp933FaceTornComponent> ent, ref ComponentShutdown args)
    {
        if (!_net.IsServer)
            return;

        if (ent.Comp.MutedByScp933)
            RemComp<MutedComponent>(ent);

        if (ent.Comp.TornBy is { } bearer && TryComp<Scp933MasterComponent>(bearer, out var master))
            master.FaceTornVictims.Remove(ent);
    }

    private void OnFaceTornMobStateChanged(Entity<Scp933FaceTornComponent> ent, ref MobStateChangedEvent args)
    {
        if (!_net.IsServer)
            return;

        if (args.NewMobState != MobState.Dead)
            return;

        RemComp<Scp933FaceTornComponent>(ent);
    }

    /// <summary>
    /// Спрятать лицо гуманоида (SCP-933): носитель и жертвы после ритуала.
    /// </summary>
    public void EraseFaceFor933(EntityUid uid)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidComp))
            return;

        var humanoidEnt = new Entity<HumanoidAppearanceComponent?>(uid, humanoidComp);
        _humanoid.SetLayersVisibility(humanoidEnt,
        [
            HumanoidVisualLayers.Eyes,
            HumanoidVisualLayers.Snout,
            HumanoidVisualLayers.Head,
            HumanoidVisualLayers.Hair,
            HumanoidVisualLayers.FacialHair,
            HumanoidVisualLayers.HeadTop,
            HumanoidVisualLayers.HeadSide,
            HumanoidVisualLayers.SnoutCover,
        ], false);
    }

    /// <summary>
    /// Выдать носителю ленты после инкубации.
    /// </summary>
    public void ConvertToMaster(EntityUid victim)
    {
        if (!_net.IsServer)
            return;

        if (HasComp<Scp933MasterComponent>(victim))
            return;

        EnsureComp<Scp933MasterComponent>(victim);
    }

    /// <summary>
    /// Финал ритуала: срыв ленты с лица — визуал без лица и немота. Управление остаётся у игрока-жертвы.
    /// </summary>
    public void ApplyFaceTornAfterRip(EntityUid tapeBearer, EntityUid victim)
    {
        if (!_net.IsServer)
            return;

        if (!TryComp<Scp933MasterComponent>(tapeBearer, out var masterComp))
            return;

        if (!TryComp<HumanoidAppearanceComponent>(victim, out _))
            return;

        if (HasComp<Scp933FaceTornComponent>(victim) || HasComp<Scp933MasterComponent>(victim))
            return;

        var torn = EnsureComp<Scp933FaceTornComponent>(victim);
        torn.TornBy = tapeBearer;
        torn.MutedByScp933 = true;
        EnsureComp<MutedComponent>(victim);

        EraseFaceFor933(victim);

        masterComp.FaceTornVictims.Add(victim);
        Dirty(tapeBearer, masterComp);

        _popup.PopupEntity(Loc.GetString("scp933-victim-face-torn"), victim, victim, PopupType.LargeCaution);
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
