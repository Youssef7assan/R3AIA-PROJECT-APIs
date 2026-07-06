using Microsoft.AspNetCore.Mvc;
using R3AIA.DTOs;
using R3AIA.Repositories;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly IAccountRepository _accountRepository;

	public AuthController(IAccountRepository accountRepository)
	{
		_accountRepository = accountRepository;
	}

	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var repo = _accountRepository as AccountRepository;
		var result = await _accountRepository.RegisterAsync(dto);
		if (result is null)
		{
			var errors = repo?.Errors ?? new[] { "Registration failed" };
			return BadRequest(new { errors });
		}

		return Ok(result);
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var result = await _accountRepository.LoginAsync(dto);
		if (result is null) return Unauthorized();

		return Ok(result);
	}

	[HttpPost("change-password")]
	[Microsoft.AspNetCore.Authorization.Authorize]
	public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
					 ?? User.FindFirst("sub")?.Value;
		if (userId == null) return Unauthorized();

		var repo = _accountRepository as AccountRepository;
		var success = await _accountRepository.ChangePasswordAsync(userId, dto);
		if (!success)
		{
			var errors = repo?.Errors ?? new[] { "Failed to change password" };
			return BadRequest(new { errors });
		}

		return Ok(new { message = "Password changed successfully" });
	}
	[HttpPost("reset-password")]
	public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var repo = _accountRepository as AccountRepository;
		var success = await _accountRepository.ResetPasswordAsync(dto);
		if (!success)
		{
			var errors = repo?.Errors ?? new[] { "Failed to reset password" };
			return BadRequest(new { errors });
		}

		return Ok(new { message = "تم تغيير كلمة المرور بنجاح" });
	}
}
