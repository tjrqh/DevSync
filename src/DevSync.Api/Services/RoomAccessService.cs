using DevSync.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DevSync.Api.Services;

/// <summary>
/// 룸 접근 권한 검증을 한 곳에 모아두는 서비스입니다.
/// 컨트롤러와 SignalR Hub가 같은 규칙을 사용하므로 권한 로직 중복을 줄일 수 있습니다.
/// </summary>
public sealed class RoomAccessService(AppDbContext db)
{
    /// <summary>사용자가 승인된 룸 멤버인지 확인합니다.</summary>
    public Task<bool> IsApprovedMemberAsync(int roomId, int userId, CancellationToken cancellationToken = default)
    {
        return db.RoomMembers.AnyAsync(
            x => x.RoomId == roomId && x.UserId == userId && x.IsApproved,
            cancellationToken);
    }

    /// <summary>현재 사용자가 해당 룸의 방장인지 확인합니다.</summary>
    public Task<bool> IsOwnerAsync(int roomId, int userId, CancellationToken cancellationToken = default)
    {
        return db.Rooms.AnyAsync(
            x => x.Id == roomId && x.OwnerUserId == userId,
            cancellationToken);
    }
}
