using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCleanRoom.Models
{
    [Table("ErrorLog", Schema = "dbo")]
    public class ErrorLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime? ErrorTime { get; set; } 
        public string ErrorSource { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorType { get; set; }
        public string StackTrace { get; set; }
    }
}
