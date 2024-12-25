using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp096.Mask;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp096MaskComponent : Component
{
    /// <summary>
    /// Время, которое сцп 096 должен рвать свою маску
    /// </summary>
    [DataField]
    public float TearTime = 60f;

    #region Safe time

    [DataField]
    public float SafeTime = 600f;

    [AutoNetworkedField]
    public TimeSpan? SafeTimeEnd;

    #endregion

    #region Sounds

    [DataField]
    public SoundSpecifier? EquipSound;

    [DataField]
    public SoundSpecifier? TearSound;

    // TODO: Постоянный звук разрывания маски, которые будет проигрываться на протяжении всего процесса

    #endregion

}
