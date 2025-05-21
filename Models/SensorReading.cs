using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanroomMonitoring.Software.Models
{
    [Table("SensorReading")]
    public class SensorReading
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public long ReadingID { get; set; }
        public int? SensorInfoID { get; set; }
        public decimal? ReadingValue { get; set; }
        public DateTime ReadingTime { get; set; } = DateTime.Now;
        public bool IsValid { get; set; } = true;
    }
}
