using DevSync.Api.Data;
using DevSync.Api.Entities;
using DevSync.Shared.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DevSync.Api.Services;

/// <summary>
/// 회원가입과 로그인을 담당하는 서비스입니다.
/// 컨트롤러는 HTTP 요청/응답만 다루고, 실제 인증 규칙은 이 서비스에 모읍니다.
/// </summary>
public sealed class AuthService(AppDbContext db, IPasswordHasher<User> passwordHasher)
{
    /// <summary>
    /// 회원가입 처리입니다.
    /// 비밀번호는 절대 평문으로 저장하지 않고 PasswordHasher가 만든 해시만 DB에 저장합니다.
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var displayName = request.DisplayName.Trim();
        var email = NormalizeEmail(request.Email);

        if (displayName.Length is < 2 or > 50)
        {
            throw new InvalidOperationException("이름은 2자 이상 50자 이하로 입력해야 합니다.");
        }

        if (email.Length is < 5 or > 120 || !email.Contains('@'))
        {
            throw new InvalidOperationException("올바른 이메일을 입력해야 합니다.");
        }

        if (request.Password.Length < 8)
        {
            throw new InvalidOperationException("비밀번호는 8자 이상이어야 합니다.");
        }

        var exists = await db.Users.AnyAsync(x => x.Email == email, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("이미 가입된 이메일입니다.");
        }

        var user = new User
        {
            DisplayName = displayName,
            Email = email
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        // 학원 과제 데모 사용성을 위해 첫 번째 기본 룸에는 가입 즉시 승인 멤버로 넣습니다.
        // 실제 서비스에서는 이 자동 가입 대신 초대 토큰 + 방장 승인 흐름만 사용하면 됩니다.
        var defaultRoom = await db.Rooms.OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);
        if (defaultRoom is not null)
        {
            db.RoomMembers.Add(new RoomMember
            {
                RoomId = defaultRoom.Id,
                UserId = user.Id,
                IsApproved = true,
                ApprovedAt = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
        }

        return new AuthResponse(user.Id, user.DisplayName, user.Email);
    }

    /// <summary>
    /// 로그인 처리입니다.
    /// 입력 비밀번호를 다시 해시해서 단순 비교하지 않고, PasswordHasher의 검증 API를 사용합니다.
    /// </summary>
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("이메일 또는 비밀번호가 올바르지 않습니다.");
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new InvalidOperationException("이메일 또는 비밀번호가 올바르지 않습니다.");
        }

        return new AuthResponse(user.Id, user.DisplayName, user.Email);
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
