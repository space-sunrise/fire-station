using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
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

        // Скрыть лицо
        if (TryComp<HumanoidAppearanceComponent>(ent, out var humanoidComp))
        {
            var humanoidEnt = new Entity<HumanoidAppearanceComponent?>(ent, humanoidComp);
            _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.Eyes, false);
            _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.Snout, false);
            _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.Head, false);
        }

        _popup.PopupEntity("SCP-933-02 пробудился...", ent, ent, PopupType.LargeCaution);
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
    /// Применить ленту на жертву. Первая жертва становится 933-02 (босс).
    /// </summary>
    public void ApplyTape(EntityUid victim)
    {
        if (!_net.IsServer)
            return;

        // Если у жертвы уже есть лента - выход
        if (HasComp<Scp933MasterComponent>(victim) || HasComp<Scp933ControlledComponent>(victim))
            return;

        if (!TryComp<HumanoidAppearanceComponent>(victim, out _))
            return;

        // Проверить есть ли уже мастер 933
        var masters = EntityQueryEnumerator<Scp933MasterComponent>();
        if (!masters.MoveNext(out var masterId, out _))
        {
            // Нет боссов - эта жертва становится боссом 933-02
            ConvertToMaster(victim);
        }
        else
        {
            // Уже есть босс - эта жертва становится миньоном
            ControlVictim(masterId, victim);
        }
    }

    /// <summary>
    /// Превратить жертву в SCP-933-02 (босс).
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

        // Скрыть лицо
        if (TryComp<HumanoidAppearanceComponent>(victim, out var humanoidComp))
        {
            var humanoidEnt = new Entity<HumanoidAppearanceComponent?>(victim, humanoidComp);
            _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.Head, false);
        }

        _popup.PopupEntity("SCP-933-02 пробудился...", victim, victim, PopupType.LargeCaution);
    }

    /// <summary>
    /// Контролировать жертву как миньон босса.
    /// </summary>
    public void ControlVictim(EntityUid master, EntityUid victim)
    {
        if (!_net.IsServer)
            return;

        if (!TryComp<Scp933MasterComponent>(master, out var masterComp))
            return;

        if (!TryComp<HumanoidAppearanceComponent>(victim, out _))
            return;

        // Если уже контролирует - выход
        if (HasComp<Scp933ControlledComponent>(victim) || HasComp<Scp933MasterComponent>(victim))
            return;

        // Добавить компонент контроля
        var controlComp = new Scp933ControlledComponent { Master = master };
        AddComp(victim, controlComp);

        // Добавить маску ленты
        if (!HasComp<TapedFaceComponent>(victim))
        {
            AddComp(victim, new TapedFaceComponent());
        }

        // Добавить MutedComponent чтобы не могли говорить
        if (!HasComp<MutedComponent>(victim))
        {
            AddComp(victim, new MutedComponent());
        }

        // Скрыть лицо и добавить визуальный слой маски
        if (TryComp<HumanoidAppearanceComponent>(victim, out var humanoidComp))
        {
            var humanoidEnt = new Entity<HumanoidAppearanceComponent?>(victim, humanoidComp);
            _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.Head, false);
            // Показать слой маски (используем Ensnare для визуализации ленты на лице)
            _humanoid.SetLayerVisibility(humanoidEnt, HumanoidVisualLayers.Ensnare, true);
        }

        // Добавить в список контролируемых
        masterComp.Controlled.Add(victim);
        Dirty(master, masterComp);

        _popup.PopupEntity("Вы контролируемы SCP-933-02!", victim, victim, PopupType.LargeCaution);
    }
}
