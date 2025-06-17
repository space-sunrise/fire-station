using System.Linq;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Other.AirlockManEater;

public abstract class SharedAirlockManEaterSystem : EntitySystem
{
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float VictimSearchRadiusOpen = 5f;
    private const float VictimSearchRadiusClose = 2.3f;

    private static readonly TimeSpan VictimSearchDelay = TimeSpan.FromSeconds(0.3f);
    private static TimeSpan _nextVictimSearchTime = TimeSpan.Zero;

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

    private bool IsProperVictim(EntityUid airlock, EntityUid human, float range)
    {
        return (_mob.IsAlive(human) || _mob.IsCritical(human)) && _interaction.InRangeUnobstructed(airlock, Transform(human).Coordinates, range);
    }
}
