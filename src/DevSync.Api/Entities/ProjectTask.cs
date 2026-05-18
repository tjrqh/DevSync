using DevSync.Shared.Enums;

namespace DevSync.Api.Entities;

/// <summary>프로젝트 룸 안에서 관리되는 지라 스타일 업무 카드입니다.</summary>
public sealed class ProjectTask
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Priority { get; set; } = "Medium";
    public DateTime? DueDate { get; set; }

    /// <summary>Todo, InProgress, Done 중 하나의 칸반 상태입니다.</summary>
    public ProjectTaskStatus Status { get; set; } = ProjectTaskStatus.Todo;

    /// <summary>담당자가 없을 수 있으므로 nullable로 둡니다.</summary>
    public int? AssigneeUserId { get; set; }
    public User? AssigneeUser { get; set; }
}
