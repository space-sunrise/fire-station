using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp096.Main.Components;

/// <summary>
/// Компонент, отвечающий за пред-агр состояние скромника.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class ActiveScp096HeatingUpComponent : Component
{
    /// <summary>
    /// Время окончания состояния, после которого скромник переходит в режим ярости.
    /// </summary>
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? RageHeatUpEnd;

    /// <summary>
    /// Длительность состояния, после которого скромник переходит в режим ярости.
    /// </summary>
    [DataField]
    public TimeSpan RageHeatUp = TimeSpan.FromSeconds(30f);

    /// <summary>
    /// Звук, проигрывающийся скромником, после перехода в состояние.
    /// </summary>
    [DataField]
    public SoundSpecifier TriggerSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/triggered.ogg",
        AudioParams.Default.WithVolume(20f).WithMaxDistance(30f).WithRolloffFactor(5f));

    /// <summary>
    /// Минимальная интенсивность оверлея. Выставляется в начале перехода в состояние
    /// </summary>
    [DataField]
    public double OverlayIntensityMin = 0d;

    /// <summary>
    /// Максимальная интенсивность оверлея.
    /// Выставляется в пиковой точке, когда время становится равным <see cref="RageHeatUpEnd"/>
    /// </summary>
    [DataField]
    public double OverlayIntensityMax = 1d;
}
