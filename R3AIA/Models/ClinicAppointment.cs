using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static R3AIA.Models.Enums;

namespace R3AIA.Models
{
	public class ClinicAppointment
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int DoctorId { get; set; }
		[ForeignKey("DoctorId")]
		public Doctor Doctor { get; set; }

		public string? PatientId { get; set; }
		[ForeignKey("PatientId")]
		public ApplicationUser? Patient { get; set; }

		[Required]
		public string PatientName { get; set; }

		[Required]
		public string PatientPhone { get; set; }

		public string? Notes { get; set; }

		public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

		public DateTime CreatedAt { get; set; } = DateTime.Now;
	}
}
