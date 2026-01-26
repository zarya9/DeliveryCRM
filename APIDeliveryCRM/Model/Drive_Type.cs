using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class VehicleDriveType
    {
        [Key]
        public int ID_DriveType { get; set; }
        public string Name { get; set; }
    }
}
