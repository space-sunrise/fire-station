using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Misc.AnnounceOnSpawn;

[RegisterComponent]
public sealed partial class ScpAnnounceOnSpawnComponent : Component
{
    [DataField(required: true)]
    public string Text;

    [DataField]
    public List<ProtoId<RadioChannelPrototype>>? Channels;

    [DataField]
    public bool UseGlobalAnnouncement;

    [DataField]
    public bool AnnounceOnArrival = true;

    [DataField]
    public bool AnnounceOnMapInit;

    [DataField]
    public bool AnnounceOnStationGridEntered;

    [DataField]
    public bool IncludeJobName;

    [ViewVariables]
    public bool Announced;

    [DataField]
    public SoundSpecifier? StationAnnouncementSound;

    [DataField]
    public SoundSpecifier? GlobalAnnouncementSound;

    [DataField]
    public Color? StationAnnouncementColor;

    [DataField]
    public string? StationAnnouncementVoice;
}
