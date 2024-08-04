using Content.Shared._Scp.Abilities;
using Robust.Client.GameObjects;

namespace Content.Client._Scp.Abilities;

public sealed class BorgResistSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<BorgShieldEnabledEvent>(OnEnabled);
        SubscribeNetworkEvent<BorgShieldDisabledEvent>(OnDisabled);
    }

    private void OnEnabled(BorgShieldEnabledEvent args)
    {
        var uid = GetEntity(args.Borg);

        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
            return;

        spriteComponent.LayerSetVisible(BorgResistVisuals.Shielding, true);
    }

    private void OnDisabled(BorgShieldDisabledEvent args)
    {
        var uid = GetEntity(args.Borg);

        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
            return;

        spriteComponent.LayerSetVisible(BorgResistVisuals.Shielding, false);
    }
}
