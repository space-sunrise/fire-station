using Content.Server.Ghost;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;

namespace Content.Server.Light.EntitySystems;

/// <summary>
///     System for the PoweredLightComponents
/// </summary>
public sealed class PoweredLightSystem : SharedPoweredLightSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PoweredLightComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<PoweredLightComponent, GhostBooEvent>(OnGhostBoo);
    }

    private void OnGhostBoo(EntityUid uid, PoweredLightComponent light, GhostBooEvent args)
    {
        if (light.IgnoreGhostsBoo || HasComp<BlinkingPoweredLightComponent>(uid))
            return; // The light is immune or already blinking.

        // check cooldown first to prevent abuse
        var curTime = GameTiming.CurTime;
        if (light.LastGhostBlink != null && curTime <= light.LastGhostBlink + light.GhostBlinkingCooldown)
            return;

        light.LastGhostBlink = curTime;

        var blinkingComp = EnsureComp<BlinkingPoweredLightComponent>(uid);
        blinkingComp.StopBlinkingTime = curTime + light.GhostBlinkingTime;
        Dirty(uid, blinkingComp);

        args.Handled = true;
    }

    private void OnMapInit(EntityUid uid, PoweredLightComponent light, MapInitEvent args)
    {
        // TODO: Use ContainerFill dog
        if (light.HasLampOnSpawn != null)
        {
            // Fire edit start - для того, чтобы при переносе сломанных лампочек из раунда в раунд стандартные лампочкине валялась на полу
            var entity = Spawn(light.HasLampOnSpawn, Comp<TransformComponent>(uid).Coordinates);
            var canInsert = ContainerSystem.Insert(entity, light.LightBulbContainer);

            if (!canInsert)
            {
                QueueDel(entity);
                return;
            }
            // Fire edit end
        }
        // need this to update visualizers
        UpdateLight(uid, light);
    }
}
