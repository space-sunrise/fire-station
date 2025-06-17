using System.Threading;
using Content.Server.Doors.Systems;
using Content.Shared._Scp.Other.Events;
using Content.Shared.Doors.Components;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Scp.Misc.AirlockManEater;

public sealed class AirlockManEaterSystem : EntitySystem
{
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly AirlockSystem _airlock = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private static readonly TimeSpan CrushAgainAfter = TimeSpan.FromSeconds(0.5f);
    private static readonly TimeSpan LaughAfter = TimeSpan.FromSeconds(0.3f);

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

        doorComponent.CloseTimeOne /= AirlockManEaterComponent.TimeModifier;
        doorComponent.CloseTimeTwo /= AirlockManEaterComponent.TimeModifier;

        doorComponent.ClosingAnimationTime /= AirlockManEaterComponent.TimeModifier;

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
    }

    private static void Clear()
    {
        _token.Cancel();
        _token = new();
    }
}
