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

            const string schemaUsers = "пользователи_и_доступ";
            const string schemaOrders = "заказы";
            const string schemaLogistics = "логистика_и_смены";
            const string schemaFleet = "автопарк";
            const string schemaComm = "коммуникации";
            const string schemaBilling = "биллинг_и_подписки";
            const string schemaAnalytics = "аналитика_и_crm";

            static void Map<TEntity>(ModelBuilder mb, string table, string schema)
                where TEntity : class
                => mb.Entity<TEntity>().ToTable(table, schema);

            Map<Company>(modelBuilder, "Companies", schemaUsers);
            Map<User>(modelBuilder, "Users", schemaUsers);
            Map<Role>(modelBuilder, "Roles", schemaUsers);
            Map<Login>(modelBuilder, "Logins", schemaUsers);
            Map<PasswordResetCode>(modelBuilder, "PasswordResetCodes", schemaUsers);
            Map<ClientProfile>(modelBuilder, "ClientProfiles", schemaUsers);
            Map<ClientStatus>(modelBuilder, "ClientStatuses", schemaUsers);
            Map<ClientSegment>(modelBuilder, "ClientSegments", schemaUsers);
            Map<ClientNote>(modelBuilder, "ClientNotes", schemaUsers);
            Map<ClientNoteType>(modelBuilder, "ClientNoteTypes", schemaUsers);
            Map<CourierProfile>(modelBuilder, "CourierProfiles", schemaUsers);
            Map<ManagerProfile>(modelBuilder, "ManagerProfiles", schemaUsers);
            Map<CourierStatus>(modelBuilder, "CourierStatuses", schemaUsers);

            Map<Order>(modelBuilder, "Orders", schemaOrders);
            Map<OrderType>(modelBuilder, "OrderTypes", schemaOrders);
            Map<OrderStatus>(modelBuilder, "OrderStatuses", schemaOrders);
            Map<PackageType>(modelBuilder, "PackageTypes", schemaOrders);
            Map<PaymentMethod>(modelBuilder, "PaymentMethods", schemaOrders);
            Map<ScheduleType>(modelBuilder, "ScheduleTypes", schemaOrders);
            Map<OrderRouteStop>(modelBuilder, "OrderRouteStops", schemaOrders);
            Map<OrderTimelineEvent>(modelBuilder, "OrderTimelineEvents", schemaOrders);

            Map<LogisticsHub>(modelBuilder, "LogisticsHubs", schemaLogistics);
            Map<Address>(modelBuilder, "Addresses", schemaLogistics);
            Map<CourierShift>(modelBuilder, "CourierShifts", schemaLogistics);
            Map<ShiftStatus>(modelBuilder, "ShiftStatuses", schemaLogistics);
            Map<ShiftAssignment>(modelBuilder, "ShiftAssignments", schemaLogistics);
            Map<ShiftPlan>(modelBuilder, "ShiftPlans", schemaLogistics);
            Map<ServiceAreaZone>(modelBuilder, "ServiceAreaZones", schemaLogistics);
            Map<ServiceAreaZoneCourier>(modelBuilder, "ServiceAreaZoneCouriers", schemaLogistics);

            Map<Vehicle>(modelBuilder, "Vehicles", schemaFleet);
            Map<VehicleCategory>(modelBuilder, "VehicleCategories", schemaFleet);
            Map<VehicleModel>(modelBuilder, "VehicleModels", schemaFleet);
            Map<VehicleBrand>(modelBuilder, "VehicleBrands", schemaFleet);
            Map<VehicleBodyType>(modelBuilder, "VehicleBodyTypes", schemaFleet);
            Map<FuelType>(modelBuilder, "FuelTypes", schemaFleet);
            Map<TransmissionType>(modelBuilder, "TransmissionTypes", schemaFleet);
            Map<VehicleDriveType>(modelBuilder, "DriveTypes", schemaFleet);
            Map<VehicleAssignment>(modelBuilder, "VehicleAssignments", schemaFleet);
            Map<FuelCard>(modelBuilder, "FuelCards", schemaFleet);
            Map<FuelCardStatus>(modelBuilder, "FuelCardStatuses", schemaFleet);
            Map<FuelCardType>(modelBuilder, "FuelCardTypes", schemaFleet);
            Map<FuelCompany>(modelBuilder, "FuelCompanies", schemaFleet);
            Map<CourierFuelCard>(modelBuilder, "CourierFuelCards", schemaFleet);

            Map<ChatRoom>(modelBuilder, "ChatRooms", schemaComm);
            Map<ChatRoomType>(modelBuilder, "ChatRoomTypes", schemaComm);
            Map<ChatMessage>(modelBuilder, "ChatMessages", schemaComm);
            Map<ChatParticipant>(modelBuilder, "ChatParticipants", schemaComm);
            Map<ChatQuickReplyTemplate>(modelBuilder, "ChatQuickReplyTemplates", schemaComm);
            Map<NotificationType>(modelBuilder, "NotificationTypes", schemaComm);
            Map<Notification>(modelBuilder, "Notifications", schemaComm);
            Map<CommunicationTemplate>(modelBuilder, "CommunicationTemplates", schemaComm);
            Map<SupportTicket>(modelBuilder, "SupportTickets", schemaComm);

            Map<SubscriptionPlan>(modelBuilder, "SubscriptionPlans", schemaBilling);
            Map<CompanySubscription>(modelBuilder, "CompanySubscriptions", schemaBilling);
            Map<BillingInvoice>(modelBuilder, "BillingInvoices", schemaBilling);
            Map<PaymentTransaction>(modelBuilder, "PaymentTransactions", schemaBilling);
            Map<BillingWebhookEvent>(modelBuilder, "BillingWebhookEvents", schemaBilling);

            Map<AuditLog>(modelBuilder, "AuditLogs", schemaAnalytics);
            Map<Report>(modelBuilder, "Reports", schemaAnalytics);
            Map<ReportStatus>(modelBuilder, "ReportStatuses", schemaAnalytics);
            Map<Lead>(modelBuilder, "Leads", schemaAnalytics);
            Map<LeadSource>(modelBuilder, "LeadSources", schemaAnalytics);
            Map<LeadStage>(modelBuilder, "LeadStages", schemaAnalytics);
            Map<Review>(modelBuilder, "Reviews", schemaAnalytics);
            Map<ScheduledReportJob>(modelBuilder, "ScheduledReportJobs", schemaAnalytics);

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

            modelBuilder.Entity<PasswordResetCode>()
                .HasOne(p => p.Login)
                .WithMany(l => l.PasswordResetCodes)
                .HasForeignKey(p => p.LoginId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientProfile>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.User_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientProfile>()
                .HasOne(c => c.ClientStatus)
                .WithMany(s => s.ClientProfiles)
                .HasForeignKey(c => c.ClientStatus_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientProfile>()
                .HasOne(c => c.ClientSegment)
                .WithMany(s => s.ClientProfiles)
                .HasForeignKey(c => c.ClientSegment_id)
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

            modelBuilder.Entity<ShiftAssignment>()
                .HasOne(sa => sa.ShiftPlan)
                .WithMany(sp => sp.Assignments)
                .HasForeignKey(sa => sa.ShiftPlan_id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ShiftAssignment>()
                .HasOne(sa => sa.OrderRouteStop)
                .WithMany()
                .HasForeignKey(sa => sa.OrderRouteStop_id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ShiftAssignment>()
                .HasIndex(sa => new { sa.OrderRouteStop_id, sa.Status })
                .HasDatabaseName("IX_ShiftAssignments_RouteStop_Status");

            modelBuilder.Entity<ShiftAssignment>()
                .HasIndex(sa => sa.ShiftPlan_id);

            modelBuilder.Entity<ShiftAssignment>()
                .HasIndex(sa => sa.Order_id)
                .HasDatabaseName("UX_ShiftAssignments_ActiveOrder")
                .IsUnique()
                .HasFilter("\"ShiftPlan_id\" IS NOT NULL AND \"Status\" IN (1,2)");

            modelBuilder.Entity<ShiftPlan>()
                .HasOne(sp => sp.Company)
                .WithMany()
                .HasForeignKey(sp => sp.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShiftPlan>()
                .HasOne(sp => sp.CourierShift)
                .WithMany()
                .HasForeignKey(sp => sp.Shift_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShiftPlan>()
                .HasOne(sp => sp.CourierProfile)
                .WithMany()
                .HasForeignKey(sp => sp.Courier_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShiftPlan>()
                .HasOne(sp => sp.Vehicle)
                .WithMany()
                .HasForeignKey(sp => sp.Vehicle_id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ShiftPlan>()
                .HasIndex(sp => new { sp.Company_id, sp.Status });

            modelBuilder.Entity<Order>()
                .HasOne(o => o.LockedShiftPlan)
                .WithMany()
                .HasForeignKey(o => o.Plan_locked_shiftPlan_id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Plan_locked_shiftPlan_id);

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

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.CourierShift)
                .WithMany()
                .HasForeignKey(n => n.Shift_id)
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

            modelBuilder.Entity<LogisticsHub>()
                .HasOne(h => h.Company)
                .WithMany()
                .HasForeignKey(h => h.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LogisticsHub>()
                .HasOne(h => h.Address)
                .WithMany()
                .HasForeignKey(h => h.Address_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.OriginHub)
                .WithMany()
                .HasForeignKey(o => o.OriginHub_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.DestinationHub)
                .WithMany()
                .HasForeignKey(o => o.DestinationHub_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderRouteStop>()
                .HasOne(s => s.Order)
                .WithMany(o => o.RouteStops)
                .HasForeignKey(s => s.Order_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderRouteStop>()
                .HasOne(s => s.Address)
                .WithMany()
                .HasForeignKey(s => s.Address_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderRouteStop>()
                .HasOne(s => s.LogisticsHub)
                .WithMany()
                .HasForeignKey(s => s.LogisticsHub_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderRouteStop>()
                .HasOne(s => s.AssignedCourier)
                .WithMany()
                .HasForeignKey(s => s.AssignedCourier_id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OrderTimelineEvent>()
                .HasOne(e => e.Order)
                .WithMany(o => o.TimelineEvents)
                .HasForeignKey(e => e.Order_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SupportTicket>()
                .HasOne(t => t.Company)
                .WithMany()
                .HasForeignKey(t => t.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupportTicket>()
                .HasOne(t => t.Order)
                .WithMany()
                .HasForeignKey(t => t.Order_id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SupportTicket>()
                .HasOne(t => t.ClientProfile)
                .WithMany()
                .HasForeignKey(t => t.ClientProfile_id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SupportTicket>()
                .HasOne(t => t.ResponsibleUser)
                .WithMany()
                .HasForeignKey(t => t.ResponsibleUser_id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SupportTicket>()
                .HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUser_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServiceAreaZone>()
                .HasOne(z => z.Company)
                .WithMany()
                .HasForeignKey(z => z.Company_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServiceAreaZoneCourier>()
                .HasOne(zc => zc.Zone)
                .WithMany(z => z.Couriers)
                .HasForeignKey(zc => zc.ServiceAreaZone_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ServiceAreaZoneCourier>()
                .HasOne(zc => zc.Courier)
                .WithMany()
                .HasForeignKey(zc => zc.CourierProfile_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CompanySubscription>()
                .HasOne(s => s.Company)
                .WithMany()
                .HasForeignKey(s => s.Company_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CompanySubscription>()
                .HasOne(s => s.Plan)
                .WithMany()
                .HasForeignKey(s => s.SubscriptionPlan_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BillingInvoice>()
                .HasOne(i => i.Company)
                .WithMany()
                .HasForeignKey(i => i.Company_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BillingInvoice>()
                .HasOne(i => i.Plan)
                .WithMany()
                .HasForeignKey(i => i.SubscriptionPlan_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(t => t.Invoice)
                .WithMany()
                .HasForeignKey(t => t.BillingInvoice_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommunicationTemplate>()
                .HasOne(t => t.Company)
                .WithMany()
                .HasForeignKey(t => t.Company_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ScheduledReportJob>()
                .HasOne(j => j.Company)
                .WithMany()
                .HasForeignKey(j => j.Company_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BillingWebhookEvent>()
                .HasIndex(e => new { e.Provider, e.EventKey })
                .IsUnique();

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

            modelBuilder.Entity<ChatQuickReplyTemplate>()
                .HasOne(t => t.Company)
                .WithMany()
                .HasForeignKey(t => t.Company_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatQuickReplyTemplate>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.User_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientNote>()
                .HasOne(n => n.ClientProfile)
                .WithMany()
                .HasForeignKey(n => n.ClientProfile_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientNote>()
                .HasOne(n => n.Author)
                .WithMany()
                .HasForeignKey(n => n.Author_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientNote>()
                .HasOne(n => n.ClientNoteType)
                .WithMany(t => t.ClientNotes)
                .HasForeignKey(n => n.ClientNoteType_id)
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
                .IsRequired(false)
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
                .IsRequired(false)
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
        public DbSet<PasswordResetCode> PasswordResetCodes { get; set; }
        public DbSet<ClientProfile> ClientProfiles { get; set; }
        public DbSet<ClientStatus> ClientStatuses { get; set; }
        public DbSet<ClientSegment> ClientSegments { get; set; }
        public DbSet<ClientNote> ClientNotes { get; set; }
        public DbSet<ClientNoteType> ClientNoteTypes { get; set; }
        public DbSet<CourierProfile> CourierProfiles { get; set; }
        public DbSet<ManagerProfile> ManagerProfiles { get; set; }
        public DbSet<CourierStatus> CourierStatuses { get; set; }
        public DbSet<CourierShift> CourierShifts { get; set; }
        public DbSet<ShiftStatus> ShiftStatuses { get; set; }
        public DbSet<ShiftAssignment> ShiftAssignments { get; set; }
        public DbSet<ShiftPlan> ShiftPlans { get; set; }
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
        public DbSet<ChatQuickReplyTemplate> ChatQuickReplyTemplates { get; set; }
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
        public DbSet<Lead> Leads { get; set; }
        public DbSet<LeadSource> LeadSources { get; set; }
        public DbSet<LeadStage> LeadStages { get; set; }
        public DbSet<LogisticsHub> LogisticsHubs { get; set; }
        public DbSet<OrderRouteStop> OrderRouteStops { get; set; }
        public DbSet<OrderTimelineEvent> OrderTimelineEvents { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<ServiceAreaZone> ServiceAreaZones { get; set; }
        public DbSet<ServiceAreaZoneCourier> ServiceAreaZoneCouriers { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<CompanySubscription> CompanySubscriptions { get; set; }
        public DbSet<BillingInvoice> BillingInvoices { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<BillingWebhookEvent> BillingWebhookEvents { get; set; }
        public DbSet<CommunicationTemplate> CommunicationTemplates { get; set; }
        public DbSet<ScheduledReportJob> ScheduledReportJobs { get; set; }
    }
}


