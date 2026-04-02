using Content.Shared._Scp.Audio;
using Robust.Client.Player;
using Robust.Shared.Audio.Components;

namespace Content.Client._Scp.Audio;

public sealed class AudioEffectsEntryPointSystem : EntitySystem
{
    [Dependency] private readonly AudioEffectsManagerSystem _effects = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AudioComponent, ComponentAdd>(OnAudioAdd);
        SubscribeLocalEvent<AudioEffectedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnAudioAdd(Entity<AudioComponent> ent, ref ComponentAdd args)
    {
        if (ent.Comp.Global)
            return;

        if (!_player.LocalEntity.HasValue)
            return;

        if (!IsAllowedToHear(ent, _player.LocalEntity.Value))
            return;

        if ((MetaData(ent).Flags & MetaDataFlags.Detached) != 0)
            return;

        AddComp<AudioEffectedComponent>(ent);
    }

    private void OnShutdown(Entity<AudioEffectedComponent> ent, ref ComponentShutdown args)
    {
        _effects.RemoveAllEffects(ent.Owner);
    }

    /// <summary>
    /// Checks if player is allowed to hear the sound.
    /// Uses a loop because [Access] attribute prevent using the .Contains() method.
    /// </summary>
    private bool IsAllowedToHear(Entity<AudioComponent> ent, EntityUid player)
    {
        if (ent.Comp.IncludedEntities == null || ent.Comp.IncludedEntities.Count == 0)
            return true;

        if (ent.Comp.ExcludedEntity == player)
            return false;

        foreach (var someEntity in ent.Comp.IncludedEntities)
        {
            if (someEntity == player)
                return true;
        }

        return false;
    }
}
