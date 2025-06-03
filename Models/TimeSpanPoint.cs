namespace avalonia_test.Models
{
    public class TimeSpanPoint
    {
        public long Time { get; set; } // Represents the start ticks of the event
        public double Value { get; set; } // Represents the duration in seconds

        public TimeSpanPoint(long time, double value)
        {
            Time = time;
            Value = value;
        }
    }
}