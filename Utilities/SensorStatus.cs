using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCleanRoom.Utilities
{
    // Enum định nghĩa các loại vấn đề sensor
    public enum SensorIssueType
    {
        MissingData,
        NoConfig,
        ProcessingError,
        ConnectionLost,
        ConnectionRestored,
        BelowValidRange,
        AboveValidRange,
        BelowThreshold,
        AboveThreshold,
        ValueNormalized,
        Flickering,
        FrequentDisconnection,
        NoData
    }
    // Enum định nghĩa trạng thái sensor
    public enum SensorStatus
    {
        Normal,
        Warning,
        Error,
        Recovered
    }
}
