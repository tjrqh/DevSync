namespace DevSync.Api.Entities;

/// <summary>사용자 채팅과 시스템 알림을 모두 저장하는 메시지 엔티티입니다.</summary>
public sealed class ChatMessage
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room? Room { get; set; }

    /// <summary>시스템 알림은 특정 발신자가 없을 수 있으므로 nullable입니다.</summary>
    public int? SenderUserId { get; set; }
    public User? SenderUser { get; set; }

    public string Content { get; set; } = string.Empty;

    /// <summary>true이면 업무 상태 변경 등 서버가 만든 알림 메시지입니다.</summary>
    public bool IsSystem { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
