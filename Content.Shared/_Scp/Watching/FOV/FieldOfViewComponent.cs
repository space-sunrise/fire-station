﻿using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Watching.FOV;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class FieldOfViewComponent : Component
{
    public const float MaxOpacity = 0.95f;
    public const float MinOpacity = 0.55f;

    public const float MaxBlurScale = 1f;
    public const float MinBlurScale = 0.25f;

    public const float MaxCooldownCheck = 0.3f;
    public const float MinCooldownCheck = 0.05f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Angle = 180f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float AngleTolerance = 14f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float ConeOpacity = 0.85f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Vector2 Offset = new(0, 0.5f);

    [ViewVariables, AutoNetworkedField]
    public EntityUid? RelayEntity;
}
