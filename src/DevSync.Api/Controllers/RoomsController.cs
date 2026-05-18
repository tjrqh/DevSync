using DevSync.Api.Data;
using DevSync.Api.Entities;
using DevSync.Api.Services;
using DevSync.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevSync.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public sealed class RoomsController(AppDbContext db, RoomAccessService roomAccess) : ControllerBase
{
    /// <summary>로그인 후 초기 화면에서 사용자가 참여 중인 룸과 승인 대기 룸을 조회합니다.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MyRoomDto>>> GetMyRooms([FromQuery] int userId, CancellationToken cancellationToken)
    {
        var userExists = await db.Users.AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
        {
            return BadRequest("사용자를 찾을 수 없습니다.");
        }

        var rooms = await db.RoomMembers
            .AsNoTracking()
            .Include(x => x.Room)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsApproved)
            .ThenBy(x => x.Room!.Name)
            .Select(x => new MyRoomDto(
                x.RoomId,
                x.Room!.Name,
                x.IsApproved,
                x.Room.OwnerUserId == userId))
            .ToListAsync(cancellationToken);

        return rooms;
    }

    /// <summary>프로젝트 룸을 생성하고 방장을 즉시 승인된 멤버로 등록합니다.</summary>
    [HttpPost]
    public async Task<ActionResult<RoomDto>> CreateRoom(CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var owner = await db.Users.FindAsync([request.OwnerUserId], cancellationToken);
        if (owner is null)
        {
            return BadRequest("방장 사용자를 찾을 수 없습니다.");
        }

        var room = new Room
        {
            Name = request.Name.Trim(),
            OwnerUserId = owner.Id,
            InviteToken = Guid.NewGuid().ToString("N")
        };

        room.Members.Add(new RoomMember
        {
            Room = room,
            UserId = owner.Id,
            IsApproved = true,
            ApprovedAt = DateTimeOffset.UtcNow
        });

        db.Rooms.Add(room);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetRoom), new { roomId = room.Id, userId = owner.Id },
            new RoomDto(room.Id, room.Name, room.InviteToken, room.OwnerUserId));
    }

    /// <summary>승인된 멤버만 룸 정보를 조회할 수 있습니다.</summary>
    [HttpGet("{roomId:int}")]
    public async Task<ActionResult<RoomDto>> GetRoom(int roomId, [FromQuery] int userId, CancellationToken cancellationToken)
    {
        if (!await roomAccess.IsApprovedMemberAsync(roomId, userId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "승인된 룸 멤버만 접근할 수 있습니다.");
        }

        var room = await db.Rooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == roomId, cancellationToken);
        return room is null
            ? NotFound()
            : new RoomDto(room.Id, room.Name, room.InviteToken, room.OwnerUserId);
    }

    /// <summary>
    /// 초대 토큰이 맞으면 RoomMember에 참여 신청을 생성합니다.
    /// 단, 방장의 승인 전까지 IsApproved=false라서 실제 입장은 불가능합니다.
    /// </summary>
    [HttpPost("join-by-token")]
    public async Task<ActionResult<JoinRoomResultDto>> JoinByToken(JoinRoomByTokenRequest request, CancellationToken cancellationToken)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(x => x.InviteToken == request.InviteToken, cancellationToken);
        if (room is null)
        {
            return NotFound("초대 토큰과 일치하는 룸이 없습니다.");
        }

        var userExists = await db.Users.AnyAsync(x => x.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            return BadRequest("사용자를 찾을 수 없습니다.");
        }

        var member = await db.RoomMembers.FindAsync([room.Id, request.UserId], cancellationToken);
        if (member is not null)
        {
            return new JoinRoomResultDto(room.Id, room.Name, member.IsApproved);
        }

        db.RoomMembers.Add(new RoomMember
        {
            RoomId = room.Id,
            UserId = request.UserId,
            IsApproved = false
        });

        await db.SaveChangesAsync(cancellationToken);
        return new JoinRoomResultDto(room.Id, room.Name, false);
    }

    /// <summary>방장이 특정 사용자의 룸 입장을 승인합니다.</summary>
    [HttpPost("{roomId:int}/approve")]
    public async Task<ActionResult> ApproveMember(int roomId, ApproveMemberRequest request, CancellationToken cancellationToken)
    {
        if (!await roomAccess.IsOwnerAsync(roomId, request.OwnerUserId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "방장만 멤버를 승인할 수 있습니다.");
        }

        var member = await db.RoomMembers.FindAsync([roomId, request.UserIdToApprove], cancellationToken);
        if (member is null)
        {
            return NotFound("승인할 참여 신청을 찾을 수 없습니다.");
        }

        member.IsApproved = true;
        member.ApprovedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>사용자가 룸 멤버십에서 완전히 나갑니다. 방장은 룸 소유권 때문에 탈퇴할 수 없습니다.</summary>
    [HttpDelete("{roomId:int}/members/{userId:int}")]
    public async Task<ActionResult> LeaveMembership(
        int roomId,
        int userId,
        [FromQuery] int requesterUserId,
        CancellationToken cancellationToken)
    {
        if (requesterUserId != userId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "본인만 룸에서 나갈 수 있습니다.");
        }

        var room = await db.Rooms.FirstOrDefaultAsync(x => x.Id == roomId, cancellationToken);
        if (room is null)
        {
            return NotFound("룸을 찾을 수 없습니다.");
        }

        if (room.OwnerUserId == userId)
        {
            return BadRequest("방장은 룸을 나갈 수 없습니다. 방장 위임 또는 룸 삭제 기능이 필요합니다.");
        }

        var member = await db.RoomMembers.FindAsync([roomId, userId], cancellationToken);
        if (member is null)
        {
            return NotFound("룸 멤버십을 찾을 수 없습니다.");
        }

        db.RoomMembers.Remove(member);

        var notifications = await db.Notifications
            .Where(x => x.RoomId == roomId && x.UserId == userId)
            .ToListAsync(cancellationToken);
        db.Notifications.RemoveRange(notifications);

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>룸 멤버 목록을 조회합니다. 방장 화면에서 승인 대기자를 보여줄 때 사용합니다.</summary>
    [HttpGet("{roomId:int}/members")]
    public async Task<ActionResult<IReadOnlyList<RoomMemberDto>>> GetMembers(
        int roomId,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        if (!await roomAccess.IsApprovedMemberAsync(roomId, userId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "승인된 룸 멤버만 멤버 목록을 조회할 수 있습니다.");
        }

        var members = await db.RoomMembers
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.RoomId == roomId)
            .Select(x => new RoomMemberDto(x.UserId, x.User!.DisplayName, x.IsApproved))
            .ToListAsync(cancellationToken);

        return members;
    }
}
