using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Blood;

/// <summary>
/// Компонент партикла крови, который разлетается от удара по персонажу.
/// Выдается самому партиклу
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodParticleComponent : Component
{
    /// <summary>
    /// Название контейнера с реагентами внутри частицы.
    /// </summary>
    [DataField]
    public string SolutionName = "blood";

    /// <summary>
    /// Лужицы крови, которые будут оставаться на месте падения партикла крови.
    /// </summary>
    [DataField]
    public HashSet<EntProtoId> BloodEntities =
    [
        "BloodSplatter1",
        "BloodSplatter2",
        "BloodSplatter3",
        "BloodSplatter4",
        "BloodSplatter5",
        "BloodSplatter6",
    ];

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

    /// <summary>
    /// Скорость полета частиц (min, max).
    /// </summary>
    [DataField]
    public Vector2 Distance = new(5f, 20f);

    [DataField]
    public TimeSpan FlyTime = TimeSpan.FromSeconds(1f);

    [DataField]
    public int MoveTimes = 40;

    [ViewVariables]
    public Vector2? Velocity;

    [ViewVariables]
    public Vector2 Speed = Vector2.Zero;

    [ViewVariables]
    public TimeSpan MoveCooldown;

    [ViewVariables]
    public TimeSpan NextMoveTime = TimeSpan.Zero;
}
