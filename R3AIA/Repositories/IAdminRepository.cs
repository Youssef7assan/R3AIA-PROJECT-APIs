using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;
using R3AIA.Services;

namespace R3AIA.Repositories;

public interface IAdminRepository
{
	Task<IEnumerable<ApplicationUser>> GetPendingUsersAsync();
	Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();
	Task<bool> VerifyUserAsync(UserVerificationDto dto);
	Task<bool> BanUserAsync(string userId);
	Task<bool> UnbanUserAsync(string userId);
	Task<DonationCase> CreateDonationCaseAsync(CreateDonationCaseDto dto, string imageUrl);
	Task<IEnumerable<DonationCase>> GetAllDonationCasesAsync();
	Task<DonationCase?> EditDonationCaseAsync(int id, EditDonationCaseDto dto, string? newImageUrl);
	Task<IEnumerable<UserReport>> GetAllReportsAsync();
	Task<bool> ResolveReportAsync(ResolveReportDto dto);

	Task<IEnumerable<AdminActiveRequestDto>> GetStalledRequestsAsync(TimeSpan threshold);
	Task<AdminRequestDetailDto?> GetRequestDetailAsync(string type, int id);
	Task<bool> DeleteDonationCaseAsync(int id);
	Task<bool> BroadcastUrgentSosAsync(string type, int id);
}

public class AdminRepository : IAdminRepository
{
	private readonly AppDbContext _context;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly ISupportRepository _supportRepository;

	public AdminRepository(AppDbContext context, UserManager<ApplicationUser> userManager, ISupportRepository supportRepository)
	{
		_context = context;
		_userManager = userManager;
		_supportRepository = supportRepository;
	}

	public async Task<IEnumerable<ApplicationUser>> GetPendingUsersAsync()
	{
		// أي مستخدم لم يتم توثيقه بعد (بما في ذلك المرضى)
		return await _context.Users
			.Where(u => !u.IsVerified)
			.ToListAsync();
	}

	public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
	{
		return await _context.Users.ToListAsync();
	}

	public async Task<bool> VerifyUserAsync(UserVerificationDto dto)
	{
		var user = await _userManager.FindByIdAsync(dto.UserId);
		if (user is null) return false;

		user.IsVerified = dto.IsApproved;
		if (dto.IsApproved) 
		{
			user.AccountStatus = Enums.AccountStatus.Active;
		}
		
		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded) return false;

		if (dto.IsApproved)
		{
			if (user.UserType == Enums.UserType.Patient)
			{
				var patient = await _context.Patients.FirstOrDefaultAsync(p => p.IdentityUserId == user.Id);
				if (patient != null)
				{
					patient.IsVerified = true;
					await _context.SaveChangesAsync();
				}
				await _supportRepository.PushNotificationAsync(user.Id, "تم توثيق حسابك بنجاح، يمكنك الآن البدء بطلب الأدوية.");
			}
			else if (user.UserType == Enums.UserType.Doctor)
			{
				var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == user.Id);
				if (doctor != null)
				{
					doctor.IsVerified = true;
					await _context.SaveChangesAsync();
				}
				await _supportRepository.PushNotificationAsync(user.Id, "تم تفعيل حسابك كطبيب، يمكنك الآن استقبال الاستشارات وإدراجك في دليل الأطباء.");
			}
			else if (user.UserType == Enums.UserType.Pharmacist)
			{
				var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.IdentityUserId == user.Id);
				if (pharmacy != null)
				{
					pharmacy.IsVerified = true;
					await _context.SaveChangesAsync();
				}
				await _supportRepository.PushNotificationAsync(user.Id, "تم تفعيل حساب الصيدلية الخاص بك، يمكنك الآن التعاون في المنصة.");
			}
			else
			{
				await _supportRepository.PushNotificationAsync(user.Id, "تم تفعيل حسابك، يمكنك الآن استقبال الطلبات.");
			}
		}

		return true;
	}

	public async Task<DonationCase> CreateDonationCaseAsync(CreateDonationCaseDto dto, string imageUrl)
	{
		var donationCase = new DonationCase
		{
			Title = dto.Title,
			Description = dto.Description,
			GoalAmount = dto.GoalAmount,
			CaseImage = imageUrl,
			PatientName = dto.PatientName,
			CreatedAt = DateTime.Now
		};

		_context.DonationCases.Add(donationCase);
		await _context.SaveChangesAsync();
		return donationCase;
	}

	public async Task<IEnumerable<DonationCase>> GetAllDonationCasesAsync()
	{
		return await _context.DonationCases.OrderByDescending(c => c.CreatedAt).ToListAsync();
	}

	public async Task<DonationCase?> EditDonationCaseAsync(int id, EditDonationCaseDto dto, string? newImageUrl)
	{
		var donationCase = await _context.DonationCases.FindAsync(id);
		if (donationCase == null) return null;

		if (!string.IsNullOrEmpty(dto.Title)) donationCase.Title = dto.Title;
		if (!string.IsNullOrEmpty(dto.Description)) donationCase.Description = dto.Description;
		if (dto.GoalAmount.HasValue) donationCase.GoalAmount = dto.GoalAmount.Value;
		if (!string.IsNullOrEmpty(dto.PatientName)) donationCase.PatientName = dto.PatientName;
		
		if (!string.IsNullOrEmpty(newImageUrl))
		{
			donationCase.CaseImage = newImageUrl;
		}

		await _context.SaveChangesAsync();
		return donationCase;
	}

	public async Task<IEnumerable<UserReport>> GetAllReportsAsync()
	{
		return await _context.UserReports
			.Include(r => r.Reporter)
			.Include(r => r.ReportedUser)
			.ToListAsync();
	}

	public async Task<bool> ResolveReportAsync(ResolveReportDto dto)
	{
		var report = await _context.UserReports.FindAsync(dto.ReportId);
		if (report is null) return false;

		if (Enum.TryParse<ReportStatus>(dto.NewStatus, true, out var status))
			report.Status = status;
		else
			report.Status = ReportStatus.Resolved;

		report.AdminActionNotes = dto.AdminComment;

		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> BanUserAsync(string userId)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user is null) return false;

		user.AccountStatus = Enums.AccountStatus.Banned;
		user.IsActive = false;
		user.IsVerified = false;

		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded) return false;

		await _supportRepository.PushNotificationAsync(user.Id,
			"تم حظر حسابك نهائياً بسبب مخالفة سياسات الاستخدام.");

		return true;
	}

	public async Task<bool> UnbanUserAsync(string userId)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user is null) return false;

		// إعادة الحساب إلى حالة Pending ليعاد تقييمه ثم توثيقه
		user.AccountStatus = Enums.AccountStatus.Pending;
		user.IsActive = true;

		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded) return false;

		await _supportRepository.PushNotificationAsync(user.Id,
			"تم فك الحظر عن حسابك، سيتم مراجعة بياناتك مرة أخرى.");

		return true;
	}

	public async Task<IEnumerable<AdminActiveRequestDto>> GetStalledRequestsAsync(TimeSpan threshold)
	{
		var now = DateTime.Now;
		var thresholdDate = now.Subtract(threshold);

		// Medical requests stalled (doctors bottleneck)
		var medicalQueryResponse = await _context.MedicalRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.City)
			.Where(r => r.RequestStatus == Enums.RequestStatus.Pending && r.CreatedAt <= thresholdDate)
			.Select(r => new
			{
				r.Id,
				r.CreatedAt,
				PatientName = r.Patient.FullName,
				PatientGovernorate = r.Patient.Governorate.Name,
				PatientCity = r.Patient.City.Name,
				PatientPhone = r.Patient.PhoneNumber
			})
			.ToListAsync();

		var stalledMedicalDtos = medicalQueryResponse.Select(r => new AdminActiveRequestDto
		{
			Id = r.Id,
			Type = "Medical",
			CreatedAt = r.CreatedAt,
			AgeMinutes = now.Subtract(r.CreatedAt).TotalMinutes,
			Bottleneck = "Doctor",
			PatientName = r.PatientName,
			PatientGovernorate = r.PatientGovernorate,
			PatientCity = r.PatientCity,
			PatientPhone = r.PatientPhone
		}).ToList();

		// Medicine requests stalled (pharmacy or volunteer bottleneck)
		var stalledMedicine = await _context.MedicineRequests
			.Where(r => r.CreatedAt <= thresholdDate)
			.Select(r => new
			{
				Id = r.Id,
				CreatedAt = r.CreatedAt,
				NeedDelivery = r.NeedDelivery,
				PatientName = r.Patient.FullName,
				PatientGovernorate = r.Patient.Governorate.Name,
				PatientCity = r.Patient.City.Name,
				PatientPhone = r.Patient.PhoneNumber,
				HasPharmacy = r.PharmacyId != null,
				HasDeliveryTask = _context.DeliveryTasks.Any(t =>
					t.MedicineRequestId == r.Id && t.TaskStatus != Enums.DeliveryStatus.Delivered)
			})
			.ToListAsync();

		var stalledMedicineDtos = stalledMedicine.Select(x =>
		{
			var bottleneck = !x.HasPharmacy ? "Pharmacy"
				: (x.NeedDelivery && !x.HasDeliveryTask ? "Volunteer" : "Unknown");

			return new AdminActiveRequestDto
			{
				Id = x.Id,
				Type = "Medicine",
				CreatedAt = x.CreatedAt,
				AgeMinutes = now.Subtract(x.CreatedAt).TotalMinutes,
				Bottleneck = bottleneck,
				PatientName = x.PatientName,
				PatientGovernorate = x.PatientGovernorate,
				PatientCity = x.PatientCity,
				PatientPhone = x.PatientPhone
			};
		}).ToList();

		return stalledMedicalDtos.Concat(stalledMedicineDtos)
			.OrderByDescending(r => r.AgeMinutes)
			.ToList();
	}

	public async Task<AdminRequestDetailDto?> GetRequestDetailAsync(string type, int id)
	{
		var now = DateTime.Now;

		if (string.Equals(type, "Medical", StringComparison.OrdinalIgnoreCase))
		{
			var req = await _context.MedicalRequests
				.Include(r => r.Patient)
					.ThenInclude(p => p.City)
				.Include(r => r.Patient)
					.ThenInclude(p => p.Governorate)
				.Include(r => r.Specialty)
				.Include(r => r.Doctor)
				.FirstOrDefaultAsync(r => r.Id == id);

			if (req is null) return null;

			var detail = new AdminRequestDetailDto
			{
				Id = req.Id,
				Type = "Medical",
				CreatedAt = req.CreatedAt,
				AgeMinutes = now.Subtract(req.CreatedAt).TotalMinutes,
				PatientName = req.Patient.FullName,
				PatientGovernorate = req.Patient.Governorate?.Name ?? "غير محدد",
				PatientCity = req.Patient.City?.Name ?? "غير محدد",
				PatientPhone = req.Patient.PhoneNumber,
				SpecialtyName = req.Specialty?.Name ?? "غير محدد",
				Description = req.Description
			};

			// إذا لم يتم قبول الطلب بعد، نعرض الأطباء المرشحين
			if (req.DoctorId == null)
			{
				var doctors = await _context.Doctors
					.Where(d => d.GovernorateId == req.Patient.GovernorateId &&
					            d.SpecialtyId == req.SpecialtyId)
					.ToListAsync();

				detail.SuggestedContacts = doctors
					.Select(d => new AdminContactSuggestionDto
					{
						Name = d.FullName,
						Role = "Doctor",
						PhoneNumber = d.PhoneNumber
					})
					.ToList();
			}

			return detail;
		}

		if (string.Equals(type, "Medicine", StringComparison.OrdinalIgnoreCase))
		{
			var req = await _context.MedicineRequests
				.Include(r => r.Patient)
					.ThenInclude(p => p.City)
				.Include(r => r.Patient)
					.ThenInclude(p => p.Governorate)
				.Include(r => r.Pharmacy)
				.FirstOrDefaultAsync(r => r.Id == id);

			if (req is null) return null;

			var detail = new AdminRequestDetailDto
			{
				Id = req.Id,
				Type = "Medicine",
				CreatedAt = req.CreatedAt,
				AgeMinutes = now.Subtract(req.CreatedAt).TotalMinutes,
				PatientName = req.Patient.FullName,
				PatientGovernorate = req.Patient.Governorate?.Name ?? "غير محدد",
				PatientCity = req.Patient.City?.Name ?? "غير محدد",
				PatientPhone = req.Patient.PhoneNumber,
				PrescriptionImageUrl = req.PrescriptionImage
			};

			// إذا لم تُسند لصيدلية، نعرض الصيدليات المتاحة في نفس المحافظة
			if (req.PharmacyId == null)
			{
				var pharmacies = await _context.Pharmacies
					.Where(p => p.GovernorateId == req.Patient.GovernorateId)
					.ToListAsync();

				detail.SuggestedContacts = pharmacies
					.Select(p => new AdminContactSuggestionDto
					{
						Name = p.PharmacyName,
						Role = "Pharmacy",
						PhoneNumber = p.PhoneNumber
					})
					.ToList();
			}

			return detail;
		}


		return null;
	}

	public async Task<bool> DeleteDonationCaseAsync(int id)
	{
		var donationCase = await _context.DonationCases.FindAsync(id);
		if (donationCase is null) return false;

		_context.DonationCases.Remove(donationCase);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> BroadcastUrgentSosAsync(string type, int id)
	{
		string actionUrl = $"/urgent-case?type={type.ToLower()}&id={id}";
		string msgTitle = "🚨 نداء طوارئ: حالة متعثرة تتطلب تدخلاً عاجلاً!";
		string msgBody = "";

		if (string.Equals(type, "Medical", StringComparison.OrdinalIgnoreCase))
		{
			var req = await _context.MedicalRequests.Include(r => r.Patient).FirstOrDefaultAsync(r => r.Id == id);
			if (req == null) return false;

			var doctors = await _context.Doctors
				.Where(d => d.GovernorateId == req.Patient.GovernorateId && d.SpecialtyId == req.SpecialtyId)
				.ToListAsync();

			if (!doctors.Any()) return false;

			msgBody = $"يوجد مريض في محافظتك يحتاج استشارة طبية عاجلة في تخصصك. يرجى التكفل بالحالة الآن!";

			foreach (var doc in doctors)
			{
				await _supportRepository.PushNotificationAsync(doc.IdentityUserId, msgBody, msgTitle, actionUrl);
			}
			return true;
		}
		
		if (string.Equals(type, "Medicine", StringComparison.OrdinalIgnoreCase))
		{
			var req = await _context.MedicineRequests.Include(r => r.Patient).FirstOrDefaultAsync(r => r.Id == id);
			if (req == null) return false;

			// If no pharmacy assigned, notify pharmacies
			if (req.PharmacyId == null)
			{
				var pharmacies = await _context.Pharmacies
					.Where(p => p.GovernorateId == req.Patient.GovernorateId)
					.ToListAsync();
				msgBody = $"يوجد مريض في محافظتك يحتاج تبرعاً عاجلاً بالدواء. اضغط للتكفل بالحالة وتوفير الدواء!";
				foreach (var ph in pharmacies)
				{
					await _supportRepository.PushNotificationAsync(ph.IdentityUserId, msgBody, msgTitle, actionUrl);
				}
			}
			else if (req.NeedDelivery)
			{
				// Pharmacy assigned but needs volunteer, and apparently stalled
				var volunteers = await _context.Volunteers
					.Where(v => v.GovernorateId == req.Patient.GovernorateId)
					.ToListAsync();
				msgBody = $"يتوفر دواء في صيدلية بمحافظتك مجهز بالكامل للمريض ولكن لا يوجد من يوصله كمتطوع! أنقذ الموقف الآن.";
				foreach (var vol in volunteers)
				{
					await _supportRepository.PushNotificationAsync(vol.IdentityUserId, msgBody, msgTitle, actionUrl);
				}
			}

			return true;
		}

		return false;
	}
}
 

