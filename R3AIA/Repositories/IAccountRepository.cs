using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;
using R3AIA.Services;
using static R3AIA.Models.Enums;

namespace R3AIA.Repositories;

public interface IAccountRepository
{
	Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
	Task<AuthResponseDto?> LoginAsync(LoginDto dto);
	Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto dto);
	Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
}

public class AccountRepository : IAccountRepository
{
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly SignInManager<ApplicationUser> _signInManager;
	private readonly IJwtService _jwtService;

	public string[] Errors { get; set; } = Array.Empty<string>();

	public AccountRepository(
		UserManager<ApplicationUser> userManager,
		SignInManager<ApplicationUser> signInManager,
		IJwtService jwtService)
	{
		_userManager = userManager;
		_signInManager = signInManager;
		_jwtService = jwtService;
	}

	public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
	{
		// منع التسجيل لو الرقم القومي محظور مسبقاً
		var bannedUser = await _userManager.Users
			.FirstOrDefaultAsync(u => u.NationalID == dto.NationalID && u.AccountStatus == Enums.AccountStatus.Banned);
		if (bannedUser != null)
		{
			Errors = new[] { "This National ID is banned." };
			return null;
		}

		// التحقق من أن الرقم القومي غير مسجل مسبقاً
		var existingUserByNationalId = await _userManager.Users
			.FirstOrDefaultAsync(u => u.NationalID == dto.NationalID);
		if (existingUserByNationalId != null)
		{
			if (existingUserByNationalId.AccountStatus == Enums.AccountStatus.Pending)
			{
				Errors = new[] { "This account is under review." };
			}
			else
			{
				Errors = new[] { "This National ID is already registered." };
			}
			return null;
		}

		// التحقق من البريد الإلكتروني
		var existingByEmail = await _userManager.Users
			.FirstOrDefaultAsync(u => u.Email == dto.Email);
		if (existingByEmail != null)
		{
			Errors = new[] { "This email is already in use." };
			return null;
		}

		// التحقق من اسم المستخدم
		var existingByUsername = await _userManager.Users
			.FirstOrDefaultAsync(u => u.UserName == dto.UserName);
		if (existingByUsername != null)
		{
			Errors = new[] { "This username is already in use." };
			return null;
		}

		// تحويل اسم الدور المرسل من الفرونت-إند لاسم الـ Enum
		// الفرونت يرسل "Pharmacy" لكن الـ Enum هو "Pharmacist"
		var roleMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["Patient"] = "Patient",
			["Doctor"] = "Doctor",
			["Pharmacy"] = "Pharmacist",
			["Pharmacist"] = "Pharmacist",
			["Volunteer"] = "Volunteer",
			["Admin"] = "Admin"
		};

		var normalizedRole = roleMapping.TryGetValue(dto.Role ?? "", out var mapped) ? mapped : "Patient";

		UserType userType = UserType.Patient;
		if (Enum.TryParse<UserType>(normalizedRole, true, out var parsedType))
		{
			userType = parsedType;
		}

		var user = new ApplicationUser
		{
			Email = dto.Email,
			UserName = dto.UserName,
			FullName = dto.FullName,
			NationalID = dto.NationalID,
			UserType = userType,
			AccountStatus = Enums.AccountStatus.Pending,
			IsVerified = false,
			HasCompletedProfile = false,
			PhoneNumber = dto.PhoneNumber
		};

		var result = await _userManager.CreateAsync(user, dto.Password);
		if (!result.Succeeded)
		{
			Errors = result.Errors.Select(e => e.Description).ToArray();
			return null;
		}

		// الـ Identity Role يستخدم نفس اسم الـ Enum (Pharmacist مش Pharmacy)
		await _userManager.AddToRoleAsync(user, normalizedRole);

		var roles = await _userManager.GetRolesAsync(user);
		var token = _jwtService.GenerateToken(user, roles);

		return new AuthResponseDto
		{
			Token = token,
			UserId = user.Id,
			FullName = user.FullName,
			Role = roles.FirstOrDefault() ?? string.Empty,
			IsVerified = user.IsVerified,
			HasCompletedProfile = user.HasCompletedProfile,
			AccountStatus = user.AccountStatus.ToString(),
			PhoneNumber = user.PhoneNumber ?? string.Empty
		};
	}

	public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
	{
		var input = dto.EmailOrUsername?.Trim() ?? "";

		// البحث عن المستخدم بالإيميل أو اسم المستخدم
		var user = await _userManager.Users
			.FirstOrDefaultAsync(u =>
				u.Email == input ||
				u.UserName == input ||
				u.NormalizedUserName == input.ToUpper() ||
				u.NormalizedEmail == input.ToUpper());

		if (user is null) return null;

		var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
		if (!result.Succeeded) return null;

		var roles = await _userManager.GetRolesAsync(user);
		var token = _jwtService.GenerateToken(user, roles);

		return new AuthResponseDto
		{
			Token = token,
			UserId = user.Id,
			FullName = user.FullName,
			Role = roles.FirstOrDefault() ?? string.Empty,
			IsVerified = user.IsVerified,
			HasCompletedProfile = user.HasCompletedProfile,
			AccountStatus = user.AccountStatus.ToString(),
			PhoneNumber = user.PhoneNumber ?? string.Empty
		};
	}

	public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto dto)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null)
		{
			Errors = new[] { "User not found" };
			return false;
		}

		var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
		if (!result.Succeeded)
		{
			Errors = result.Errors.Select(e => e.Description).ToArray();
			return false;
		}

		return true;
	}

	public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
	{
		var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.NationalID == dto.NationalID);
		if (user == null)
		{
			Errors = new[] { "البريد الإلكتروني أو الرقم القومي غير صحيح." };
			return false;
		}

		var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
		var result = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);
		if (!result.Succeeded)
		{
			Errors = result.Errors.Select(e => e.Description).ToArray();
			return false;
		}

		return true;
	}
}
