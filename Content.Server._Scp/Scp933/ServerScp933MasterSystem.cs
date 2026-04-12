using Content.Server.NPC.Systems;
using Content.Shared._Scp.Scp933;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server._Scp.Scp933;

/// <summary>
/// Сервер-сторонная система для SCP-933-02.
/// Управляет соответствующим на жертв, применением ленты и контролем.
/// </summary>
public sealed class ServerScp933MasterSystem : SharedScp933MasterSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly NpcBalancingSystem _npcBalancing = default!;

    private float _updateTimer = 0f;
    private const float UpdateInterval = 0.5f; // Обновляем каждые полсекунды

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Scp933MasterComponent, StartCollideEvent>(OnMasterCollide);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;
        if (_updateTimer < UpdateInterval)
            return;

        _updateTimer = 0f;

        // Обновить логику для всех SCP-933-02
        var masterQuery = EntityQueryEnumerator<Scp933MasterComponent>();
        while (masterQuery.MoveNext(out var uid, out var master))
        {
            UpdateMaster(uid, master);
        }
    }

    private void UpdateMaster(EntityUid uid, Scp933MasterComponent master)
    {
        if (!_mobState.IsAlive(uid))
            return;

        // Получить видимые цели рядом
        var nearbyTargets = GetNearbyTargets((uid, master), master.VisionRange);

        // Выбрать ближайшую цель
        EntityUid? closestTarget = null;
        float closestDistance = float.MaxValue;

        var masterPos = Transform(uid).WorldPosition;

        foreach (var target in nearbyTargets)
        {
            if (!TryComp<TransformComponent>(target, out var targetTransform))
                continue;

            var distance = Vector2.Distance(masterPos, targetTransform.WorldPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }

        master.CurrentTarget = closestTarget;
        Dirty(uid, master);
    }

    /// <summary>
    /// Обработка столкновения SCP-933-02 с жертвой.
    /// </summary>
    private void OnMasterCollide(Entity<Scp933MasterComponent> ent, ref StartCollideEvent args)
    {
        if (!ent.Comp.CurrentTarget.HasValue)
            return;

        var other = args.OtherEntity;

        if (other != ent.Comp.CurrentTarget)
            return;

        // Убедиться что это человек
        if (!TryComp<HumanoidAppearanceComponent>(other, out _))
            return;

        // Убедиться что не уже контролируемый
        if (HasComp<Scp933ControlledComponent>(other))
            return;

        // Активировать контроль
        ControlVictim(ent, other);
    }
}
