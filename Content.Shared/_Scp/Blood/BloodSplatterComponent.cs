using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Blood;

/// <summary>
/// Компонент партикла крови, выдается самому партиклу.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodSplatterComponent : Component
{
    /// <summary>
    /// Название контейнера с реагентами внутри частицы.
    /// </summary>
    [DataField]
    public string SolutionName = "blood";

    /// <summary>
    /// Звук, проигрываемый при появлении частицы.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Шанс проигрывания звука при появлении партикла.
    /// </summary>
    [DataField]
    public float SoundProbability = 0.4f;

    [DataField]
    public TimeSpan LifeTime = TimeSpan.FromSeconds(5f);
}
