using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCleanRoom.Models
{
    [Table("AlertHistory", Schema = "dbo")]
    public class AlertHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AlertID { get; set; }

        public int  SensorInfoID { get; set; }
        public DateTime? AlertTime { get; set; }

         
        public string AlertType { get; set; } 
        public string AlertMessage { get; set; }
          
        public decimal? AlertValue { get; set; }
        public bool  IsHandled { get; set; }
    }
}
