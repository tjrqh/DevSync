namespace DevSync.Api.Entities;

/// <summary>
/// 시스템 사용자입니다.
/// 실제 서비스에서는 ASP.NET Core Identity 사용자로 교체할 수 있습니다.
/// </summary>
public sealed class User
{
    public int Id { get; set; }

    /// <summary>채팅 메시지와 업무 알림에 표시되는 이름입니다.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>로그인 ID로 사용하는 이메일입니다. 중복 가입을 막기 위해 DB에서 유니크 인덱스를 겁니다.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>평문 비밀번호가 아니라 PasswordHasher가 만든 해시 문자열만 저장합니다.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>사용자가 소유한 프로젝트 룸 목록입니다.</summary>
    public List<Room> OwnedRooms { get; set; } = [];

    /// <summary>사용자가 참여 신청 또는 승인된 룸 목록입니다.</summary>
    public List<RoomMember> RoomMembers { get; set; } = [];
}
