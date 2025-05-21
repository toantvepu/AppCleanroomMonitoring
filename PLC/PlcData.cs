using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCleanRoom.PLC
{
    public class PlcData
    {
        public string IpAddress { get; set; }
        public int ThanhGhi { get; set; }
        public decimal DuLieu { get; set; }
        public bool HasError { get; set; } =false;
    }
}
