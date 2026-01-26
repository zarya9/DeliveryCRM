using APIDeliveryCRM.Model;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.ContextDb
{
    public class ContextDB : DbContext
    {
        public ContextDB(DbContextOptions<ContextDB> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Company relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Company)
                .WithMany()
                .HasForeignKey(o => o.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientProfile>()
                .HasOne(c => c.Company)
                .WithMany()
                .HasForeignKey(c => c.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourierProfile>()
                .HasOne(c => c.Company)
                .WithMany()
                .HasForeignKey(c => c.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ManagerProfile>()
                .HasOne(m => m.Company)
                .WithMany()
                .HasForeignKey(m => m.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Address>()
                .HasOne(a => a.Company)
                .WithMany()
                .HasForeignKey(a => a.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatRoom>()
                .HasOne(cr => cr.Company)
                .WithMany()
                .HasForeignKey(cr => cr.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Company)
                .WithMany()
                .HasForeignKey(n => n.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Company)
                .WithMany()
                .HasForeignKey(r => r.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Company)
                .WithMany()
                .HasForeignKey(v => v.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Company)
                .WithMany()
                .HasForeignKey(r => r.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourierShift>()
                .HasOne(s => s.Company)
                .WithMany()
                .HasForeignKey(s => s.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FuelCard>()
                .HasOne(fc => fc.Company)
                .WithMany()
                .HasForeignKey(fc => fc.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.Company)
                .WithMany()
                .HasForeignKey(a => a.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShiftAssignment>()
                .HasOne(sa => sa.Company)
                .WithMany()
                .HasForeignKey(sa => sa.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehicleAssignment>()
                .HasOne(va => va.Company)
                .WithMany()
                .HasForeignKey(va => va.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourierFuelCard>()
                .HasOne(cfc => cfc.Company)
                .WithMany()
                .HasForeignKey(cfc => cfc.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            // Existing relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.Role_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Login>()
                .HasOne(l => l.User)
                .WithMany(u => u.Logins)
                .HasForeignKey(l => l.ID_User)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientProfile>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.User_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ManagerProfile>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.User_id)
                .OnDelete(DeleteBehavior.Restrict);

            // PaymentMethod table configuration
            // Table name is "PaymentMethods" (without underscore) after RenameModels migration
            modelBuilder.Entity<PaymentMethod>()
                .ToTable("PaymentMethods");

            // ClientProfile -> PaymentMethod relationship
            modelBuilder.Entity<ClientProfile>()
                .HasOne(c => c.PaymentMethod)
                .WithMany()
                .HasForeignKey(c => c.Preferred_payment_method_id)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            modelBuilder.Entity<CourierProfile>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.User_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourierProfile>()
                .HasOne(c => c.VehicleCategory)
                .WithMany(vc => vc.CourierProfiles)
                .HasForeignKey(c => c.VehicleCategory_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourierProfile>()
                .HasOne(c => c.ScheduleType)
                .WithMany()
                .HasForeignKey(c => c.WorkSchedule_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourierProfile>()
                .HasOne(c => c.CourierStatus)
                .WithMany(s => s.CourierProfiles)
                .HasForeignKey(c => c.CurrentStatus_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourierShift>()
                .HasOne(s => s.CourierProfile)
                .WithMany()
                .HasForeignKey(s => s.Courier_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourierShift>()
                .HasOne(s => s.ShiftStatus)
                .WithMany(st => st.CourierShifts)
                .HasForeignKey(s => s.ShiftStatus_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.ClientProfile)
                .WithMany()
                .HasForeignKey(o => o.Client_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.OrderType)
                .WithMany()
                .HasForeignKey(o => o.OrderType_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.OrderStatus)
                .WithMany()
                .HasForeignKey(o => o.Status_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.CourierProfile)
                .WithMany()
                .HasForeignKey(o => o.Courier_id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.PackageType)
                .WithMany()
                .HasForeignKey(o => o.PackageType_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.PaymentMethod)
                .WithMany()
                .HasForeignKey(o => o.PaymentMethod_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShiftAssignment>()
                .HasOne(sa => sa.Shift)
                .WithMany()
                .HasForeignKey(sa => sa.Shift_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShiftAssignment>()
                .HasOne(sa => sa.Order)
                .WithMany()
                .HasForeignKey(sa => sa.Order_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.User_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.NotificationType)
                .WithMany()
                .HasForeignKey(n => n.Type_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Order)
                .WithMany()
                .HasForeignKey(n => n.Order_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.Order_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.UserAuthor)
                .WithMany()
                .HasForeignKey(r => r.Author_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.UserTarget)
                .WithMany()
                .HasForeignKey(r => r.TargetUser_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Address>()
                .HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.User_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.PickupAddress)
                .WithMany(a => a.PickupOrders)
                .HasForeignKey(o => o.PickupAddress_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.DeliveryAddress)
                .WithMany(a => a.DeliveryOrders)
                .HasForeignKey(o => o.DeliveryAddress_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatRoom>()
                .HasOne(cr => cr.ChatRoomType)
                .WithMany(crt => crt.ChatRooms)
                .HasForeignKey(cr => cr.ChatRoomType_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatRoom>()
                .HasOne(cr => cr.Order)
                .WithMany(o => o.ChatRooms)
                .HasForeignKey(cr => cr.Order_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatParticipant>()
                .HasOne(cp => cp.ChatRoom)
                .WithMany(cr => cr.Participants)
                .HasForeignKey(cp => cp.ChatRoom_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatParticipant>()
                .HasOne(cp => cp.User)
                .WithMany(u => u.ChatParticipants)
                .HasForeignKey(cp => cp.User_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.ChatRoom)
                .WithMany(cr => cr.Messages)
                .HasForeignKey(cm => cm.ChatRoom_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(cm => cm.Sender_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.VehicleCategory)
                .WithMany()
                .HasForeignKey(v => v.Category_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.VehicleModel)
                .WithMany()
                .HasForeignKey(v => v.Model_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.VehicleBodyType)
                .WithMany()
                .HasForeignKey(v => v.BodyType_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.FuelType)
                .WithMany()
                .HasForeignKey(v => v.FuelType_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.CourierProfile)
                .WithMany()
                .HasForeignKey(v => v.CurrentCourier_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehicleAssignment>()
                .HasOne(va => va.Vehicle)
                .WithMany()
                .HasForeignKey(va => va.Vehicle_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehicleAssignment>()
                .HasOne(va => va.Courier)
                .WithMany()
                .HasForeignKey(va => va.Courier_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehicleModel>()
                .HasOne(vm => vm.VehicleBrand)
                .WithMany()
                .HasForeignKey(vm => vm.Brand_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehicleModel>()
                .HasOne(vm => vm.TransmissionType)
                .WithMany()
                .HasForeignKey(vm => vm.TransmissionType_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehicleModel>()
                .HasOne(vm => vm.VehicleDriveType)
                .WithMany()
                .HasForeignKey(vm => vm.DriveType_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FuelCard>()
                .HasOne(fc => fc.FuelCardStatus)
                .WithMany()
                .HasForeignKey(fc => fc.Status_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FuelCard>()
                .HasOne(fc => fc.FuelCompany)
                .WithMany()
                .HasForeignKey(fc => fc.FuelCompany_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FuelCard>()
                .HasOne(fc => fc.FuelCardType)
                .WithMany()
                .HasForeignKey(fc => fc.Type_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourierFuelCard>()
                .HasOne(cfc => cfc.CourierProfile)
                .WithMany()
                .HasForeignKey(cfc => cfc.Courier_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourierFuelCard>()
                .HasOne(cfc => cfc.FuelCard)
                .WithMany()
                .HasForeignKey(cfc => cfc.FuelCard_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourierFuelCard>()
                .HasOne(cfc => cfc.User)
                .WithMany()
                .HasForeignKey(cfc => cfc.AssignedByUser_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.User_id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.User_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.ReportStatus)
                .WithMany(rs => rs.Reports)
                .HasForeignKey(r => r.Status_id)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Login> Logins { get; set; }
        public DbSet<ClientProfile> ClientProfiles { get; set; }
        public DbSet<CourierProfile> CourierProfiles { get; set; }
        public DbSet<ManagerProfile> ManagerProfiles { get; set; }
        public DbSet<CourierStatus> CourierStatuses { get; set; }
        public DbSet<CourierShift> CourierShifts { get; set; }
        public DbSet<ShiftStatus> ShiftStatuses { get; set; }
        public DbSet<ShiftAssignment> ShiftAssignments { get; set; }
        public DbSet<PackageType> PackageTypes { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderType> OrderTypes { get; set; }
        public DbSet<OrderStatus> OrderStatuses { get; set; }
        public DbSet<NotificationType> NotificationTypes { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ScheduleType> ScheduleTypes { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatRoomType> ChatRoomTypes { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatParticipant> ChatParticipants { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleCategory> VehicleCategories { get; set; }
        public DbSet<VehicleModel> VehicleModels { get; set; }
        public DbSet<VehicleBrand> VehicleBrands { get; set; }
        public DbSet<VehicleBodyType> VehicleBodyTypes { get; set; }
        public DbSet<FuelType> FuelTypes { get; set; }
        public DbSet<TransmissionType> TransmissionTypes { get; set; }
        public DbSet<VehicleDriveType> DriveTypes { get; set; }
        public DbSet<VehicleAssignment> VehicleAssignments { get; set; }
        public DbSet<FuelCard> FuelCards { get; set; }
        public DbSet<FuelCardStatus> FuelCardStatuses { get; set; }
        public DbSet<FuelCardType> FuelCardTypes { get; set; }
        public DbSet<FuelCompany> FuelCompanies { get; set; }
        public DbSet<CourierFuelCard> CourierFuelCards { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportStatus> ReportStatuses { get; set; }
    }
}


