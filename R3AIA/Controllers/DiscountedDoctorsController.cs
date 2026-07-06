using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.Models;
using static R3AIA.Models.Enums;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscountedDoctorsController : ControllerBase
{
	private readonly AppDbContext _context;

	public DiscountedDoctorsController(AppDbContext context)
	{
		_context = context;
	}

	/// <summary>
	/// Public endpoint: list doctors with discounted or free consultations.
	/// Supports filtering by specialtyId, governorateId, cityId.
	/// </summary>
	[HttpGet]
	public async Task<IActionResult> GetAll(
		[FromQuery] int? specialtyId,
		[FromQuery] int? governorateId,
		[FromQuery] int? cityId)
	{
		var query = _context.Doctors
			.Include(d => d.Specialty)
			.Include(d => d.Governorate)
			.Include(d => d.City)
			.Where(d => d.IsVerified);

		if (specialtyId.HasValue)
			query = query.Where(d => d.SpecialtyId == specialtyId.Value);

		if (governorateId.HasValue)
			query = query.Where(d => d.GovernorateId == governorateId.Value);

		if (cityId.HasValue)
			query = query.Where(d => d.CityId == cityId.Value);

		var doctors = await query
			.OrderByDescending(d => d.Id)
			.Select(d => new
			{
				d.Id,
				d.FullName,
				specialty = d.Specialty.Name,
				specialtyId = d.SpecialtyId,
				governorate = d.Governorate.Name,
				governorateId = d.GovernorateId,
				city = d.City.Name,
				cityId = d.CityId,
				d.ClinicAddress,
				d.ClinicPhone,
				d.WorkingHours,
				d.ProfileImage,
				d.Description,
				consultationType = d.ConsultationType.ToString(),
				d.OriginalPrice,
				d.DiscountedPrice,
				discountPercentage = d.ConsultationType == ConsultationType.Discounted
					&& d.OriginalPrice.HasValue && d.OriginalPrice > 0
					? Math.Round((1 - (d.DiscountedPrice ?? 0) / d.OriginalPrice.Value) * 100)
					: (decimal?)null
			})
			.ToListAsync();

		return Ok(doctors);
	}

	/// <summary>
	/// Public endpoint: get single doctor details by ID.
	/// </summary>
	[HttpGet("{id}")]
	public async Task<IActionResult> GetById(int id)
	{
		var doctor = await _context.Doctors
			.Include(d => d.Specialty)
			.Include(d => d.Governorate)
			.Include(d => d.City)
			.Where(d => d.Id == id && d.IsVerified)
			.Select(d => new
			{
				d.Id,
				d.FullName,
				d.PhoneNumber,
				specialty = d.Specialty.Name,
				specialtyId = d.SpecialtyId,
				governorate = d.Governorate.Name,
				governorateId = d.GovernorateId,
				city = d.City.Name,
				cityId = d.CityId,
				d.ClinicAddress,
				d.ClinicPhone,
				d.WorkingHours,
				d.ProfileImage,
				d.Description,
				consultationType = d.ConsultationType.ToString(),
				d.OriginalPrice,
				d.DiscountedPrice,
				discountPercentage = d.ConsultationType == ConsultationType.Discounted
					&& d.OriginalPrice.HasValue && d.OriginalPrice > 0
					? Math.Round((1 - (d.DiscountedPrice ?? 0) / d.OriginalPrice.Value) * 100)
					: (decimal?)null
			})
			.FirstOrDefaultAsync();

		if (doctor is null) return NotFound();
		return Ok(doctor);
	}
}
