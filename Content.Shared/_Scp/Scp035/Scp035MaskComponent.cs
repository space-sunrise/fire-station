using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp035;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp035MaskComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? User;

    [AutoNetworkedField]
    [DataField]
    public TimeSpan NextMessageDelay = TimeSpan.FromSeconds(60);

    [AutoNetworkedField]
    [DataField]
    public List<LocId> Messages = new();

    [AutoNetworkedField]
    [DataField]
    public string ReagentName = "PoisonWine";

    [AutoNetworkedField]
    [DataField]
    public float ReagentRangeAvailable = 5;

    [AutoNetworkedField]
    [DataField]
    public int ReagentDestructLevel = 200;

    [AutoNetworkedField]
    [DataField]
    public TimeSpan NextLiquidSpawnDelay = TimeSpan.FromSeconds(60);

    [AutoNetworkedField]
    [DataField]
    public Vector2 CorrosionBox = new (6f, 3f);

    [AutoNetworkedField]
    [DataField]
    public int MaxTilesCorrosionPry = 10;

    [AutoNetworkedField]
    [DataField]
    public float EntityCorrosionRange = 3f;

    [AutoNetworkedField]
    [DataField]
    public DamageSpecifier DamageSpecif = new()
    {
        DamageDict = new() { { "Structural", 60f } }
    };

    [AutoNetworkedField]
    [DataField]
    public EntityWhitelist TargetStructures;

    [AutoNetworkedField]
    [DataField]
    public SoundSpecifier EquipSound = new SoundCollectionSpecifier("EquipScp035");

    [AutoNetworkedField]
    [DataField]
    public TimeSpan EquippedParalyzeDuration = TimeSpan.FromSeconds(5);

    [AutoNetworkedField]
    [DataField]
    public TimeSpan EquippeAttemptParalyzeDuration = TimeSpan.FromSeconds(10);

    [AutoNetworkedField]
    [DataField]
    public EntProtoId SpawnWeaponProto = "Chainsaw";

    [AutoNetworkedField]
    [DataField]
    public EntProtoId NewUserFaction = "SimpleHostile";

    [AutoNetworkedField]
    [DataField]
    public FixedPoint2 NewCriticalThreshold = FixedPoint2.New(800);

    [AutoNetworkedField]
    [DataField]
    public FixedPoint2 NewDeadThreshold = FixedPoint2.New(800);

    [AutoNetworkedField]
    [DataField]
    public float ImpulseModificator = 10000;

    public TimeSpan NextMessaging = TimeSpan.Zero;
    public TimeSpan NextLiquidSpawning = TimeSpan.Zero;

    public EntProtoId ActionRaiseArmy = "ActionScp035RaiseArmy";
    public EntProtoId ActionOrderStay = "ActionScp035OrderStay";
    public EntProtoId ActionOrderFollow = "ActionScp035OrderFollow";
    public EntProtoId ActionOrderKill = "ActionScp035OrderKill";
    public EntProtoId ActionOrderLoose = "ActionScp035OrderLoose";
    public EntProtoId ActionStun = "ActionScp035Stun";
}
