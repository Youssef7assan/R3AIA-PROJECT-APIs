using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace R3AIA.DTOs;

public class EditDonationCaseDto
{
	public string? Title { get; set; }
	public string? Description { get; set; }
	public decimal? GoalAmount { get; set; }
	public string? PatientName { get; set; }
	public IFormFile? CaseImage { get; set; }
}
