using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCleanRoom.Models
{
    [Table("SensorConnectionStatus", Schema = "dbo")]
    public class SensorConnectionStatus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SensorConnectionStatusId { get; set; }
        public int SensorInfoID { get; set; }
        public bool IsConnected { get; set; }
        public DateTime? LastConnectionTime { get; set; }
        public DateTime? LastDisconnectionTime { get; set; }
        public int DisconnectionCount { get; set; }
        public string LastIssueType { get; set; }
        public string LastIssueDescription { get; set; }

        // Khóa ngoại
        public virtual SensorInfo SensorInfo { get; set; }

    }
}
