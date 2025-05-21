using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCleanRoom.Models
{
    /// <summary>
    /// Thông tin chi tiết về thiết bị cảm biến được lắp đặt
    /// </summary>
    [Table("SensorInfo")]
    public class SensorInfo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public int SensorInfoID { get; set; }
        
        public int RoomID { get; set; }
        public int SensorTypeID { get; set; }
        public string SensorName { get; set; }
        public int? ModbusAddress { get; set; }
        public string IpAddress { get; set; }
      
        public string Phase { get; set; }
        public bool  IsActive { get; set; } = true;
   
        public string COMMENT { get; set; }
       
      

        

    }
}
