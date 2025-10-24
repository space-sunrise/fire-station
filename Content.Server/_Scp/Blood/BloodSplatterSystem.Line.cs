using Content.Shared._Scp.Blood;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Scp.Blood;

public sealed partial class BloodSplatterSystem
{
    public void CreateBloodLine(Entity<BloodSplattererComponent> ent,
        EntityUid target,
        ref Entity<SolutionComponent>? bloodSolutionEntity,
        Solution bloodSolution)
    {
        var angle = _random.NextAngle(0, 360);
        var coords = _transform.GetMapCoordinates(target).Offset(angle.ToWorldVec());

        SpawnBloodLines(ent, coords, ent.Comp.BloodLineProto, angle, ref bloodSolutionEntity, bloodSolution, _random.Next(1, 3));
    }

    private void SpawnBloodLines(Entity<BloodSplattererComponent> ent,
        MapCoordinates start,
        EntProtoId proto,
        Angle rotation,
        ref Entity<SolutionComponent>? bloodSolutionEntity,
        Solution bloodSolution,
        int count = 1)
    {
        if (!bloodSolutionEntity.HasValue)
        {
            Log.Error("Found blood PARTICLE with null blood Solution Entity");
            return;
        }

        var direction = rotation.ToWorldVec();

        for (var i = 0; i <= count; i++)
        {
            var randomizedBlood =
                _random.NextFloat(ent.Comp.BloodToTakeToPerLine.X, ent.Comp.BloodToTakeToPerLine.Y);

            var amountToTake = FixedPoint2.Min(randomizedBlood, bloodSolution.Volume);
            var solution = _solution.SplitSolution(bloodSolutionEntity.Value, amountToTake);

            // Если в человеке закончилась кровь - больше не спавним.
            if (solution.Volume == FixedPoint2.Zero)
                continue;

            var spawnPos = start.Position + direction * i;
            var uid = Spawn(proto, new MapCoordinates(spawnPos, start.MapId));

            // Временно передает solution сюда, чтобы частичка крови окрасилась в нужный цвет
            if (!_solution.TryGetSolution(uid, SolutionName, out var solutionEntity, out _))
            {
                Log.Error($"Found blood LINE without any solution, prototype: {proto}");
                return;
            }

            _solution.TryAddSolution(solutionEntity.Value, solution);
            _transform.SetWorldRotation(uid, rotation);

            Timer.Spawn(BloodLineAnimatedComponent.AnimationDuration, () => RemComp<BloodLineAnimatedComponent>(uid), _token.Token);
        }
    }
}
