namespace DevSync.Api.Entities;

/// <summary>
/// User와 Room 사이의 다대다 관계를 표현하는 조인 테이블입니다.
/// IsApproved가 true일 때만 룸 입장, 채팅, 업무 조작이 가능합니다.
/// </summary>
public sealed class RoomMember
{
    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    /// <summary>방장이 초대 신청을 승인했는지 여부입니다.</summary>
    public bool IsApproved { get; set; }

    /// <summary>초대 토큰으로 참여 신청한 시각입니다.</summary>
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>방장이 승인한 시각입니다. 승인 전에는 null입니다.</summary>
    public DateTimeOffset? ApprovedAt { get; set; }
}
