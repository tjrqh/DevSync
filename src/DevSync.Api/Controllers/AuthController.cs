using DevSync.Api.Services;
using DevSync.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DevSync.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>회원가입 API입니다. 성공하면 바로 로그인된 사용자 정보를 반환합니다.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await authService.RegisterAsync(request, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>로그인 API입니다. 이메일과 비밀번호가 맞으면 사용자 정보를 반환합니다.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await authService.LoginAsync(request, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}
