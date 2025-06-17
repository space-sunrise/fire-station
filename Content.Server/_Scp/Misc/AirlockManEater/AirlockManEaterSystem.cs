using System.Linq;
using System.Threading;
using Content.Server.Doors.Systems;
using Content.Server.Interaction;
using Content.Shared._Scp.Other.Events;
using Content.Shared.Doors.Components;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Scp.Misc.AirlockManEater;

public sealed class AirlockManEaterSystem : EntitySystem
{
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly AirlockSystem _airlock = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan CrushAgainAfter = TimeSpan.FromSeconds(0.5f);
    private static readonly TimeSpan LaughAfter = TimeSpan.FromSeconds(0.3f);

    private const float VictimSearchRadiusOpen = 4.5f;
    private const float VictimSearchRadiusClose = 2.3f;

    private static readonly TimeSpan VictimSearchDelay = TimeSpan.FromSeconds(0.3f);
    private static TimeSpan _nextVictimSearchTime = TimeSpan.Zero;

    private static readonly SoundSpecifier AirlockLaughSound = new SoundPathSpecifier("/Audio/Machines/airlock_deny.ogg");

    private static EntityQuery<DoorComponent> _doors;
    private static EntityQuery<AirlockComponent> _airlocks;

    private static CancellationTokenSource _token = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockManEaterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AirlockManEaterComponent, AirlockCrushedEvent>(OnCrush);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => Clear());

        _doors = GetEntityQuery<DoorComponent>();
        _airlocks = GetEntityQuery<AirlockComponent>();
    }

    // Возможно это не самый производительный способ
    // Но зато смешно. Ловушка шлюзера
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextVictimSearchTime)
            return;

        var query = AllEntityQuery<AirlockManEaterComponent, DoorComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var door, out var xform))
        {
            var nearbyEntities = _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(xform.Coordinates, VictimSearchRadiusOpen)
                .Where(e => IsProperVictim(uid, e, VictimSearchRadiusOpen))
                .ToList();

            var closeEntities = _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(xform.Coordinates, VictimSearchRadiusClose)
                .Where(e => IsProperVictim(uid, e, VictimSearchRadiusClose))
                .ToHashSet();

            var midRangeEntities = nearbyEntities.Where(e => !closeEntities.Contains(e)).ToList();

            // Закрытие, если кто-то вблизи
            if (closeEntities.Any())
            {
                if (door.State == DoorState.Closed || door.State == DoorState.Closing)
                    continue;

                _door.TryClose(uid, door);
                continue;
            }

            //  Открытие, если кто-то в миде
            if (midRangeEntities.Any())
            {
                if (door.State == DoorState.Open || door.State == DoorState.Opening)
                    continue;

                _door.TryOpen(uid, door);
            }
        }

        _nextVictimSearchTime = _timing.CurTime + VictimSearchDelay;
    }

    private void OnMapInit(Entity<AirlockManEaterComponent> ent, ref MapInitEvent args)
    {
        DoAirlockStuff(ent);
        DoDoorStuff(ent);
    }

    private void DoAirlockStuff(Entity<AirlockManEaterComponent> ent)
    {
        if (!_airlocks.TryComp(ent, out var airlockComponent))
            return;

        _airlock.SetSafety(airlockComponent, false);
        _airlock.SetAutoCloseDelayModifier(airlockComponent, AirlockManEaterComponent.AutoCloseModifier);
    }

    private void DoDoorStuff(Entity<AirlockManEaterComponent> ent)
    {
        if (!_doors.TryComp(ent, out var doorComponent))
            return;

        doorComponent.CanCrush = true;
        doorComponent.CrushDamage = ent.Comp.CrushDamage;
        doorComponent.DoorStunTime = ent.Comp.StunTime;

        doorComponent.OpenTimeOne /= AirlockManEaterComponent.TimeModifier;
        doorComponent.OpenTimeTwo /= AirlockManEaterComponent.TimeModifier;
        doorComponent.CloseTimeOne /= AirlockManEaterComponent.TimeModifier;
        doorComponent.CloseTimeTwo /= AirlockManEaterComponent.TimeModifier;

        doorComponent.OpeningAnimationTime /= AirlockManEaterComponent.TimeModifier;
        doorComponent.ClosingAnimationTime /= AirlockManEaterComponent.TimeModifier;

        doorComponent.UnsafeClosing = true;

        Dirty(ent, doorComponent);
    }

    private void OnCrush(Entity<AirlockManEaterComponent> ent, ref AirlockCrushedEvent args)
    {
        var entity = GetEntity(args.Entity);

        if (!HasComp<MobStateComponent>(entity))
            return;

        if (_mob.IsDead(entity))
            return;

        Timer.Spawn(LaughAfter, () => _audio.PlayPvs(AirlockLaughSound, ent, AudioParams.Default.WithPitchScale(0.5f)), _token.Token);
        Timer.Spawn(CrushAgainAfter, () => _door.TryOpen(ent), _token.Token);

        // TODO: Какой-нибудь звук победы шлюза над человеком
    }

    private static void Clear()
    {
        _token.Cancel();
        _token = new();
    }

    private bool IsProperVictim(EntityUid airlock, EntityUid human, float range)
    {
        return (_mob.IsAlive(human) || _mob.IsCritical(human)) && _interaction.InRangeUnobstructed(airlock, Transform(human).Coordinates, range);
    }
}
