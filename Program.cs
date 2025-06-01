using Avalonia;
using System;
using LiveChartsCore; // Required for LiveCharts.Configure
using LiveChartsCore.Kernel; // Required for Coordinate
using avalonia_test.ViewModels; // Required to access MainWindowViewModel.TimeSpanPoint

namespace avalonia_test // Ensure this namespace matches your project
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            // Configure LiveCharts Mappers
            LiveCharts.Configure(config =>
                config.HasMap<MainWindowViewModel.TimeSpanPoint>((model, index) =>
                    // For StackedRowSeries:
                    // The first parameter is the PrimaryValue (length of the bar/segment).
                    // The second parameter is the SecondaryValue (position on the axis).
                    new Coordinate(
                        model.Value,   // Corresponds to the duration (PrimaryValue)
                        model.Time     // Corresponds to the start ticks (SecondaryValue)
                    ))
                // You can add other mappers here if needed for other custom types
            );

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>() // Assuming you have an App.axaml or App.cs
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
                // .UseReactiveUI(); // If you are using ReactiveUI
    }
}