using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIDeliveryCRM.Migrations
{
    /// <inheritdoc />
    public partial class SplitTablesByRussianSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "логистика_и_смены");

            migrationBuilder.EnsureSchema(
                name: "аналитика_и_crm");

            migrationBuilder.EnsureSchema(
                name: "биллинг_и_подписки");

            migrationBuilder.EnsureSchema(
                name: "коммуникации");

            migrationBuilder.EnsureSchema(
                name: "пользователи_и_доступ");

            migrationBuilder.EnsureSchema(
                name: "автопарк");

            migrationBuilder.EnsureSchema(
                name: "заказы");

            migrationBuilder.RenameTable(
                name: "Vehicles",
                newName: "Vehicles",
                newSchema: "автопарк");

            migrationBuilder.RenameTable(
                name: "VehicleModels",
                newName: "VehicleModels",
                newSchema: "автопарк");

            migrationBuilder.RenameTable(
                name: "VehicleCategories",
                newName: "VehicleCategories",
                newSchema: "автопарк");

            migrationBuilder.RenameTable(
                name: "VehicleBrands",
                newName: "VehicleBrands",
                newSchema: "автопарк");

            migrationBuilder.RenameTable(
                name: "VehicleBodyTypes",
                newName: "VehicleBodyTypes",
                newSchema: "автопарк");

            migrationBuilder.RenameTable(
                name: "VehicleAssignments",
                newName: "VehicleAssignments",
                newSchema: "автопарк");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Users",
                newSchema: "пользователи_и_доступ");

            migrationBuilder.RenameTable(
                name: "TransmissionTypes",
                newName: "TransmissionTypes",
                newSchema: "автопарк");

            migrationBuilder.RenameTable(
                name: "SupportTickets",
                newName: "SupportTickets",
                newSchema: "коммуникации");

            migrationBuilder.RenameTable(
                name: "SubscriptionPlans",
                newName: "SubscriptionPlans",
                newSchema: "биллинг_и_подписки");

            migrationBuilder.RenameTable(
                name: "ShiftStatuses",
                newName: "ShiftStatuses",
                newSchema: "логистика_и_смены");

            migrationBuilder.RenameTable(
                name: "ShiftPlans",
                newName: "ShiftPlans",
                newSchema: "логистика_и_смены");

            migrationBuilder.RenameTable(
                name: "ShiftAssignments",
                newName: "ShiftAssignments",
                newSchema: "логистика_и_смены");

            migrationBuilder.RenameTable(
                name: "ServiceAreaZones",
                newName: "ServiceAreaZones",
                newSchema: "логистика_и_смены");

            migrationBuilder.RenameTable(
                name: "ServiceAreaZoneCouriers",
                newName: "ServiceAreaZoneCouriers",
                newSchema: "логистика_и_смены");

            migrationBuilder.RenameTable(
                name: "ScheduleTypes",
                newName: "ScheduleTypes",
                newSchema: "заказы");

            migrationBuilder.RenameTable(
                name: "ScheduledReportJobs",
                newName: "ScheduledReportJobs",
                newSchema: "аналитика_и_crm");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "Roles",
                newSchema: "пользователи_и_доступ");

            migrationBuilder.RenameTable(
                name: "Reviews",
                newName: "Reviews",
                newSchema: "аналитика_и_crm");

            migrationBuilder.RenameTable(
                name: "ReportStatuses",
                newName: "ReportStatuses",
                newSchema: "аналитика_и_crm");

            migrationBuilder.RenameTable(
                name: "Reports",
                newName: "Reports",
                newSchema: "аналитика_и_crm");

            migrationBuilder.RenameTable(
                name: "PaymentTransactions",
                newName: "PaymentTransactions",
                newSchema: "биллинг_и_подписки");

            migrationBuilder.RenameTable(
                name: "PackageTypes",
                newName: "PackageTypes",
                newSchema: "заказы");

            migrationBuilder.RenameTable(
                name: "OrderTypes",
                newName: "OrderTypes",
                newSchema: "заказы");

            migrationBuilder.RenameTable(
                name: "OrderTimelineEvents",
                newName: "OrderTimelineEvents",
                newSchema: "заказы");

            migrationBuilder.RenameTable(
                name: "OrderStatuses",
                newName: "OrderStatuses",
                newSchema: "заказы");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "Orders",
                newSchema: "заказы");

            migrationBuilder.RenameTable(
                name: "OrderRouteStops",
                newName: "OrderRouteStops",
                newSchema: "заказы");

            migrationBuilder.RenameTable(
                name: "NotificationTypes",
                newName: "NotificationTypes",
                newSchema: "коммуникации");

            migrationBuilder.RenameTable(
                name: "Notifications",
                newName: "Notifications",
                newSchema: "коммуникации");

            migrationBuilder.RenameTable(
                name: "ManagerProfiles",
                newName: "ManagerProfiles",
                newSchema: "пользователи_и_доступ");

            migrationBuilder.RenameTable(
                name: "LogisticsHubs",
                newName: "LogisticsHubs",
                newSchema: "логистика_и_смены");

            migrationBuilder.RenameTable(
                name: "Logins",
                newName: "Logins",
                newSchema: "пользователи_и_доступ");

            migrationBuilder.RenameTable(
                name: "LeadStages",
                newName: "LeadStages",
                newSchema: "аналитика_и_crm");

            migrationBuilder.RenameTable(
                name: "LeadSources",
                newName: "LeadSources",
                newSchema: "аналитика_и_crm");

            migrationBuilder.RenameTable(
                name: "Leads",
                newName: "Leads",
                newSchema: "аналитика_и_crm");

            migrationBuilder.RenameTable(
                name: "FuelTypes",
                newName: "FuelTypes",
                newSchema: "автопарк");

            migrationBuilder.RenameTable(
                name: "FuelCompanies",
                newName: "FuelCompanies",
                newSchema: "автопарк");

            migrationBuilder.RenameTable(
                name: "FuelCardTypes",
                newName: "FuelCardTypes",
                newSchema: "автопарк");

            migrationBuilder.RenameTable(
                name: "FuelCardStatuses",
                newName: "FuelCardStatuses",
                newSchema: "автопарк");

            migrationBuilder.RenameTable(
                name: "FuelCards",
                newName: "FuelCards",
                newSchema: "автопарк");

            migrationBuilder.RenameTable(
                name: "DriveTypes",
                newName: "DriveTypes",
                newSchema: "автопарк");

            migrationBuilder.RenameTable(
                name: "CourierStatuses",
                newName: "CourierStatuses",
                newSchema: "пользователи_и_доступ");

            migrationBuilder.RenameTable(
                name: "CourierShifts",
                newName: "CourierShifts",
                newSchema: "логистика_и_смены");

            migrationBuilder.RenameTable(
                name: "CourierProfiles",
                newName: "CourierProfiles",
                newSchema: "пользователи_и_доступ");

            migrationBuilder.RenameTable(
                name: "CourierFuelCards",
                newName: "CourierFuelCards",
                newSchema: "автопарк");

            migrationBuilder.RenameTable(
                name: "CompanySubscriptions",
                newName: "CompanySubscriptions",
                newSchema: "биллинг_и_подписки");

            migrationBuilder.RenameTable(
                name: "Companies",
                newName: "Companies",
                newSchema: "пользователи_и_доступ");

            migrationBuilder.RenameTable(
                name: "CommunicationTemplates",
                newName: "CommunicationTemplates",
                newSchema: "коммуникации");

            migrationBuilder.RenameTable(
                name: "ClientStatuses",
                newName: "ClientStatuses",
                newSchema: "пользователи_и_доступ");

            migrationBuilder.RenameTable(
                name: "ClientSegments",
                newName: "ClientSegments",
                newSchema: "пользователи_и_доступ");

            migrationBuilder.RenameTable(
                name: "ClientProfiles",
                newName: "ClientProfiles",
                newSchema: "пользователи_и_доступ");

            migrationBuilder.RenameTable(
                name: "ClientNoteTypes",
                newName: "ClientNoteTypes",
                newSchema: "пользователи_и_доступ");

            migrationBuilder.RenameTable(
                name: "ClientNotes",
                newName: "ClientNotes",
                newSchema: "пользователи_и_доступ");

            migrationBuilder.RenameTable(
                name: "ChatRoomTypes",
                newName: "ChatRoomTypes",
                newSchema: "коммуникации");

            migrationBuilder.RenameTable(
                name: "ChatRooms",
                newName: "ChatRooms",
                newSchema: "коммуникации");

            migrationBuilder.RenameTable(
                name: "ChatQuickReplyTemplates",
                newName: "ChatQuickReplyTemplates",
                newSchema: "коммуникации");

            migrationBuilder.RenameTable(
                name: "ChatParticipants",
                newName: "ChatParticipants",
                newSchema: "коммуникации");

            migrationBuilder.RenameTable(
                name: "ChatMessages",
                newName: "ChatMessages",
                newSchema: "коммуникации");

            migrationBuilder.RenameTable(
                name: "BillingWebhookEvents",
                newName: "BillingWebhookEvents",
                newSchema: "биллинг_и_подписки");

            migrationBuilder.RenameTable(
                name: "BillingInvoices",
                newName: "BillingInvoices",
                newSchema: "биллинг_и_подписки");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "AuditLogs",
                newSchema: "аналитика_и_crm");

            migrationBuilder.RenameTable(
                name: "Addresses",
                newName: "Addresses",
                newSchema: "логистика_и_смены");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Vehicles",
                schema: "автопарк",
                newName: "Vehicles");

            migrationBuilder.RenameTable(
                name: "VehicleModels",
                schema: "автопарк",
                newName: "VehicleModels");

            migrationBuilder.RenameTable(
                name: "VehicleCategories",
                schema: "автопарк",
                newName: "VehicleCategories");

            migrationBuilder.RenameTable(
                name: "VehicleBrands",
                schema: "автопарк",
                newName: "VehicleBrands");

            migrationBuilder.RenameTable(
                name: "VehicleBodyTypes",
                schema: "автопарк",
                newName: "VehicleBodyTypes");

            migrationBuilder.RenameTable(
                name: "VehicleAssignments",
                schema: "автопарк",
                newName: "VehicleAssignments");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "пользователи_и_доступ",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "TransmissionTypes",
                schema: "автопарк",
                newName: "TransmissionTypes");

            migrationBuilder.RenameTable(
                name: "SupportTickets",
                schema: "коммуникации",
                newName: "SupportTickets");

            migrationBuilder.RenameTable(
                name: "SubscriptionPlans",
                schema: "биллинг_и_подписки",
                newName: "SubscriptionPlans");

            migrationBuilder.RenameTable(
                name: "ShiftStatuses",
                schema: "логистика_и_смены",
                newName: "ShiftStatuses");

            migrationBuilder.RenameTable(
                name: "ShiftPlans",
                schema: "логистика_и_смены",
                newName: "ShiftPlans");

            migrationBuilder.RenameTable(
                name: "ShiftAssignments",
                schema: "логистика_и_смены",
                newName: "ShiftAssignments");

            migrationBuilder.RenameTable(
                name: "ServiceAreaZones",
                schema: "логистика_и_смены",
                newName: "ServiceAreaZones");

            migrationBuilder.RenameTable(
                name: "ServiceAreaZoneCouriers",
                schema: "логистика_и_смены",
                newName: "ServiceAreaZoneCouriers");

            migrationBuilder.RenameTable(
                name: "ScheduleTypes",
                schema: "заказы",
                newName: "ScheduleTypes");

            migrationBuilder.RenameTable(
                name: "ScheduledReportJobs",
                schema: "аналитика_и_crm",
                newName: "ScheduledReportJobs");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: "пользователи_и_доступ",
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "Reviews",
                schema: "аналитика_и_crm",
                newName: "Reviews");

            migrationBuilder.RenameTable(
                name: "ReportStatuses",
                schema: "аналитика_и_crm",
                newName: "ReportStatuses");

            migrationBuilder.RenameTable(
                name: "Reports",
                schema: "аналитика_и_crm",
                newName: "Reports");

            migrationBuilder.RenameTable(
                name: "PaymentTransactions",
                schema: "биллинг_и_подписки",
                newName: "PaymentTransactions");

            migrationBuilder.RenameTable(
                name: "PackageTypes",
                schema: "заказы",
                newName: "PackageTypes");

            migrationBuilder.RenameTable(
                name: "OrderTypes",
                schema: "заказы",
                newName: "OrderTypes");

            migrationBuilder.RenameTable(
                name: "OrderTimelineEvents",
                schema: "заказы",
                newName: "OrderTimelineEvents");

            migrationBuilder.RenameTable(
                name: "OrderStatuses",
                schema: "заказы",
                newName: "OrderStatuses");

            migrationBuilder.RenameTable(
                name: "Orders",
                schema: "заказы",
                newName: "Orders");

            migrationBuilder.RenameTable(
                name: "OrderRouteStops",
                schema: "заказы",
                newName: "OrderRouteStops");

            migrationBuilder.RenameTable(
                name: "NotificationTypes",
                schema: "коммуникации",
                newName: "NotificationTypes");

            migrationBuilder.RenameTable(
                name: "Notifications",
                schema: "коммуникации",
                newName: "Notifications");

            migrationBuilder.RenameTable(
                name: "ManagerProfiles",
                schema: "пользователи_и_доступ",
                newName: "ManagerProfiles");

            migrationBuilder.RenameTable(
                name: "LogisticsHubs",
                schema: "логистика_и_смены",
                newName: "LogisticsHubs");

            migrationBuilder.RenameTable(
                name: "Logins",
                schema: "пользователи_и_доступ",
                newName: "Logins");

            migrationBuilder.RenameTable(
                name: "LeadStages",
                schema: "аналитика_и_crm",
                newName: "LeadStages");

            migrationBuilder.RenameTable(
                name: "LeadSources",
                schema: "аналитика_и_crm",
                newName: "LeadSources");

            migrationBuilder.RenameTable(
                name: "Leads",
                schema: "аналитика_и_crm",
                newName: "Leads");

            migrationBuilder.RenameTable(
                name: "FuelTypes",
                schema: "автопарк",
                newName: "FuelTypes");

            migrationBuilder.RenameTable(
                name: "FuelCompanies",
                schema: "автопарк",
                newName: "FuelCompanies");

            migrationBuilder.RenameTable(
                name: "FuelCardTypes",
                schema: "автопарк",
                newName: "FuelCardTypes");

            migrationBuilder.RenameTable(
                name: "FuelCardStatuses",
                schema: "автопарк",
                newName: "FuelCardStatuses");

            migrationBuilder.RenameTable(
                name: "FuelCards",
                schema: "автопарк",
                newName: "FuelCards");

            migrationBuilder.RenameTable(
                name: "DriveTypes",
                schema: "автопарк",
                newName: "DriveTypes");

            migrationBuilder.RenameTable(
                name: "CourierStatuses",
                schema: "пользователи_и_доступ",
                newName: "CourierStatuses");

            migrationBuilder.RenameTable(
                name: "CourierShifts",
                schema: "логистика_и_смены",
                newName: "CourierShifts");

            migrationBuilder.RenameTable(
                name: "CourierProfiles",
                schema: "пользователи_и_доступ",
                newName: "CourierProfiles");

            migrationBuilder.RenameTable(
                name: "CourierFuelCards",
                schema: "автопарк",
                newName: "CourierFuelCards");

            migrationBuilder.RenameTable(
                name: "CompanySubscriptions",
                schema: "биллинг_и_подписки",
                newName: "CompanySubscriptions");

            migrationBuilder.RenameTable(
                name: "Companies",
                schema: "пользователи_и_доступ",
                newName: "Companies");

            migrationBuilder.RenameTable(
                name: "CommunicationTemplates",
                schema: "коммуникации",
                newName: "CommunicationTemplates");

            migrationBuilder.RenameTable(
                name: "ClientStatuses",
                schema: "пользователи_и_доступ",
                newName: "ClientStatuses");

            migrationBuilder.RenameTable(
                name: "ClientSegments",
                schema: "пользователи_и_доступ",
                newName: "ClientSegments");

            migrationBuilder.RenameTable(
                name: "ClientProfiles",
                schema: "пользователи_и_доступ",
                newName: "ClientProfiles");

            migrationBuilder.RenameTable(
                name: "ClientNoteTypes",
                schema: "пользователи_и_доступ",
                newName: "ClientNoteTypes");

            migrationBuilder.RenameTable(
                name: "ClientNotes",
                schema: "пользователи_и_доступ",
                newName: "ClientNotes");

            migrationBuilder.RenameTable(
                name: "ChatRoomTypes",
                schema: "коммуникации",
                newName: "ChatRoomTypes");

            migrationBuilder.RenameTable(
                name: "ChatRooms",
                schema: "коммуникации",
                newName: "ChatRooms");

            migrationBuilder.RenameTable(
                name: "ChatQuickReplyTemplates",
                schema: "коммуникации",
                newName: "ChatQuickReplyTemplates");

            migrationBuilder.RenameTable(
                name: "ChatParticipants",
                schema: "коммуникации",
                newName: "ChatParticipants");

            migrationBuilder.RenameTable(
                name: "ChatMessages",
                schema: "коммуникации",
                newName: "ChatMessages");

            migrationBuilder.RenameTable(
                name: "BillingWebhookEvents",
                schema: "биллинг_и_подписки",
                newName: "BillingWebhookEvents");

            migrationBuilder.RenameTable(
                name: "BillingInvoices",
                schema: "биллинг_и_подписки",
                newName: "BillingInvoices");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                schema: "аналитика_и_crm",
                newName: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "Addresses",
                schema: "логистика_и_смены",
                newName: "Addresses");
        }
    }
}
