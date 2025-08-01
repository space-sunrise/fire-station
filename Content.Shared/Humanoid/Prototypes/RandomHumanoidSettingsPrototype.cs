using Content.Shared._Sunrise.TTS;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Humanoid.Prototypes;

/// <summary>
///     This is what is used to change a humanoid spawned by RandomHumanoidSystem in Content.Server.
/// </summary>
[Prototype]
public sealed partial class RandomHumanoidSettingsPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [ParentDataField(typeof(PrototypeIdArraySerializer<RandomHumanoidSettingsPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; private set; }

    /// <summary>
    ///     Whether the humanoid's name should take from the randomized profile or not.
    /// </summary>
    [DataField]
    public bool RandomizeName { get; private set; } = true;

    /// <summary>
    ///     Species that will be ignored by the randomizer.
    /// </summary>
    [DataField("speciesBlacklist")]
    public HashSet<string> SpeciesBlacklist { get; private set; } = new();

    // Fire edit start
    [DataField]
    public List<ProtoId<TTSVoicePrototype>>? VoiceWhitelist = [];

    [DataField]
    public List<Gender>? GenderWhitelist = [];

    /// <summary>
    ///     Extra components to add to this entity.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry? Components { get; private set; }
}
