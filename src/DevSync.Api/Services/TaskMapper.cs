using DevSync.Api.Entities;
using DevSync.Shared.Dtos;

namespace DevSync.Api.Services;

/// <summary>ProjectTask 엔티티를 칸반 보드 표시용 DTO로 변환합니다.</summary>
public static class TaskMapper
{
    public static ProjectTaskDto ToDto(ProjectTask task)
    {
        return new ProjectTaskDto(
            task.Id,
            task.RoomId,
            task.Title,
            task.Description,
            task.Priority,
            task.DueDate,
            task.Status,
            task.AssigneeUserId,
            task.AssigneeUser?.DisplayName);
    }
}
