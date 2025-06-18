using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Mobs.Components;

/// <summary>
/// Специальный компонент, обозначающий СЦП.
/// Я специально не стал использовать ScpRestrictionComponent, так как он служит другой функции
/// Функции ограничения взаимодействий с СЦП. Не все СЦП могут его иметь
/// </summary>
// TODO: Сделать все компонентами сцп наследниками этого, возможно вынести сюда какой-нибудь бул отвечающий за контейм
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScpComponent : Component
{
    /// <summary>
    /// Последний раз, когда с СЦП взаимодействовали для получения исследовательского материала
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan? TimeLastInteracted;

    /// <summary>
    /// Класс SCP объекта
    /// </summary>
    [DataField(required: true)]
    public Classification Class;
}

public enum Classification
{
    Neutralized,
    Safe,
    Euclid,
    Keter,
}
