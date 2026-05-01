#pragma warning disable IDE0130 // Namespace does not match folder structure
using Content.Server._Scp.Knowledge;
using Content.Shared.Chat;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class HeadsetSystem
{
    [Dependency] private readonly ScpKnowledgeSystem _knowledge = default!;

    private MsgChatMessage GetScpKnowledgeRadioChatMessage(EntityUid listener, EntityUid source, string message, MsgChatMessage chatMessage)
    {
        var highlightedWrappedMessage = _knowledge.HighlightWrappedChatMessage(
            listener,
            message,
            chatMessage.Message.WrappedMessage,
            source);

        if (string.Equals(highlightedWrappedMessage, chatMessage.Message.WrappedMessage, StringComparison.Ordinal))
            return chatMessage;

        return ScpKnowledgeChatMessage.CloneWithWrappedMessage(chatMessage, highlightedWrappedMessage);
    }
}
