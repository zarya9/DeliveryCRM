using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class Role
    {
        [Key]
        public int ID_Role { get; set; }
        public string Name { get; set; }
    }
}
