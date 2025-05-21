using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCleanRoom.Models
{
    [Table("SensorConfig")]
    public class SensorConfig
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public int SensorConfigID { get; set; }
        [ForeignKey("SensorInfo")]
        public int  SensorInfoID { get; set; }
        public SensorInfo SensorInfo { get; set; }
         
        public decimal? MinValidValue { get; set; }
        public decimal? MaxValidValue { get; set; }
        public int? ScanInterval { get; set; }
        public bool IsMonitored { get; set; } = true;
        public bool RequestConvertData { get; set; } = false;
        public decimal? LowAlertThreshold { get; set; }
        public decimal? HighAlertThreshold { get; set; }
    }
}
