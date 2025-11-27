using Content.Shared._Scp.Blood;
using Robust.Shared.Map;

namespace Content.Server._Scp.Blood;

public sealed partial class BloodSplatterSystem
{
    public void CreateBloodLine(Entity<BloodSplattererComponent> ent, EntityUid target)
    {
        var angle = _random.NextAngle(0, 360);
        var coords = _transform.GetMapCoordinates(target).Offset(angle.ToWorldVec());

        _audio.PlayPvs(ent.Comp.BloodLineSpawnedSound, target);

        SpawnBloodLines(ent, target, coords, angle, _random.Next(1, 3));
    }

    private void SpawnBloodLines(Entity<BloodSplattererComponent> ent,
        EntityUid target,
        MapCoordinates start,
        Angle rotation,
        int count = 1)
    {
        if (count <= 0)
            return;

        var direction = rotation.ToWorldVec();

        for (var i = 0; i <= count; i++)
        {
            var spawnPos = start.Position + direction * i;
            var uid = Spawn(ent.Comp.BloodLineProto, new MapCoordinates(spawnPos, start.MapId));

            if (!TryTakeBlood(target, ent.Comp.BloodToTakeToPerLine, uid))
            {
                QueueDel(uid);
                return;
            }

            _transform.SetWorldRotation(uid, rotation);
        }
    }
}
