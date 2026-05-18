using DevSync.Api.Data;
using DevSync.Api.Services;
using DevSync.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace DevSync.Api.Controllers;

[ApiController]
[Route("api/rooms/{roomId:int}/messages")]
public sealed class ChatController(AppDbContext db, RoomAccessService roomAccess) : ControllerBase
{
    /// <summary>화면 최초 진입 시 최근 채팅 내역을 불러옵니다.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChatMessageDto>>> GetMessages(
        int roomId,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        if (!await roomAccess.IsApprovedMemberAsync(roomId, userId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "승인된 룸 멤버만 채팅 내역을 조회할 수 있습니다.");
        }

        var messageEntities = await db.ChatMessages
            .AsNoTracking()
            .Include(x => x.SenderUser)
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        var approvedMembers = await db.RoomMembers
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.RoomId == roomId && x.IsApproved)
            .Select(x => new MentionMember(x.UserId, x.User!.DisplayName))
            .ToListAsync(cancellationToken);

        return messageEntities
            .Select(message =>
            {
                var mentionedUserIds = FindMentionedUserIds(message.Content, approvedMembers);
                return ChatMessageMapper.ToDto(message, mentionedUserIds);
            })
            .ToList();
    }

    private static IReadOnlyList<int> FindMentionedUserIds(
        string content,
        IReadOnlyList<MentionMember> approvedMembers)
    {
        var mentionNames = Regex.Matches(content, @"@([^\s@]+)")
            .Select(x => x.Groups[1].Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return approvedMembers
            .Where(x => mentionNames.Contains((string)x.DisplayName))
            .Select(x => x.UserId)
            .ToList();
    }

    private sealed record MentionMember(int UserId, string DisplayName);
}
