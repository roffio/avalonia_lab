using System;

namespace avalonia_test.Models
{
    public class MachineStatusLog
    {
        public string Status { get; set; } = string.Empty;
        public DateTime LogTime { get; set; }
        public double DurationSeconds { get; set; }
    }
}