using Content.Server.Chat.Systems;
using Content.Shared._Scp.Scp939;
using Content.Shared.Mobs.Components;
using Content.Shared.Throwing;
using Robust.Server.Audio;

namespace Content.Server._Scp.Scp939;

public sealed partial class Scp939System
{
    [Dependency] private readonly AudioSystem _audioSystem = default!;

    private void InitializeVisibility()
    {
        SubscribeLocalEvent<MobStateComponent, ComponentStartup>(OnMobStartup);

        SubscribeLocalEvent<Scp939VisibilityComponent, EntitySpokeEvent>(OnTargetSpoke);
        SubscribeLocalEvent<Scp939VisibilityComponent, EmoteEvent>(OnTargetEmote);
        SubscribeLocalEvent<Scp939VisibilityComponent, ThrowEvent>(OnThrow);
    }

    private void OnThrow(Entity<Scp939VisibilityComponent> ent, ref ThrowEvent args)
    {
        MobDidSomething(ent);
    }

    private void OnTargetEmote(Entity<Scp939VisibilityComponent> ent, ref EmoteEvent args)
    {
        MobDidSomething(ent);
    }

    private void OnMobStartup(Entity<MobStateComponent> ent, ref ComponentStartup args)
    {
        if (HasComp<Scp939Component>(ent))
        {
            return;
        }

        var visibilityComponent = EnsureComp<Scp939VisibilityComponent>(ent);
        visibilityComponent.VisibilityAcc = 0;
    }

    private void OnTargetSpoke(Entity<Scp939VisibilityComponent> ent, ref EntitySpokeEvent args)
    {
        MobDidSomething(ent);
    }

    private void MobDidSomething(Entity<Scp939VisibilityComponent> ent)
    {
        ent.Comp.VisibilityAcc = 0;
        Dirty(ent);
    }
}
