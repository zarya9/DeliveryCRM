using System.ComponentModel.DataAnnotations;

namespace APIDeliveryCRM.Model
{
    public class TransmissionType
    {
        [Key]
        public int ID_TransmisType { get; set; }
        public string Name { get; set; }
    }
}
