using System.Diagnostics.CodeAnalysis;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Mobs.Components;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.ScpMask;

/// <summary>
/// Система масок для сцп объектов, работающая для абсолютно всех гуманоидных объектов, которые могут носить одежду.
/// Позволяет контролировать будет ли надета маска, дает способность разорвать маску.
/// Имеет два метода для работы извне для получения Entity c маской и ее разрыва
/// </summary>
/// TODO: Рефактор на выдачу компонента на время ношения маски. Компонент должен хранить ссылку на маску
public sealed partial class ScpMaskSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly PredictedRandomSystem _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpComponent, ScpTearMaskEvent>(OnTear);
        SubscribeLocalEvent<ScpComponent, ScpTearMaskDoAfterEvent>(OnTearSuccess);

        SubscribeLocalEvent<ScpComponent, DamageChangedEvent>(OnDamage);

        InitializeEquipment();
        InitializeRestrictions();

        Log.Level = LogLevel.Info;
    }

    private void OnTear(Entity<ScpComponent> scp, ref ScpTearMaskEvent args)
    {
        if (!TryGetScpMask(scp, out var scpMask))
            return;

        // Нельзя снять маску, пока действует сейвтайм
        if (scpMask.Value.Comp.SafeTimeEnd != null && _timing.CurTime < scpMask.Value.Comp.SafeTimeEnd)
        {
            var message = Loc.GetString("scp-mask-cannot-tear-safetime", ("time", scpMask.Value.Comp.SafeTimeEnd - _timing.CurTime));
            _popup.PopupClient(message, scp, scp);

            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, scp, scpMask.Value.Comp.TearTime, new ScpTearMaskDoAfterEvent(), scp, scp, scpMask)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnTearSuccess(Entity<ScpComponent> scp, ref ScpTearMaskDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryTear(scp))
            return;

        args.Handled = true;
    }

    private void OnDamage(Entity<ScpComponent> scp, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        // Нужен только тот урон, что нанесли игроки
        if (!args.Origin.HasValue)
            return;

        if (!TryGetScpMask(scp, out var scpMask))
            return;

        if (!_random.ProbForEntity(scp, scpMask.Value.Comp.TearChanceOnDamage))
            return;

        TryTear(scp, scpMask.Value);
    }

    #region Public API

    /// <summary>
    /// Моментально маску на выбранном сцп
    /// </summary>
    /// <returns>Успешна ли попытка порвать маску</returns>
    public bool TryTear(EntityUid scp)
    {
        if (!TryGetScpMask(scp, out var scpMask))
            return false;

        if (!TryTear(scp, scpMask.Value))
            return false;

        return true;
    }

    public bool TryTear(EntityUid scp, Entity<ScpMaskComponent> mask)
    {
        if (mask.Comp.TearSound != null)
            _audio.PlayPvs(mask.Comp.TearSound,scp);

        var message = Loc.GetString(
            "scp-destroyed-mask",
            ("scp", Name(scp)),
            ("mask", Name(mask))
        );

        _popup.PopupPredicted(message, scp, null, PopupType.LargeCaution);

        if (_net.IsServer)
            QueueDel(mask);

        return true;
    }

    /// <summary>
    /// Метод, позволяющий проверить, надета ли на сцп маска. В случае привета передает ентити маски
    /// </summary>
    /// <returns>Надета ли маска</returns>
    public bool TryGetScpMask(EntityUid scp, [NotNullWhen(true)] out Entity<ScpMaskComponent>? mask)
    {
        mask = null;

        if (!_inventory.TryGetInventoryEntity<ScpMaskComponent>(scp, out var maskEntity))
            return false;

        if (!Resolve(maskEntity, ref maskEntity.Comp))
            return false;

        if (!Exists(maskEntity) || Terminating(maskEntity))
            return false;

        mask = (maskEntity.Owner, maskEntity.Comp);

        return true;
    }

    public bool HasScpMask(EntityUid scp)
    {
        return TryGetScpMask(scp, out _);
    }

    /// <summary>
    /// Создает попап, говорящий о невозможности использовать способность из-за маски
    /// </summary>
    public bool TryCreatePopup(EntityUid scp, EntityUid? mask)
    {
        if (!mask.HasValue)
            return false;

        var message = Loc.GetString("scp-mask-action-blocked", ("mask", Name(mask.Value)));
        if (_net.IsServer) // Для работы вызовов с сервера
            _popup.PopupEntity(message, scp, scp, PopupType.LargeCaution);

        return true;
    }

    #endregion

}

#region Events

public sealed partial class ScpTearMaskEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class ScpTearMaskDoAfterEvent : SimpleDoAfterEvent;


#endregion

