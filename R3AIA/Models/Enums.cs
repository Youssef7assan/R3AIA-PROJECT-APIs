namespace R3AIA.Models
{
	public class Enums
	
	{
		public enum UserType { Admin = 1, Doctor, Pharmacist, Patient, Volunteer }

		public enum AccountStatus { Active = 1, Pending, Banned }

		public enum RequestStatus { Pending, Accepted, Completed, Cancelled, Fulfilled }

		
		public enum DeliveryStatus { Available, Taken, OutForDelivery, Delivered }

		public enum DonationStatus { Pending, Approved, Rejected }
		
		public enum SupportTicketStatus { Open, Closed }

		public enum ConsultationType { Free, Discounted }

		public enum AppointmentStatus { Pending, Confirmed, Cancelled }
	}
}

