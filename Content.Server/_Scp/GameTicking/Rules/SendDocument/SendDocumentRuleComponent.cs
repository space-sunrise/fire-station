using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.GameTicking.Rules.SendDocument;

[RegisterComponent]
public sealed partial class SendDocumentRuleComponent : Component
{
    /// <summary>
    /// Отображаемое имя документа
    /// </summary>
    [DataField(required: true)]
    public string DocumentName;

    /// <summary>
    /// Кусочки текста, которые будут использованы в документе.
    /// Крепятся друг другу через новую строку
    /// </summary>
    [DataField(required: true)] public string[] TextParts;

    /// <summary>
    /// Прототип печати, который будет использован в документе.
    /// Можно не использовать печать в принципе.
    /// </summary>
    [DataField]
    public EntProtoId? StampProtoId;

    /// <summary>
    /// Требуется ли от факса иметь галочку ReceiveStationGoal?
    /// </summary>
    [DataField]
    public bool RequireReceiveStationGoal;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

}
