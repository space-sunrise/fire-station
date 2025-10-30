using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared._Scp.Other.DamageOnCollide;

public sealed class ScpDamageOnCollideSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpDamageOnCollideComponent, StartCollideEvent>(OnCollide);

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
    }

    private void OnCollide(Entity<ScpDamageOnCollideComponent> ent, ref StartCollideEvent args)
    {
        foreach (var param in ent.Comp.Params)
        {
            if (!CheckParameter(args.OtherEntity, param))
                continue;

            _damageable.TryChangeDamage(args.OtherEntity, param.Damage, useVariance: param.UseVariance);

            _audio.PlayPredicted(param.TargetSound, args.OtherEntity, ent);
            _audio.PlayPredicted(param.EntitySound, ent, ent);

            // Выходим, как только нанесли урон.
            return;
        }
    }

    /// <summary>
    /// Проверяет, подходит ли сущность под параметры
    /// </summary>
    /// <param name="target">Сущность для проверки</param>
    /// <param name="param">Параметры, указанные в компоненте</param>
    /// <returns>Подходит или нет</returns>
    private bool CheckParameter(EntityUid target, DamageOnCollideParameters param)
    {
        // Если вайтлист не проходит - сущность не подходит
        if (!_whitelist.IsWhitelistPassOrNull(param.Whitelist, target))
            return false;

        // Если блеклист проходит - то сущность не подходит. Это же блеклист
        if (!_whitelist.IsWhitelistFailOrNull(param.Blacklist, target))
            return false;

        if (param.RequiresVelocity && !CheckVelocity(target))
            return false;

        if (param.RequiredMobStates != null && !CheckMobState(target, param.RequiredMobStates))
            return false;

        return true;
    }

    private bool CheckVelocity(EntityUid target)
    {
        if (!_physicsQuery.TryComp(target, out var physics))
            return false;

        if (physics.LinearVelocity.IsLengthZero())
            return false;

        return true;
    }

    private bool CheckMobState(EntityUid target, List<MobState> states)
    {
        if (!_mobStateQuery.TryComp(target, out var mobState))
            return false;

        if (!states.Contains(mobState.CurrentState))
            return false;

        return true;
    }
}
