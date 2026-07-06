using AutoMapper;
using R3AIA.DTOs;
using R3AIA.Models;

namespace R3AIA.Mapping;

	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<MedicineRequest, MedicineRequestSummaryDto>()
			.ForMember(d => d.PatientName, opt => opt.MapFrom(s => s.Patient.FullName))
			.ForMember(d => d.PatientPhone, opt => opt.MapFrom(s => s.Patient.PhoneNumber))
			.ForMember(d => d.PatientAddress, opt => opt.MapFrom(s => s.Patient.Address))
			.ForMember(d => d.PatientCity, opt => opt.MapFrom(s => s.Patient.City.Name))
			.ForMember(d => d.PatientGovernorate, opt => opt.MapFrom(s => s.Patient.Governorate.Name))
			.ForMember(d => d.PrescriptionImageUrl, opt => opt.MapFrom(s => s.PrescriptionImage))
			.ForMember(d => d.Status, opt => opt.MapFrom(s => s.RequestStatus.ToString()));

		CreateMap<MedicalRequest, MedicalRequestSummaryDto>()
			.ForMember(d => d.PatientName, opt => opt.MapFrom(s => s.Patient.FullName))
			.ForMember(d => d.PatientPhone, opt => opt.MapFrom(s => s.Patient.PhoneNumber))
			.ForMember(d => d.PatientGovernorate, opt => opt.MapFrom(s => s.Patient.Governorate.Name))
			.ForMember(d => d.SpecialtyName, opt => opt.MapFrom(s => s.Specialty.Name))
			.ForMember(d => d.Status, opt => opt.MapFrom(s => s.RequestStatus.ToString()))
			.ForMember(d => d.HasAttachments, opt => opt.MapFrom(s => s.HasAttachments));

		// Mapping for medical request detail
		CreateMap<MedicalRequest, MedicalRequestDetailDto>()
			.ForMember(dest => dest.RequestId, opt => opt.MapFrom(src => src.Id))
			.ForMember(dest => dest.SpecialtyName, opt => opt.MapFrom(src => src.Specialty.Name))
			.ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.RequestStatus.ToString()))
			.ForMember(dest => dest.Patient, opt => opt.MapFrom(src => src.Patient))
			.ForMember(dest => dest.MedicalImages, opt => opt.MapFrom(src => 
				string.IsNullOrEmpty(src.MedicalImages) 
				? new List<string>() 
				: src.MedicalImages.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()));

		// Mapping for patient info
		CreateMap<Patient, PatientInfoDto>()
			.ForMember(dest => dest.GovernorateName, opt => opt.MapFrom(src => src.Governorate.Name))
			.ForMember(dest => dest.CityName, opt => opt.MapFrom(src => src.City.Name));

		// Mapping for patient's own requests
		CreateMap<MedicalRequest, MyMedicalRequestDto>()
			.ForMember(dest => dest.SpecialtyName, opt => opt.MapFrom(src => src.Specialty.Name))
			.ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.RequestStatus.ToString()))
			.ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => 
				src.Doctor != null ? src.Doctor.FullName : null))
			.ForMember(dest => dest.DoctorPhone, opt => opt.MapFrom(src => 
				src.Doctor != null ? src.Doctor.PhoneNumber : null))
			.ForMember(dest => dest.ClinicAddress, opt => opt.MapFrom(src => 
				src.Doctor != null ? src.Doctor.ClinicAddress : null));

		CreateMap<DonationCase, DonationCaseSummaryDto>();
		CreateMap<Donation, DonationResultDto>()
			.ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
		CreateMap<Notification, NotificationDto>();

		// UserReport mapping
		CreateMap<UserReport, UserReportDto>()
			.ForMember(d => d.ReporterName, opt => opt.MapFrom(s => s.Reporter.UserName))
			.ForMember(d => d.ReportedUserName, opt => opt.MapFrom(s => s.ReportedUser.UserName))
			.ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
		
		
		// Medicine Request mappings
		CreateMap<MedicineRequest, MyMedicineRequestDto>()
			.ForMember(dest => dest.PrescriptionImageUrl, opt => opt.MapFrom(src => src.PrescriptionImage))
			.ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.RequestStatus.ToString()))
			.ForMember(dest => dest.PharmacyName, opt => opt.MapFrom(src => 
				src.Pharmacy != null ? src.Pharmacy.PharmacyName : null))
			.ForMember(dest => dest.PharmacyPhone, opt => opt.MapFrom(src => 
				src.Pharmacy != null ? src.Pharmacy.PhoneNumber : null))
			.ForMember(dest => dest.PharmacyAddress, opt => opt.MapFrom(src => 
				src.Pharmacy != null ? src.Pharmacy.Address : null))
			.ForMember(dest => dest.VolunteerName, opt => opt.MapFrom(src => 
				src.Volunteer != null ? src.Volunteer.FullName : null))
			.ForMember(dest => dest.VolunteerPhone, opt => opt.MapFrom(src => 
				src.Volunteer != null ? src.Volunteer.PhoneNumber : null))
			.ForMember(dest => dest.DeliveryStatus, opt => opt.MapFrom(src => 
				src.DeliveryStatus != null ? src.DeliveryStatus.ToString() : null));
		
		CreateMap<MedicineRequest, DeliveryTaskDto>()
			.ForMember(dest => dest.RequestId, opt => opt.MapFrom(src => src.Id))
			.ForMember(dest => dest.PharmacyName, opt => opt.MapFrom(src => src.Pharmacy!.PharmacyName))
			.ForMember(dest => dest.PharmacyAddress, opt => opt.MapFrom(src => src.Pharmacy!.Address))
			.ForMember(dest => dest.PharmacyPhone, opt => opt.MapFrom(src => src.Pharmacy!.PhoneNumber))
			.ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.Patient.FullName))
			.ForMember(dest => dest.PatientAddress, opt => opt.MapFrom(src => 
				src.Patient.City != null ? $"{src.Patient.City.Name}, {src.Patient.Governorate.Name}" : ""))
			.ForMember(dest => dest.PatientPhone, opt => opt.MapFrom(src => src.Patient.PhoneNumber));
	}
	}
