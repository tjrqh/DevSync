using DevSync.Api.Data;
using DevSync.Api.Entities;
using DevSync.Api.Hubs;
using DevSync.Api.Services;
using DevSync.Shared.Dtos;
using DevSync.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DevSync.Api.Controllers;

[ApiController]
[Route("api/rooms/{roomId:int}/tasks")]
public sealed class TasksController(
    AppDbContext db,
    RoomAccessService roomAccess,
    IHubContext<ProjectHub> hub) : ControllerBase
{
    /// <summary>룸 안의 모든 업무 카드를 조회합니다.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectTaskDto>>> GetTasks(
        int roomId,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        if (!await roomAccess.IsApprovedMemberAsync(roomId, userId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "승인된 룸 멤버만 업무를 조회할 수 있습니다.");
        }

        var taskEntities = await db.ProjectTasks
            .AsNoTracking()
            .Include(x => x.AssigneeUser)
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return taskEntities.Select(TaskMapper.ToDto).ToList();
    }

    /// <summary>새 업무 카드를 Todo 상태로 생성합니다.</summary>
    [HttpPost]
    public async Task<ActionResult<ProjectTaskDto>> CreateTask(
        int roomId,
        [FromQuery] int userId,
        CreateProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!await roomAccess.IsApprovedMemberAsync(roomId, userId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "승인된 룸 멤버만 업무를 생성할 수 있습니다.");
        }

        var task = new ProjectTask
        {
            RoomId = roomId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = NormalizePriority(request.Priority),
            DueDate = request.DueDate,
            AssigneeUserId = request.AssigneeUserId,
            Status = request.Status
        };

        db.ProjectTasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        var saved = await db.ProjectTasks.Include(x => x.AssigneeUser).FirstAsync(x => x.Id == task.Id, cancellationToken);
        var dto = TaskMapper.ToDto(saved);

        await hub.Clients.Group(ProjectHub.GroupName(roomId)).SendAsync("TaskChanged", dto, cancellationToken);
        return CreatedAtAction(nameof(GetTasks), new { roomId, userId }, dto);
    }

    /// <summary>
    /// 업무 상태를 변경하고, 같은 룸의 채팅방에 시스템 알림 메시지를 전송합니다.
    /// 예: [홍길동]님이 '로그인 기능'을 완료했습니다.
    /// </summary>
    [HttpPatch("{taskId:int}/status")]
    public async Task<ActionResult<ProjectTaskDto>> UpdateStatus(
        int roomId,
        int taskId,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!await roomAccess.IsApprovedMemberAsync(roomId, request.UserId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "승인된 룸 멤버만 업무 상태를 변경할 수 있습니다.");
        }

        var task = await db.ProjectTasks
            .Include(x => x.AssigneeUser)
            .FirstOrDefaultAsync(x => x.RoomId == roomId && x.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound("업무 카드를 찾을 수 없습니다.");
        }

        var user = await db.Users.FindAsync([request.UserId], cancellationToken);
        if (user is null)
        {
            return BadRequest("상태를 변경한 사용자를 찾을 수 없습니다.");
        }

        task.Status = request.NewStatus;

        var notification = new ChatMessage
        {
            RoomId = roomId,
            SenderUserId = null,
            Content = BuildStatusMessage(user.DisplayName, task.Title, request.NewStatus),
            IsSystem = true
        };

        db.ChatMessages.Add(notification);
        await db.SaveChangesAsync(cancellationToken);

        var taskDto = TaskMapper.ToDto(task);
        var messageDto = ChatMessageMapper.ToDto(notification);

        await hub.Clients.Group(ProjectHub.GroupName(roomId)).SendAsync("TaskChanged", taskDto, cancellationToken);
        await hub.Clients.Group(ProjectHub.GroupName(roomId)).SendAsync("ReceiveMessage", messageDto, cancellationToken);

        return taskDto;
    }

    private static string BuildStatusMessage(string displayName, string taskTitle, ProjectTaskStatus status)
    {
        var action = status switch
        {
            ProjectTaskStatus.Todo => "다시 할 일로 이동했습니다",
            ProjectTaskStatus.InProgress => "진행을 시작했습니다",
            ProjectTaskStatus.Done => "완료했습니다",
            _ => "상태를 변경했습니다"
        };

        return $"[{displayName}]님이 '{taskTitle}'을(를) {action}";
    }

    private static string NormalizePriority(string? priority)
    {
        return priority?.Trim() switch
        {
            "High" or "높음" => "High",
            "Low" or "낮음" => "Low",
            _ => "Medium"
        };
    }
}
