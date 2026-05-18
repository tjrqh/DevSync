using DevSync.Api.Entities;
using DevSync.Shared.Dtos;

namespace DevSync.Api.Services;

/// <summary>ChatMessage 엔티티를 프론트엔드용 DTO로 변환하는 공통 매퍼입니다.</summary>
public static class ChatMessageMapper
{
    public static ChatMessageDto ToDto(ChatMessage message, IReadOnlyList<int>? mentionedUserIds = null)
    {
        return new ChatMessageDto(
            message.Id,
            message.RoomId,
            message.SenderUserId,
            message.SenderUser?.DisplayName ?? "System",
            message.Content,
            message.IsSystem,
            message.CreatedAt,
            mentionedUserIds ?? []);
    }
}
