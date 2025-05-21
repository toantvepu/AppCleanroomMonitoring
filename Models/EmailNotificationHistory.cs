using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCleanRoom.Models
{
    [Table("EmailNotificationHistory", Schema = "dbo")]
    public class EmailNotificationHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long NotificationID { get; set; }

        public long? SensorInfoID { get; set; }
        public string NotificationType { get; set; } 
        public string RecipientEmail { get; set; }

        public string NotificationContent { get; set; } 
        public bool? SentSuccessfully { get; set; }
         
        public DateTime? SentTime { get; set; }
 
    }
}
