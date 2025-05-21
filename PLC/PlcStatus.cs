using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCleanRoom.PLC
{
    /// <summary>
    /// Lớp PlcStatus để quản lý trạng thái kết nối các PLC
    /// </summary>
    public class PlcStatus
    {
        public string IpAddress { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime NextRetryTime { get; set; }
        public int FailureCount { get; set; }
        public PlcStatus(string ipAddress)
        {
            IpAddress = ipAddress;
            IsAvailable = true;
            NextRetryTime = DateTime.Now;
            FailureCount = 0;
        }
        public void MarkAsFailed()
        {
            IsAvailable = false;
            FailureCount++;
            if (FailureCount >= 5) {
                NextRetryTime = DateTime.Now.AddMinutes(5);
            }
            else {
                // Thử lại sau số giây bằng với số lần thất bại
                NextRetryTime = DateTime.Now.AddSeconds(FailureCount * 30);
            }
        }
        public void MarkAvailable()
        {
            IsAvailable = true;
            FailureCount = 0;
            NextRetryTime = DateTime.Now;
        }
        public bool ShouldRetry()
        {
            return DateTime.Now >= NextRetryTime;
        }
    }
}
