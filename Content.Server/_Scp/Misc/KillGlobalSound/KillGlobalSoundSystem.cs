using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Whitelist;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Scp.Misc.KillGlobalSound;

public sealed class KillGlobalSoundSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const float ExceptRange = 16f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidAppearanceComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<HumanoidAppearanceComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!TryComp<KillGlobalSoundComponent>(args.Origin, out var killSoundComponent))
            return;

        if (_entityWhitelist.IsWhitelistFailOrNull(killSoundComponent.OriginWhitelist, args.Origin.Value))
            return;

        if (!_random.Prob(killSoundComponent.Chance))
            return;

        var xform = Transform(ent);

        if (!xform.GridUid.HasValue)
            return;

        var coords = _transform.GetMapCoordinates(ent);
        var filter = Filter.BroadcastGrid(xform.GridUid.Value)
            .AddInRange(coords, killSoundComponent.MaxRadius)
            .RemoveInRange(coords, ExceptRange);

        _audio.PlayGlobal(killSoundComponent.Sound, filter, true);
    }
}
