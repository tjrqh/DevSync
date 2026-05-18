namespace DevSync.Api.Entities;

/// <summary>지라 보드와 채팅방을 하나로 묶는 프로젝트 룸입니다.</summary>
public sealed class Room
{
    public int Id { get; set; }

    /// <summary>프로젝트 룸 이름입니다.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>방장이 공유하는 비공개 초대 토큰입니다.</summary>
    public string InviteToken { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>방장 사용자 ID입니다.</summary>
    public int OwnerUserId { get; set; }

    /// <summary>방장 사용자 탐색 속성입니다.</summary>
    public User? OwnerUser { get; set; }

    public List<RoomMember> Members { get; set; } = [];
    public List<ProjectTask> Tasks { get; set; } = [];
    public List<ChatMessage> ChatMessages { get; set; } = [];
}
