using DevSync.Api.Data;
using DevSync.Api.Entities;
using DevSync.Api.Services;
using DevSync.Shared.Dtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace DevSync.Api.Hubs;

/// <summary>
/// 프로젝트 룸 단위 실시간 채팅과 알림을 담당하는 SignalR Hub입니다.
/// SignalR Group 이름을 room-{RoomId}로 고정해서 같은 룸 사용자끼리만 메시지를 받게 합니다.
/// </summary>
public sealed class ProjectHub(AppDbContext db, RoomAccessService roomAccess) : Hub
{
    public static string GroupName(int roomId) => $"room-{roomId}";

    /// <summary>
    /// 클라이언트가 화면에 진입할 때 호출합니다.
    /// 승인된 멤버만 SignalR 그룹에 들어갈 수 있으므로 비공개 룸 요구사항을 만족합니다.
    /// </summary>
    public async Task JoinRoom(int roomId, int userId)
    {
        if (!await roomAccess.IsApprovedMemberAsync(roomId, userId))
        {
            throw new HubException("승인된 룸 멤버만 실시간 채팅방에 입장할 수 있습니다.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(roomId));
    }

    /// <summary>
    /// 슬랙처럼 사용자가 승인된 모든 룸의 멘션 알림을 받을 수 있도록 여러 그룹에 한 번에 참여합니다.
    /// </summary>
    public async Task JoinRooms(IEnumerable<int> roomIds, int userId)
    {
        foreach (var roomId in roomIds.Distinct())
        {
            if (await roomAccess.IsApprovedMemberAsync(roomId, userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(roomId));
            }
        }
    }

    /// <summary>화면을 나갈 때 SignalR 그룹에서 연결을 제거합니다.</summary>
    public Task LeaveRoom(int roomId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(roomId));
    }

    /// <summary>
    /// 사용자가 보낸 채팅을 DB에 저장한 뒤 같은 룸 그룹에 즉시 브로드캐스트합니다.
    /// </summary>
    public async Task SendMessage(int roomId, int userId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new HubException("빈 메시지는 보낼 수 없습니다.");
        }

        if (!await roomAccess.IsApprovedMemberAsync(roomId, userId))
        {
            throw new HubException("승인된 룸 멤버만 채팅을 보낼 수 있습니다.");
        }

        var user = await db.Users.FindAsync(userId)
            ?? throw new HubException("사용자를 찾을 수 없습니다.");

        var message = new ChatMessage
        {
            RoomId = roomId,
            SenderUserId = userId,
            SenderUser = user,
            Content = content.Trim(),
            IsSystem = false
        };

        db.ChatMessages.Add(message);
        await db.SaveChangesAsync();

        var mentionedUserIds = await FindMentionedUserIdsAsync(roomId, message.Content);
        var messageDto = ChatMessageMapper.ToDto(message, mentionedUserIds);

        if (mentionedUserIds.Count > 0)
        {
            var roomName = await db.Rooms
                .Where(x => x.Id == roomId)
                .Select(x => x.Name)
                .FirstAsync();

            foreach (var mentionedUserId in mentionedUserIds.Where(x => x != userId))
            {
                var notification = new Notification
                {
                    UserId = mentionedUserId,
                    RoomId = roomId,
                    ChatMessageId = message.Id,
                    Type = "Mention",
                    Title = $"{user.DisplayName}님이 나를 멘션했습니다.",
                    Body = TrimPreview(message.Content)
                };

                db.Notifications.Add(notification);
                await db.SaveChangesAsync();

                var notificationDto = new NotificationDto(
                    notification.Id,
                    notification.UserId,
                    notification.RoomId,
                    roomName,
                    notification.ChatMessageId,
                    notification.Type,
                    notification.Title,
                    notification.Body,
                    notification.IsRead,
                    notification.CreatedAt);

                await Clients.Group(GroupName(roomId)).SendAsync("ReceiveNotification", notificationDto);
            }
        }

        await Clients.Group(GroupName(roomId)).SendAsync("ReceiveMessage", messageDto);
    }

    /// <summary>
    /// 채팅 내용에서 @표시이름 패턴을 찾고, 해당 룸의 승인된 멤버 ID 목록으로 변환합니다.
    /// 클라이언트는 이 ID 목록으로 "내가 멘션됐는지"를 판단합니다.
    /// </summary>
    private async Task<IReadOnlyList<int>> FindMentionedUserIdsAsync(int roomId, string content)
    {
        var mentionNames = Regex.Matches(content, @"@([^\s@]+)")
            .Select(x => x.Groups[1].Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (mentionNames.Count == 0)
        {
            return [];
        }

        return await db.RoomMembers
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.RoomId == roomId && x.IsApproved && mentionNames.Contains(x.User!.DisplayName))
            .Select(x => x.UserId)
            .ToListAsync();
    }

    private static string TrimPreview(string content)
    {
        var preview = content.ReplaceLineEndings(" ").Trim();
        return preview.Length <= 120 ? preview : $"{preview[..117]}...";
    }
}
