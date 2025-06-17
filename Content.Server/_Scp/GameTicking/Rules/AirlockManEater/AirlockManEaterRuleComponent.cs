namespace Content.Server._Scp.GameTicking.Rules.AirlockManEater;

[RegisterComponent]
public sealed partial class AirlockManEaterRuleComponent : Component
{
    [DataField] public float Percentage = 100f; // TODO: Убрать дебаг значение
}
