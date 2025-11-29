using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp096.Main.Components;

/// <summary>
/// Компонент, отвечающий за активное состояние ярости скромника.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ActiveScp096RageComponent : Component
{
    /// <summary>
    /// Длительность состояния, в течение которого скромник находится в нем.
    /// После окончания состояние снимает и скромник переходит в спокойное состояние.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RageDuration = TimeSpan.FromMinutes(4f);

    /// <summary>
    /// Время сна, в который впадает скромник, после перехода в спокойное состояние
    /// </summary>
    [DataField]
    public TimeSpan PacifiedTime = TimeSpan.FromSeconds(60f);

    /// <summary>
    /// Время начала состояния ярости
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan? RageStartTime;

    /// <summary>
    /// Скорость скромника в состоянии ярости
    /// </summary>
    [DataField]
    public float Speed = 8f;

    /// <summary>
    /// Звук, издаваемый скромником в состоянии ярости
    /// </summary>
    [DataField]
    public SoundSpecifier RageSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/scream.ogg",
        AudioParams.Default.WithVolume(20f).WithMaxDistance(30f).WithRolloffFactor(5f).WithLoop(true));
}
