using System;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;

namespace APIDeliveryCRM.Services
{
    public class AuditService : IAuditService
    {
        private readonly ContextDB _context;

        public AuditService(ContextDB context)
        {
            _context = context;
        }

        public async Task LogAsync(
            int companyId,
            int? userId,
            string tableName,
            int recordId,
            string action,
            string? description = null,
            string? fieldName = null,
            string? oldValue = null,
            string? newValue = null,
            string? ipAddress = null)
        {
            var row = new AuditLog
            {
                Company_id = companyId,
                User_id = userId,
                TableName = tableName.Length > 100 ? tableName[..100] : tableName,
                RecordId = recordId,
                Action = action.Length > 20 ? action[..20] : action,
                Description = description != null && description.Length > 500 ? description[..500] : description,
                FieldName = fieldName != null && fieldName.Length > 500 ? fieldName[..500] : fieldName,
                OldValue = oldValue != null && oldValue.Length > 1000 ? oldValue[..1000] : oldValue,
                NewValue = newValue != null && newValue.Length > 1000 ? newValue[..1000] : newValue,
                IPAddress = ipAddress != null && ipAddress.Length > 50 ? ipAddress[..50] : ipAddress,
                Created_at = DateTime.UtcNow
            };

            _context.AuditLogs.Add(row);
            await _context.SaveChangesAsync();
        }
    }
}
