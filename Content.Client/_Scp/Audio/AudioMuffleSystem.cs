using Content.Shared._Scp.Audio;
using Content.Shared.Examine;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Audio;

public sealed class AudioMuffleSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AudioEffectsManagerSystem _effectsManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private static readonly ProtoId<AudioPresetPrototype> MuffingEffectPreset = "ScpBehindWalls";

    private const float ReducedVolume = -20f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Exists(_player.LocalEntity))
            return;

        var query = AllEntityQuery<AudioComponent>();

        while (query.MoveNext(out var uid, out var audio))
        {
            if (TerminatingOrDeleted(uid) || Paused(uid))
                continue;

            if (audio.Global)
                continue;

            if (!_examine.InRangeUnOccluded(uid, _player.LocalEntity.Value))
                TryMuffleSound((uid, audio));
            else
                TryUnMuffleSound((uid, audio));
        }
    }

    public bool TryMuffleSound(Entity<AudioComponent> ent)
    {
        if (AudioEffectsManagerSystem.HasEffect(ent, MuffingEffectPreset))
            return false;

        if (HasComp<AudioMuffledComponent>(ent))
            return false;

        var muffledComponent = EnsureComp<AudioMuffledComponent>(ent);
        muffledComponent.CachedVolume = ent.Comp.Volume;

        if (_effectsManager.TryGetEffect(ent, out var preset))
            muffledComponent.CachedPreset = preset;

        // Очищение лишних эффектов(например эхо)
        _effectsManager.RemoveAllEffects(ent);

        _effectsManager.TryAddEffect(ent, MuffingEffectPreset);
        _audio.SetVolume(ent, ent.Comp.Volume + ReducedVolume, ent);

        return true;
    }

    public bool TryUnMuffleSound(Entity<AudioComponent> ent, AudioMuffledComponent? muffledComponent = null)
    {
        if (!AudioEffectsManagerSystem.HasEffect(ent, MuffingEffectPreset))
            return false;

        if (!Resolve(ent.Owner, ref muffledComponent))
            return false;

        _effectsManager.TryRemoveEffect(ent, MuffingEffectPreset);

        if (muffledComponent.CachedPreset != null)
            _effectsManager.TryAddEffect(ent, muffledComponent.CachedPreset.Value);

        _audio.SetVolume(ent, muffledComponent.CachedVolume, ent);

        RemComp<AudioMuffledComponent>(ent);

        return true;
    }
}
