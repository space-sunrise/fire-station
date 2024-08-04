using Content.Shared._Scp.Abilities;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.PowerCell;
using Content.Shared.Throwing;
using Robust.Client.GameObjects;

namespace Content.Client._Scp.Abilities;

/// <inheritdoc/>
public sealed class BorgDashSystem : SharedBorgDashSystem
{
    private ISawmill _sawmill = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("borg.dash");
        SubscribeNetworkEvent<BorgThrownEvent>(OnDash);
        SubscribeNetworkEvent<BorgLandedEvent>(OnLanded);
    }

    private void OnDash(BorgThrownEvent args)
    {
        var uid = GetEntity(args.Borg);

        _sawmill.Debug($"ondash: {uid}");

        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
            return;

        spriteComponent.LayerSetVisible(BorgDashVisuals.NotDashing, false);
        spriteComponent.LayerSetVisible(BorgDashVisuals.Dashing, true);
    }

    private void OnLanded(BorgLandedEvent args)
    {
        var uid = GetEntity(args.Borg);

        _sawmill.Debug($"onlanded: {uid}");

        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
            return;

        spriteComponent.LayerSetVisible(BorgDashVisuals.NotDashing, true);
        spriteComponent.LayerSetVisible(BorgDashVisuals.Dashing, false);
    }
}
