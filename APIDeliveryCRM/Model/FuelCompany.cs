using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class FuelCompany
    {
        [Key]
        public int ID_Company { get; set; }
        public string Name { get; set; }
        public string PhoneManager { get; set; }
        public decimal DiscountPercent { get; set; }
        public bool IsPreferred { get; set; }
    }
}
