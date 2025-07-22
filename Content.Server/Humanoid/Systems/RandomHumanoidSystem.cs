using Content.Server.Humanoid.Components;
using Content.Server.RandomMetadata;
using Content.Server.Station.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Humanoid.Systems;

/// <summary>
///     This deals with spawning and setting up random humanoids.
/// </summary>
public sealed class RandomHumanoidSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RandomHumanoidSpawnerComponent, MapInitEvent>(OnMapInit,
            after: new []{ typeof(RandomMetadataSystem) });
    }

    private void OnMapInit(EntityUid uid, RandomHumanoidSpawnerComponent component, MapInitEvent args)
    {
        QueueDel(uid);
        if (component.SettingsPrototypeId != null)
            SpawnRandomHumanoid(component.SettingsPrototypeId, Transform(uid).Coordinates, MetaData(uid).EntityName);
    }

    public EntityUid SpawnRandomHumanoid(string prototypeId, EntityCoordinates coordinates, string name)
    {
        if (!_prototypeManager.TryIndex<RandomHumanoidSettingsPrototype>(prototypeId, out var prototype))
            throw new ArgumentException("Could not get random humanoid settings");

        var profile = HumanoidCharacterProfile.Random(prototype.SpeciesBlacklist);
        var speciesProto = _prototypeManager.Index<SpeciesPrototype>(profile.Species);
        var humanoid = EntityManager.CreateEntityUninitialized(speciesProto.Prototype, coordinates);

        _metaData.SetEntityName(humanoid, prototype.RandomizeName ? profile.Name : name);

        // Fire added start - для настройки МОГа. И фиксанул баг с порядком загрузки хуман профайлов
        if (prototype.Components != null)
        {
            foreach (var entry in prototype.Components.Values)
            {
                var comp = (Component)_serialization.CreateCopy(entry.Component, notNullableOverride: true);
                EntityManager.RemoveComponent(humanoid, comp.GetType());
                EntityManager.AddComponent(humanoid, comp);
            }
        }

        EntityManager.InitializeAndStartEntity(humanoid);


        if (prototype.VoiceWhitelist != null)
            profile = profile.WithVoice(_random.Pick(prototype.VoiceWhitelist));

        if (prototype.GenderWhitelist != null)
            profile = profile.WithGender(_random.Pick(prototype.GenderWhitelist));
        // Fire added end

        _humanoid.LoadProfile(humanoid, profile);

        return humanoid;
    }
}
