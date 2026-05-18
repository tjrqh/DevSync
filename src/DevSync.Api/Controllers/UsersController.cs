using DevSync.Api.Data;
using DevSync.Api.Entities;
using DevSync.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevSync.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(AppDbContext db) : ControllerBase
{
    /// <summary>실습용 사용자 목록을 조회합니다.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<UserDto>> GetUsers(CancellationToken cancellationToken)
    {
        return await db.Users
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new UserDto(x.Id, x.DisplayName, x.Email))
            .ToListAsync(cancellationToken);
    }

    /// <summary>실습용 사용자 생성 API입니다. 실제 가입은 api/auth/register를 사용합니다.</summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(UserDto request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            DisplayName = request.DisplayName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = "created-without-login"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return new UserDto(user.Id, user.DisplayName, user.Email);
    }
}
