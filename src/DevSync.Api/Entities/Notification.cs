namespace DevSync.Api.Entities;

/// <summary>사용자별 헤더 알림함에 표시되는 알림입니다.</summary>
public sealed class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public int? ChatMessageId { get; set; }
    public ChatMessage? ChatMessage { get; set; }

    public string Type { get; set; } = "Mention";
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
