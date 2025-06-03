using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using System;

using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp; 
using LiveChartsCore.Defaults; 

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
                    SelectedMachineName = null; 
                    UpdateHeaderText();
                    UpdateProjectText();
                    UpdateChartData(); 
                    UpdateTimelineData(); 
                    LoadComponentValues(); 
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
                    UpdateChartData(); 
                    UpdateTimelineData(); 
                    LoadComponentValues(); 
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
                    UpdateChartData(); 
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
                    UpdateChartData(); 
                }
            }
        }

        public MainWindowViewModel()
        {
            
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

        
            _timelineSeries = new ObservableCollection<ISeries>();
            _timelineXAxes = new Axis[]
            {
                new Axis
                {
                    Labeler = value => new DateTime((long)value).ToString("HH:mm"), 
                    UnitWidth = TimeSpan.FromHours(1).Ticks, 
                    MinLimit = DateTime.Now.AddHours(-24).Ticks, 
                    MaxLimit = DateTime.Now.Ticks, 
                }
            };
            _timelineYAxes = new Axis[]
            {
                new Axis
                {
                    Labels = new string[] { "" }, 
                    MaxLimit = 1,
                    MinLimit = 0,
                    IsVisible = false 
                }
            };

            _currentComponentValues = new ObservableCollection<ComponentValue>();

            LoadMachineData(); 
            UpdateChartData(); 
            UpdateTimelineData(); 
            LoadComponentValues(); 
        }

        private void LoadMachineData()
        {
            using var connection = new SqliteConnection("Data Source=app.db");
            connection.Open();

            var cmdTypes = new SqliteCommand("SELECT DISTINCT type FROM machines", connection);
            using var reader1 = cmdTypes.ExecuteReader();
            while (reader1.Read())
            {
                MachineTypes.Add(reader1.GetString(0));
            }

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
                namesToShow = _allMachines.Select(m => m?.Name).Where(name => !string.IsNullOrEmpty(name))!;
            }
            else
            {
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

        private void UpdateTimelineData()
        {
            TimelineSeries.Clear();

            if (string.IsNullOrEmpty(SelectedMachineName))
            {
                return;
            }

            using var connection = new SqliteConnection("Data Source=app.db");
            connection.Open();

            var twentyFourHoursAgo = DateTime.Now.AddHours(-24);
            var now = DateTime.Now;


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
                var logTime = DateTime.Parse(reader.GetString(1)); 
                var duration = reader.GetDouble(2);
                statusLogs.Add((status, logTime, duration));
            }

            var offData = new List<TimeSpanPoint>();
            var loadData = new List<TimeSpanPoint>();
            var onData = new List<TimeSpanPoint>();

            if (!statusLogs.Any())
            {
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

                if (segmentStart < twentyFourHoursAgo) segmentStart = twentyFourHoursAgo;
                if (segmentEnd > now) segmentEnd = now;

                if (segmentStart >= segmentEnd) continue;

                double actualDurationSeconds = (segmentEnd - segmentStart).TotalSeconds;

                
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

            TimelineSeries.Add(new StackedRowSeries<TimeSpanPoint>
            {
                Values = offData,
                Fill = new SolidColorPaint(SKColors.Black),
                Name = "Выключен",
                StackGroup = 0 
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

        private void LoadComponentValues()
        {
            CurrentComponentValues.Clear();

            if (string.IsNullOrEmpty(SelectedMachineName))
            {
                return;
            }

            using var connection = new SqliteConnection("Data Source=app.db");
            connection.Open();

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
        "WHERE T1.MachineName = @machineName " + 
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
                });
            }
        }


        private class Machine
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
        }

        public class TimeSpanPoint
        {
            public long Time { get; set; } 
            public double Value { get; set; } 

            public TimeSpanPoint(long time, double value)
            {
                Time = time;
                Value = value;
            }
        }

        // component value
        public class ComponentValue
        {
            public string Name { get; set; } = string.Empty;
            public double Value { get; set; }
        }
    }
}