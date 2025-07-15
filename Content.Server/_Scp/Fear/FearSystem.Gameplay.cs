using Content.Server.Chat.Systems;
using Content.Server.Standing;
using Content.Shared._Scp.Fear.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Movement.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Scp.Fear;

public sealed partial class FearSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly ProtoId<EmotePrototype> ScreamProtoId = "Scream";

    private void InitializeGameplay()
    {
        SubscribeLocalEvent<FearComponent, MoveInputEvent>(OnMove);
    }

    /// <summary>
    /// Обрабатывает событие хождения.
    /// Реализует случайное падение во время сильного страха
    /// </summary>
    private void OnMove(Entity<FearComponent> ent, ref MoveInputEvent args)
    {
        if (ent.Comp.State < ent.Comp.FallOffRequiredState)
            return;

        var percentNormalized = PercentToNormalized(ent.Comp.FallOffChance);

        if (!_random.Prob(percentNormalized))
            return;

        _standing.Fall(ent);
    }

    /// <summary>
    /// Пытается закричать, если увиденный объект настолько страшный.
    /// </summary>
    protected override void TryScream(Entity<FearComponent> ent)
    {
        base.TryScream(ent);

        if (ent.Comp.State < ent.Comp.ScreamRequiredState)
            return;

        _chat.TryEmoteWithChat(ent, ScreamProtoId);
    }
}
