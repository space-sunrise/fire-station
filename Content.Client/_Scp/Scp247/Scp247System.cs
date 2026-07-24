using Content.Shared._Scp.Scp247;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client._Scp.Scp247;

public sealed class Scp247System : SharedScp247System
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_player.LocalEntity is not { } viewer)
            return;

        var protectedViewer = HasComp<Scp247ProtectionComponent>(viewer);
        var query = EntityQueryEnumerator<Scp247Component, SpriteComponent>();
        while (query.MoveNext(out var uid, out var scp247, out var sprite))
        {
            var state = GetVisualState(uid, protectedViewer || scp247.AngryForLocalViewer);
            if (scp247.RenderedState == state)
                continue;

            _sprite.LayerSetRsiState((uid, sprite), 0, state);
            scp247.RenderedState = state;
        }
    }

    protected override void OnWatchTimeReached(Entity<Scp247Component> target, EntityUid viewer)
    {
        if (_player.LocalEntity != viewer)
            return;

        target.Comp.AngryForLocalViewer = true;
    }

    private string GetVisualState(EntityUid target, bool angry)
    {
        if (TryComp<MobStateComponent>(target, out var mobState) && mobState.CurrentState == MobState.Dead)
            return "dead";

        return angry ? "angry" : "scp-247";
    }
}
