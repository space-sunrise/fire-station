using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp096.Main.Components;

/// <summary>
/// Компонент-маркер, отвечающий за шейдер "тусклости" в состоянии содранного лица у скромника.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class Scp096ShaderWithoutFaceComponent : Component;
