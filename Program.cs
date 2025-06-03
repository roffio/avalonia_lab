using Avalonia;
using System;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using avalonia_test.Models;   // Updated: To access Models.TimeSpanPoint
using avalonia_test.Services; // Optional: For global service registration with DI
using avalonia_test.ViewModels; // Optional: If MainWindowViewModel is instantiated here with DI

namespace avalonia_test
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            LiveCharts.Configure(config =>
                config.HasMap<Models.TimeSpanPoint>((model, index) =>
                    new Coordinate(
                        x: model.Value, 
                        y: model.Time    
                    ))
            );

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .SetupViewModels(); 
    }
    public static class AppBuilderExtensions
    {
        public static AppBuilder SetupViewModels(this AppBuilder builder)
        {
            return builder;
        }
    }
}