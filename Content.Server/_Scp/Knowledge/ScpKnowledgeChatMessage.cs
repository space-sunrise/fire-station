using Content.Shared.Chat;

namespace Content.Server._Scp.Knowledge;

public static class ScpKnowledgeChatMessage
{
    public static MsgChatMessage CloneWithWrappedMessage(MsgChatMessage message, string wrappedMessage)
    {
        var chat = message.Message;

        return new MsgChatMessage
        {
            Message = new ChatMessage(
                chat.Channel,
                chat.Message,
                wrappedMessage,
                chat.SenderEntity,
                chat.SenderKey,
                chat.HideChat,
                chat.MessageColorOverride,
                chat.AudioPath,
                chat.AudioVolume)
        };
    }
}
