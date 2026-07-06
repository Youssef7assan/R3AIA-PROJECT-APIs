using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace R3AIA.Models
{
	public class AppDbContext : IdentityDbContext<ApplicationUser>

	{
		public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
		{
			
		}
		public DbSet<Patient> Patients { get; set; }
		public DbSet<Doctor> Doctors { get; set; }
		public DbSet<Pharmacy> Pharmacies { get; set; }
		public DbSet<Volunteer> Volunteers { get; set; }

		public DbSet<Governorate> Governorates { get; set; }
		public DbSet<City> Cities { get; set; }
		public DbSet<Specialty> Specialties { get; set; }

		public DbSet<MedicalRequest> MedicalRequests { get; set; }
		public DbSet<MedicineRequest> MedicineRequests { get; set; }
		public DbSet<DeliveryTask> DeliveryTasks { get; set; }

		public DbSet<DonationCase> DonationCases { get; set; }
		public DbSet<Donation> Donations { get; set; }
		public DbSet<Notification> Notifications { get; set; }
		public DbSet<UserReport> UserReports { get; set; }
		public DbSet<SupportTicket> SupportTickets { get; set; }
		public DbSet<SupportTicketMessage> SupportTicketMessages { get; set; }
		public DbSet<ClinicAppointment> ClinicAppointments { get; set; }
		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder); 
										  
			builder.Entity<UserReport>()
				.HasOne(r => r.Reporter)
				.WithMany()
				.HasForeignKey(r => r.ReporterId)
				.OnDelete(DeleteBehavior.NoAction); // منع الحذف التلقائي المتعدد

			// 2. ربط الشخص المُبلغ عنه (ReportedUser)
			builder.Entity<UserReport>()
				.HasOne(r => r.ReportedUser)
				.WithMany()
				.HasForeignKey(r => r.ReportedUserId)
				.OnDelete(DeleteBehavior.NoAction); // منع الحذف التلقائي المتعدد

			builder.Entity<Patient>()
				.HasIndex(p => p.NationalID).IsUnique();

			builder.Entity<Volunteer>()
				.HasIndex(v => v.NationalID).IsUnique();

			// الرقم القومي فريد على مستوى نظام الهوية بالكامل (مع تجاهل القيم الفارغة)
			builder.Entity<ApplicationUser>()
				.HasIndex(u => u.NationalID)
				.IsUnique()
				.HasFilter("[NationalID] IS NOT NULL AND [NationalID] <> ''");


			// تعطيل الحذف التلقائي للمحافظة لمنع تداخل المسارات
			builder.Entity<Doctor>()
				.HasOne(d => d.Governorate)
				.WithMany()
				.HasForeignKey(d => d.GovernorateId)
				.OnDelete(DeleteBehavior.NoAction); // هذا هو السطر السحري

			builder.Entity<Patient>()
				.HasOne(p => p.Governorate)
				.WithMany()
				.HasForeignKey(p => p.GovernorateId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Pharmacy>()
				.HasOne(ph => ph.Governorate)
				.WithMany()
				.HasForeignKey(ph => ph.GovernorateId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<MedicalRequest>()
				.HasOne(m => m.Doctor)
				.WithMany()
				.HasForeignKey(m => m.DoctorId)
				.OnDelete(DeleteBehavior.Restrict);

			
			builder.Entity<MedicineRequest>()
				.HasOne(m => m.Pharmacy)
				.WithMany()
				.HasForeignKey(m => m.PharmacyId)
				.OnDelete(DeleteBehavior.Restrict);

	
			builder.Entity<City>()
				.HasOne(c => c.Governorate)
				.WithMany(g => g.Cities)
				.HasForeignKey(c => c.GovernorateId)
				.OnDelete(DeleteBehavior.Cascade);

			// Support System Rules
			builder.Entity<SupportTicket>()
				.HasOne(t => t.User)
				.WithMany()
				.HasForeignKey(t => t.UserId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<SupportTicketMessage>()
				.HasOne(m => m.Sender)
				.WithMany()
				.HasForeignKey(m => m.SenderId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<SupportTicketMessage>()
				.HasOne(m => m.Ticket)
				.WithMany(t => t.Messages)
				.HasForeignKey(m => m.TicketId)
				.OnDelete(DeleteBehavior.Cascade);

			// Clinic Appointments Rules
			builder.Entity<ClinicAppointment>()
				.HasOne(a => a.Doctor)
				.WithMany()
				.HasForeignKey(a => a.DoctorId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<ClinicAppointment>()
				.HasOne(a => a.Patient)
				.WithMany()
				.HasForeignKey(a => a.PatientId)
				.OnDelete(DeleteBehavior.NoAction);
		}

	}
}
