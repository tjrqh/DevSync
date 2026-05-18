using DevSync.Shared.Enums;

namespace DevSync.Shared.Dtos;

/// <summary>화면과 API가 주고받는 사용자 정보입니다.</summary>
public sealed record UserDto(int Id, string DisplayName, string Email);

/// <summary>회원가입 요청입니다. 비밀번호는 서버에서 해시 처리한 뒤 DB에 저장합니다.</summary>
public sealed record RegisterRequest(string DisplayName, string Email, string Password);

/// <summary>로그인 요청입니다. 서버는 평문 비밀번호를 저장하지 않고 저장된 해시와 비교합니다.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>로그인/회원가입 성공 시 화면에 반환하는 사용자 정보입니다.</summary>
public sealed record AuthResponse(int UserId, string DisplayName, string Email);

/// <summary>프로젝트 룸의 기본 정보와 초대 토큰입니다.</summary>
public sealed record RoomDto(int Id, string Name, string InviteToken, int OwnerUserId);

/// <summary>사용자가 참여 중이거나 승인 대기 중인 룸을 초기 화면에 보여주기 위한 DTO입니다.</summary>
public sealed record MyRoomDto(int Id, string Name, bool IsApproved, bool IsOwner);

/// <summary>초대 토큰 참여 요청 결과입니다.</summary>
public sealed record JoinRoomResultDto(int RoomId, string RoomName, bool IsApproved);

/// <summary>룸 멤버 정보를 화면에 표시하기 위한 DTO입니다.</summary>
public sealed record RoomMemberDto(int UserId, string DisplayName, bool IsApproved);

/// <summary>칸반 카드 하나를 표현하는 업무 DTO입니다.</summary>
public sealed record ProjectTaskDto(
    int Id,
    int RoomId,
    string Title,
    string? Description,
    string Priority,
    DateTime? DueDate,
    ProjectTaskStatus Status,
    int? AssigneeUserId,
    string? AssigneeName);

/// <summary>채팅 메시지와 시스템 알림을 동일한 목록에서 표시하기 위한 DTO입니다.</summary>
public sealed record ChatMessageDto(
    int Id,
    int RoomId,
    int? SenderUserId,
    string SenderName,
    string Content,
    bool IsSystem,
    DateTimeOffset CreatedAt,
    IReadOnlyList<int> MentionedUserIds);

/// <summary>헤더 알림함에 표시할 사용자 알림입니다.</summary>
public sealed record NotificationDto(
    int Id,
    int UserId,
    int RoomId,
    string RoomName,
    int? ChatMessageId,
    string Type,
    string Title,
    string Body,
    bool IsRead,
    DateTimeOffset CreatedAt);

/// <summary>새 프로젝트 룸 생성 요청입니다.</summary>
public sealed record CreateRoomRequest(string Name, int OwnerUserId);

/// <summary>초대 토큰으로 룸 입장을 신청하는 요청입니다.</summary>
public sealed record JoinRoomByTokenRequest(string InviteToken, int UserId);

/// <summary>방장이 사용자의 입장 신청을 승인하는 요청입니다.</summary>
public sealed record ApproveMemberRequest(int OwnerUserId, int UserIdToApprove);

/// <summary>새 업무 카드 생성 요청입니다.</summary>
public sealed record CreateProjectTaskRequest(
    string Title,
    string? Description,
    int? AssigneeUserId,
    string Priority,
    DateTime? DueDate,
    ProjectTaskStatus Status);

/// <summary>업무 상태 변경 요청입니다.</summary>
public sealed record UpdateTaskStatusRequest(int UserId, ProjectTaskStatus NewStatus);

/// <summary>채팅 메시지 전송 요청입니다.</summary>
public sealed record SendChatMessageRequest(int UserId, string Content);
