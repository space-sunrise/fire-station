using System.Linq;
using Robust.Client.Player;
using Robust.Shared.Audio.Components;

namespace Content.Client._Scp.Audio;

public sealed class AudioEffectsEntryPointSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AudioComponent, ComponentAdd>(OnAudioAdd);
    }

    private void OnAudioAdd(Entity<AudioComponent> ent, ref ComponentAdd args)
    {
        if (ent.Comp.Global)
            return;

        if (!_player.LocalEntity.HasValue)
            return;

        if (ent.Comp.ExcludedEntity == _player.LocalEntity)
            return;

        AddComp<AudioEffectedComponent>(ent);
    }
}
