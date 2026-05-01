namespace Content.Shared._Scp.Knowledge;

public sealed class ScpKnowledgePaperReadEvent(EntityUid paper, EntityUid user, string content) : EntityEventArgs
{
    public readonly EntityUid Paper = paper;
    public readonly EntityUid User = user;
    public readonly string Content = content;
}
