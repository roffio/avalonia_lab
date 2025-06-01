using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using System;

// LiveChartsCore specific usings
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp; // Required for SKColors
using LiveChartsCore.Defaults; // Might be needed for ObservableValue or similar if you use them

namespace avalonia_test.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }
    }

    public class MainWindowViewModel : ViewModelBase
    {
        private List<Machine> _allMachines = new();
        private string? _selectedMachineType;
        private string? _selectedMachineName;
        private string _projectText = "Проект 2";
        private string _headerText = "АксиОМА Контрол";

        // Default date range: last 7 days from today
        private DateTimeOffset _startDate = DateTimeOffset.Now.AddDays(-7);
        private DateTimeOffset _endDate = DateTimeOffset.Now;

        // LiveCharts properties for overall usage chart
        private ObservableCollection<ISeries> _chartSeries;
        public ObservableCollection<ISeries> ChartSeries
        {
            get => _chartSeries;
            set => SetProperty(ref _chartSeries, value);
        }

        private Axis[] _yAxes;
        public Axis[] YAxes
        {
            get => _yAxes;
            set => SetProperty(ref _yAxes, value);
        }

        // LiveCharts properties for 24-hour timeline
        private ObservableCollection<ISeries> _timelineSeries;
        public ObservableCollection<ISeries> TimelineSeries
        {
            get => _timelineSeries;
            set => SetProperty(ref _timelineSeries, value);
        }

        private Axis[] _timelineXAxes;
        public Axis[] TimelineXAxes
        {
            get => _timelineXAxes;
            set => SetProperty(ref _timelineXAxes, value);
        }

        private Axis[] _timelineYAxes;
        public Axis[] TimelineYAxes
        {
            get => _timelineYAxes;
            set => SetProperty(ref _timelineYAxes, value);
        }

        // New property for component values table
        private ObservableCollection<ComponentValue> _currentComponentValues;
        public ObservableCollection<ComponentValue> CurrentComponentValues
        {
            get => _currentComponentValues;
            set => SetProperty(ref _currentComponentValues, value);
        }

        public ObservableCollection<string> MachineTypes { get; } = new();
        public ObservableCollection<string> MachineNames { get; } = new();

        public string? SelectedMachineType
        {
            get => _selectedMachineType;
            set
            {
                if (SetProperty(ref _selectedMachineType, value))
                {
                    UpdateMachineNames();
                    SelectedMachineName = null; // Reset machine name when type changes
                    UpdateHeaderText();
                    UpdateProjectText();
                    UpdateChartData(); // Update usage chart when machine type changes
                    UpdateTimelineData(); // Update timeline when machine type changes
                    LoadComponentValues(); // Load component values when machine type changes
                }
            }
        }

        public string? SelectedMachineName
        {
            get => _selectedMachineName;
            set
            {
                if (SetProperty(ref _selectedMachineName, value))
                {
                    UpdateHeaderText();
                    UpdateProjectText();
                    UpdateChartData(); // Update usage chart when machine name changes
                    UpdateTimelineData(); // Update timeline when machine name changes
                    LoadComponentValues(); // Load component values when machine name changes
                }
            }
        }

        public string ProjectText
        {
            get => _projectText;
            set => SetProperty(ref _projectText, value);
        }

        public string HeaderText
        {
            get => _headerText;
            set => SetProperty(ref _headerText, value);
        }

        public DateTimeOffset StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    UpdateChartData(); // Update usage chart when start date changes
                }
            }
        }

        public DateTimeOffset EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    UpdateChartData(); // Update usage chart when end date changes
                }
            }
        }

        public MainWindowViewModel()
        {
            // Initialize LiveCharts properties for overall usage chart
            _chartSeries = new ObservableCollection<ISeries>();
            _yAxes = new Axis[]
            {
                new Axis
                {
                    Labeler = val => val.ToString("P0"),
                    MaxLimit = 1,
                    MinLimit = 0,
                    ForceStepToMin = true,
                    MinStep = 0.2
                }
            };

            // Initialize LiveCharts properties for 24-hour timeline
            _timelineSeries = new ObservableCollection<ISeries>();
            _timelineXAxes = new Axis[]
            {
                new Axis
                {
                    Labeler = value => new DateTime((long)value).ToString("HH:mm"), // Format as HH:mm
                    UnitWidth = TimeSpan.FromHours(1).Ticks, // Each unit on the axis represents 1 hour
                    MinLimit = DateTime.Now.AddHours(-24).Ticks, // Start 24 hours ago
                    MaxLimit = DateTime.Now.Ticks, // End at current time
                    // You might want to adjust the visible range if the chart looks too dense.
                    // E.g., if you only want to see 4 hours at a time in the view
                    // SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) // Example for grid lines
                }
            };
            _timelineYAxes = new Axis[]
            {
                new Axis
                {
                    // This axis will represent the single "row" for the timeline
                    Labels = new string[] { "" }, // Empty label as there's only one row
                    MaxLimit = 1,
                    MinLimit = 0,
                    IsVisible = false // Hide the Y-axis labels and lines
                }
            };

            // Initialize new CurrentComponentValues collection
            _currentComponentValues = new ObservableCollection<ComponentValue>();

            LoadMachineData(); // Load initial machine data
            UpdateChartData(); // Initial usage chart data load
            UpdateTimelineData(); // Initial timeline chart data load
            LoadComponentValues(); // Initial component values load
        }

        private void LoadMachineData()
        {
            using var connection = new SqliteConnection("Data Source=app.db");
            connection.Open();

            // Load machine types
            var cmdTypes = new SqliteCommand("SELECT DISTINCT type FROM machines", connection);
            using var reader1 = cmdTypes.ExecuteReader();
            while (reader1.Read())
            {
                MachineTypes.Add(reader1.GetString(0));
            }

            // Load all machines (names and types)
            var cmdNames = new SqliteCommand("SELECT name, type FROM machines", connection);
            using var reader2 = cmdNames.ExecuteReader();
            while (reader2.Read())
            {
                _allMachines.Add(new Machine
                {
                    Name = reader2.GetString(0),
                    Type = reader2.GetString(1)
                });
            }

            UpdateMachineNames();
        }

        private void UpdateMachineNames()
        {
            MachineNames.Clear();

            if (_allMachines == null || _allMachines.Count == 0)
                return;

            IEnumerable<string> namesToShow;

            if (string.IsNullOrEmpty(SelectedMachineType))
            {
                // If no type is selected, show all machine names
                namesToShow = _allMachines.Select(m => m?.Name).Where(name => !string.IsNullOrEmpty(name))!;
            }
            else
            {
                // If a type is selected, filter names by type
                namesToShow = _allMachines.Where(m => m != null && m.Type == SelectedMachineType)
                                         .Select(m => m?.Name)
                                         .Where(name => !string.IsNullOrEmpty(name))!;
            }

            foreach (var name in namesToShow)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    MachineNames.Add(name);
                }
            }
        }

        private void UpdateProjectText()
        {
            ProjectText = !string.IsNullOrEmpty(SelectedMachineName)
                ? SelectedMachineName
                : "Проект 2";
        }

        private void UpdateHeaderText()
        {
            if (!string.IsNullOrEmpty(SelectedMachineType) && !string.IsNullOrEmpty(SelectedMachineName))
            {
                HeaderText = $"{SelectedMachineType} - {SelectedMachineName}";
            }
            else if (!string.IsNullOrEmpty(SelectedMachineType))
            {
                HeaderText = SelectedMachineType;
            }
            else
            {
                HeaderText = "АксиОМА Контрол";
            }
        }

        // --- LiveCharts Data Logic for overall usage chart ---
        private void UpdateChartData()
        {
            ChartSeries.Clear();

            if (string.IsNullOrEmpty(SelectedMachineName))
            {
                ChartSeries.Add(new StackedColumnSeries<double> { Values = new double[] { 0 }, Fill = new SolidColorPaint(SKColors.Gray), Name = "Нет данных", StackGroup = 0 });
                return;
            }

            var startDateUtc = StartDate.UtcDateTime.Date;
            var endDateUtc = EndDate.UtcDateTime.Date.AddDays(1).AddSeconds(-1);

            double totalDuration = 0;
            double offDuration = 0;
            double loadDuration = 0;
            double onDuration = 0;

            using var connection = new SqliteConnection("Data Source=app.db");
            connection.Open();

            var command = new SqliteCommand(
                "SELECT Status, DurationSeconds FROM MachineStatusLogs " +
                "WHERE MachineName = @machineName AND LogTime BETWEEN @startDate AND @endDate",
                connection);

            command.Parameters.AddWithValue("@machineName", SelectedMachineName);
            command.Parameters.AddWithValue("@startDate", startDateUtc.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@endDate", endDateUtc.ToString("yyyy-MM-dd HH:mm:ss"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var status = reader.GetString(0);
                var duration = reader.GetDouble(1);

                totalDuration += duration;
                switch (status)
                {
                    case "Выключен":
                        offDuration += duration;
                        break;
                    case "Нагрузка":
                        loadDuration += duration;
                        break;
                    case "Включен":
                        onDuration += duration;
                        break;
                }
            }

            if (totalDuration == 0)
            {
                ChartSeries.Add(new StackedColumnSeries<double> { Values = new double[] { 0 }, Fill = new SolidColorPaint(SKColors.Gray), Name = "Нет данных", StackGroup = 0 });
            }
            else
            {
                offDuration /= totalDuration;
                loadDuration /= totalDuration;
                onDuration /= totalDuration;

                ChartSeries.Add(new StackedColumnSeries<double>
                {
                    Values = new double[] { offDuration },
                    Fill = new SolidColorPaint(SKColors.Black),
                    Name = "Выключен",
                    StackGroup = 0,
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle,
                    DataLabelsFormatter = point => point.Model.ToString("P0")
                });

                ChartSeries.Add(new StackedColumnSeries<double>
                {
                    Values = new double[] { loadDuration },
                    Fill = new SolidColorPaint(SKColors.Orange),
                    Name = "Нагрузка",
                    StackGroup = 0,
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle,
                    DataLabelsFormatter = point => point.Model.ToString("P0")
                });

                ChartSeries.Add(new StackedColumnSeries<double>
                {
                    Values = new double[] { onDuration },
                    Fill = new SolidColorPaint(SKColors.Green),
                    Name = "Включен",
                    StackGroup = 0,
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle,
                    DataLabelsFormatter = point => point.Model.ToString("P0")
                });
            }
        }

        // --- LiveCharts Data Logic for 24-hour timeline ---
        private void UpdateTimelineData()
        {
            TimelineSeries.Clear();

            if (string.IsNullOrEmpty(SelectedMachineName))
            {
                // If no machine selected, clear the timeline or show a "No data" message
                return;
            }

            using var connection = new SqliteConnection("Data Source=app.db");
            connection.Open();

            // Calculate the time 24 hours ago from now
            var twentyFourHoursAgo = DateTime.Now.AddHours(-24);
            var now = DateTime.Now;

            // Retrieve status logs for the last 24 hours for the selected machine
            // Order by LogTime to process chronologically
            var command = new SqliteCommand(
                "SELECT Status, LogTime, DurationSeconds FROM MachineStatusLogs " +
                "WHERE MachineName = @machineName AND LogTime BETWEEN @startDate AND @endDate " +
                "ORDER BY LogTime ASC",
                connection);

            command.Parameters.AddWithValue("@machineName", SelectedMachineName);
            command.Parameters.AddWithValue("@startDate", twentyFourHoursAgo.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@endDate", now.ToString("yyyy-MM-dd HH:mm:ss"));

            var statusLogs = new List<(string Status, DateTime LogTime, double DurationSeconds)>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var status = reader.GetString(0);
                var logTime = DateTime.Parse(reader.GetString(1)); // Parse the string to DateTime
                var duration = reader.GetDouble(2);
                statusLogs.Add((status, logTime, duration));
            }

            // Create series for each status type (Выключен, Нагрузка, Включен)
            var offData = new List<TimeSpanPoint>();
            var loadData = new List<TimeSpanPoint>();
            var onData = new List<TimeSpanPoint>();

            // The timeline will be a single stacked row.
            // We need to calculate the "start" and "end" for each segment of status.
            // LiveCharts `TimeSpanPoint` takes Value (duration) and Time (start time of the segment).

            // If no data, ensure the chart is empty or shows a default message
            if (!statusLogs.Any())
            {
                // Optionally add a "No data" series if you want to explicitly show something
                TimelineSeries.Add(new StackedRowSeries<TimeSpanPoint>
                {
                    Values = new List<TimeSpanPoint>(),
                    Fill = new SolidColorPaint(SKColors.Gray),
                    Name = "Нет данных"
                });
                return;
            }

            DateTime currentTimePointer = twentyFourHoursAgo;

            foreach (var log in statusLogs)
            {
                DateTime segmentStart = log.LogTime;
                DateTime segmentEnd = log.LogTime.AddSeconds(log.DurationSeconds);

                // Ensure the segment falls within the 24-hour window
                if (segmentStart < twentyFourHoursAgo) segmentStart = twentyFourHoursAgo;
                if (segmentEnd > now) segmentEnd = now;

                if (segmentStart >= segmentEnd) continue; // Skip if invalid segment

                double actualDurationSeconds = (segmentEnd - segmentStart).TotalSeconds;

                // Add to the appropriate list
                switch (log.Status)
                {
                    case "Выключен":
                        offData.Add(new TimeSpanPoint(segmentStart.Ticks, actualDurationSeconds));
                        break;
                    case "Нагрузка":
                        loadData.Add(new TimeSpanPoint(segmentStart.Ticks, actualDurationSeconds));
                        break;
                    case "Включен":
                        onData.Add(new TimeSpanPoint(segmentStart.Ticks, actualDurationSeconds));
                        break;
                }
                currentTimePointer = segmentEnd;
            }

            // Add the series to the chart
            TimelineSeries.Add(new StackedRowSeries<TimeSpanPoint>
            {
                Values = offData,
                Fill = new SolidColorPaint(SKColors.Black),
                Name = "Выключен",
                StackGroup = 0 // All series belong to the same stack group for a single stacked row
            });

            TimelineSeries.Add(new StackedRowSeries<TimeSpanPoint>
            {
                Values = loadData,
                Fill = new SolidColorPaint(SKColors.Orange),
                Name = "Нагрузка",
                StackGroup = 0
            });

            TimelineSeries.Add(new StackedRowSeries<TimeSpanPoint>
            {
                Values = onData,
                Fill = new SolidColorPaint(SKColors.Green),
                Name = "Включен",
                StackGroup = 0
            });
        }

        // New method to load component values
        private void LoadComponentValues()
        {
            CurrentComponentValues.Clear();

            if (string.IsNullOrEmpty(SelectedMachineName))
            {
                return;
            }

            using var connection = new SqliteConnection("Data Source=app.db");
            connection.Open();

            // Select the latest component values for the selected machine
            // This query assumes you want the most recent value for each component for the given machine.
            // If you have multiple readings per component for the same timestamp, you might need to adjust.
            // Here, we're selecting the latest overall for each component name.
            var command = new SqliteCommand(
        "SELECT T1.ComponentName, T1.Value, T1.Timestamp " +
        "FROM ComponentValues AS T1 " +
        "INNER JOIN ( " +
        "    SELECT ComponentName, MAX(Timestamp) AS MaxTimestamp " +
        "    FROM ComponentValues " +
        "    WHERE MachineName = @machineName " +
        "    GROUP BY ComponentName " +
        ") AS T2 " +
        "ON T1.ComponentName = T2.ComponentName AND T1.Timestamp = T2.MaxTimestamp " +
        "WHERE T1.MachineName = @machineName " + // Important: Filter by machineName in the outer query too
        "ORDER BY T1.ComponentName",
        connection);


            command.Parameters.AddWithValue("@machineName", SelectedMachineName);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                CurrentComponentValues.Add(new ComponentValue
                {
                    Name = reader.GetString(0),
                    Value = reader.GetDouble(1)
                    // Timestamp could be added here if needed for display
                });
            }
        }


        private class Machine
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
        }

        // Helper class for LiveCharts TimeSpanPoint
        public class TimeSpanPoint
        {
            public long Time { get; set; } // The start time of the segment in Ticks
            public double Value { get; set; } // The duration of the segment in seconds

            public TimeSpanPoint(long time, double value)
            {
                Time = time;
                Value = value;
            }
        }

        // New class to represent a component value
        public class ComponentValue
        {
            public string Name { get; set; } = string.Empty;
            public double Value { get; set; }
        }
    }
}