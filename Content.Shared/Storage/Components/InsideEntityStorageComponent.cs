﻿using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

/// <summary>
///     Added to entities contained within entity storage, for directed event purposes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] // Fire edit
public sealed partial class InsideEntityStorageComponent : Component
{
    [AutoNetworkedField] // Fire added
    public EntityUid Storage;
}
