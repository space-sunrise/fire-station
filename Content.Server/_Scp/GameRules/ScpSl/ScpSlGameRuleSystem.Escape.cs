using Content.Shared._Scp.GameRule.Sl;
using Content.Shared.Cuffs.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server._Scp.GameRules.ScpSl;

public sealed partial class ScpSlGameRuleSystem
{
    private void InitializeEscape()
    {
        SubscribeLocalEvent<ScpSlEscapeZoneComponent, StartCollideEvent>(OnEscapeZoneCollide);
    }

    private void OnEscapeZoneCollide(Entity<ScpSlEscapeZoneComponent> ent, ref StartCollideEvent args)
    {
        if (!TryGetActiveRule(out var rule))
        {
            return;
        }

        var collidedEntityUid = args.OtherEntity;

        if (!TryComp<ScpSlHumanoidMarkerComponent>(collidedEntityUid, out var humanoidMarkerComponent)
            || !_mindSystem.TryGetMind(collidedEntityUid, out var mindEntity, out var mindComponent))
        {
            return;
        }

        var humanoidToSpawn = humanoidMarkerComponent.HumanoidType;

        if (humanoidToSpawn == ScpSlHumanoidType.ClassD)
        {
            if (IsCuffed(collidedEntityUid))
            {
                humanoidToSpawn = ScpSlHumanoidType.Mog;
            }
            else
            {
                humanoidToSpawn = ScpSlHumanoidType.Chaos;
                rule.Value.Comp1.EscapedDClass++;
            }
        }
        else if (humanoidToSpawn == ScpSlHumanoidType.Scientist)
        {
            rule.Value.Comp1.EscapedScientists++;
            humanoidToSpawn = ScpSlHumanoidType.Mog;
        }
        else
        {
            return;
        }

        var outfit = SelectHumanoidPreset(rule.Value, humanoidToSpawn);

        var spawnPoint = SelectRandomSpawnPosition(humanoidToSpawn);

        var newHumanoidUid = _randomHumanoidSystem.SpawnRandomHumanoid(outfit, spawnPoint, string.Empty);

        _mindSystem.TransferTo(mindEntity, newHumanoidUid);

        Del(collidedEntityUid);
    }

    private bool IsCuffed(EntityUid entityUid)
    {
        return TryComp<CuffableComponent>(entityUid, out var cuffableComponent)
               && cuffableComponent.CuffedHandCount >= 2;
    }
}
