using System.Threading.Tasks;

namespace APIDeliveryCRM.Interfaces
{
    public interface IAuditService
    {
        /// <summary>Запись в журнал аудита (действия логиста и др.).</summary>
        Task LogAsync(
            int companyId,
            int? userId,
            string tableName,
            int recordId,
            string action,
            string? description = null,
            string? fieldName = null,
            string? oldValue = null,
            string? newValue = null,
            string? ipAddress = null);
    }
}
