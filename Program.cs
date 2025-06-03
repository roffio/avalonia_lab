using Avalonia;
using System;
using LiveChartsCore; // Required for LiveCharts.Configure
using LiveChartsCore.Kernel; // Required for Coordinate
using avalonia_test.ViewModels; // Required to access MainWindowViewModel.TimeSpanPoint

namespace avalonia_test 
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            
            LiveCharts.Configure(config =>
                config.HasMap<MainWindowViewModel.TimeSpanPoint>((model, index) =>
                    new Coordinate(
                        model.Value,  
                        model.Time
                    ))
            );

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>() 
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}