using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks; // Required for async operations
using avalonia_test.Models;    // Using the new Models namespace
using avalonia_test.Services;  // Using the new Services namespace
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace avalonia_test.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;
        private List<Machine> _allMachines = new();
        private string? _selectedMachineType;
        private string? _selectedMachineName;
        private string _projectText = "Проект 2";
        private string _headerText = "АксиОМА Контрол";
        private DateTimeOffset _startDate = DateTimeOffset.Now.AddDays(-7);
        private DateTimeOffset _endDate = DateTimeOffset.Now;

        private ObservableCollection<ISeries> _chartSeries = new();
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

        private ObservableCollection<ISeries> _timelineSeries = new();
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

        private ObservableCollection<ComponentValue> _currentComponentValues = new();
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
                    SelectedMachineName = null; // This will trigger its own updates
                    // UpdateHeaderText and UpdateProjectText are called by SelectedMachineName setter
                    // UpdateChartData, UpdateTimelineData, LoadComponentValues are also called by SelectedMachineName setter
                    // However, if SelectedMachineName does not change (e.g. remains null), these need to be called.
                    if (SelectedMachineName == null) // Explicitly call updates if name doesn't change
                    {
                        UpdateHeaderText();
                        UpdateProjectText();
                        _ = UpdateChartDataAsync(); // Fire and forget, or handle Task
                        _ = UpdateTimelineDataAsync();
                        _ = LoadComponentValuesAsync();
                    }
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
                    _ = UpdateChartDataAsync(); // Fire and forget, or handle Task
                    _ = UpdateTimelineDataAsync();
                    _ = LoadComponentValuesAsync();
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
                    _ = UpdateChartDataAsync();
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
                    _ = UpdateChartDataAsync();
                }
            }
        }

        // Constructor for designer (optional, if you use a DI framework, this might differ)
        public MainWindowViewModel() : this(new DataService()) // Default for designer or simple instantiation
        {
             // This constructor might be used by the XAML designer.
             // Ensure it can initialize without errors, perhaps with mock data if necessary.
            if (Avalonia.Controls.Design.IsDesignMode)
            {
                // Load mock data or default states for the designer
                LoadDesignData();
            }
        }


        public MainWindowViewModel(IDataService dataService)
        {
            _dataService = dataService;

            // Initialize Axes (consider moving to a method if complex)
            _yAxes = new Axis[]
            {
                new Axis { Labeler = val => val.ToString("P0"), MaxLimit = 1, MinLimit = 0, ForceStepToMin = true, MinStep = 0.2 }
            };
            _timelineXAxes = new Axis[]
            {
                new Axis { Labeler = value => new DateTime((long)value).ToString("HH:mm"), UnitWidth = TimeSpan.FromHours(1).Ticks, MinLimit = DateTime.Now.AddHours(-24).Ticks, MaxLimit = DateTime.Now.Ticks }
            };
            _timelineYAxes = new Axis[]
            {
                new Axis { Labels = new string[] { "" }, MaxLimit = 1, MinLimit = 0, IsVisible = false }
            };
            
            // Asynchronous initialization
            _ = InitializeViewModelAsync();
        }
        
        private void LoadDesignData()
        {
            MachineTypes.Add("Type A (Design)");
            MachineTypes.Add("Type B (Design)");
            _allMachines.Add(new Machine { Name = "Machine 1 (Design)", Type = "Type A (Design)" });
            UpdateMachineNames(); // Populate MachineNames
            SelectedMachineType = "Type A (Design)";
            SelectedMachineName = "Machine 1 (Design)";

            ChartSeries.Add(new StackedColumnSeries<double> { Values = new double[] { 0.3 }, Fill = new SolidColorPaint(SKColors.Gray), Name = "Выключен (Design)" });
            ChartSeries.Add(new StackedColumnSeries<double> { Values = new double[] { 0.5 }, Fill = new SolidColorPaint(SKColors.Orange), Name = "Нагрузка (Design)" });
            ChartSeries.Add(new StackedColumnSeries<double> { Values = new double[] { 0.2 }, Fill = new SolidColorPaint(SKColors.Green), Name = "Включен (Design)" });
        
            var designTime = DateTime.Now;
            TimelineSeries.Add(new StackedRowSeries<Models.TimeSpanPoint>
            {
                Values = new List<Models.TimeSpanPoint> { new Models.TimeSpanPoint(designTime.AddHours(-5).Ticks, TimeSpan.FromHours(2).Ticks) },
                Fill = new SolidColorPaint(SKColors.Black), Name = "Выключен (Design)"
            });

            CurrentComponentValues.Add(new ComponentValue { Name = "Temp (Design)", Value = 75.5 });
        }


        private async Task InitializeViewModelAsync()
        {
            await LoadMachineDataAsync();
            // Initial updates after loading machine data
            UpdateHeaderText();
            UpdateProjectText();
            await UpdateChartDataAsync();
            await UpdateTimelineDataAsync();
            await LoadComponentValuesAsync();
        }

        private async Task LoadMachineDataAsync()
        {
            var types = await _dataService.GetMachineTypesAsync();
            MachineTypes.Clear();
            foreach (var type in types)
            {
                MachineTypes.Add(type);
            }

            _allMachines = await _dataService.GetAllMachinesAsync();
            UpdateMachineNames(); // This is synchronous and depends on _allMachines
        }

        private void UpdateMachineNames()
        {
            MachineNames.Clear();
            if (_allMachines == null || !_allMachines.Any()) return;

            IEnumerable<Machine> filteredMachines = string.IsNullOrEmpty(SelectedMachineType)
                ? _allMachines
                : _allMachines.Where(m => m.Type == SelectedMachineType);

            foreach (var machine in filteredMachines)
            {
                if (!string.IsNullOrEmpty(machine.Name))
                {
                    MachineNames.Add(machine.Name);
                }
            }
        }

        private void UpdateProjectText()
        {
            ProjectText = !string.IsNullOrEmpty(SelectedMachineName) ? SelectedMachineName : "Проект 2";
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

        private async Task UpdateChartDataAsync()
        {
            ChartSeries.Clear();
            if (string.IsNullOrEmpty(SelectedMachineName))
            {
                ChartSeries.Add(new StackedColumnSeries<double> { Values = new double[] { 0 }, Fill = new SolidColorPaint(SKColors.Gray), Name = "Нет данных", StackGroup = 0 });
                return;
            }

            var startDateUtc = StartDate.UtcDateTime.Date;
            var endDateUtc = EndDate.UtcDateTime.Date.AddDays(1).AddSeconds(-1); // End of the selected day

            var logs = await _dataService.GetMachineStatusLogsAsync(SelectedMachineName, startDateUtc, endDateUtc);

            double totalDuration = logs.Sum(l => l.DurationSeconds);
            if (totalDuration == 0)
            {
                ChartSeries.Add(new StackedColumnSeries<double> { Values = new double[] { 0 }, Fill = new SolidColorPaint(SKColors.Gray), Name = "Нет данных", StackGroup = 0 });
                return;
            }

            double offDuration = logs.Where(l => l.Status == "Выключен").Sum(l => l.DurationSeconds) / totalDuration;
            double loadDuration = logs.Where(l => l.Status == "Нагрузка").Sum(l => l.DurationSeconds) / totalDuration;
            double onDuration = logs.Where(l => l.Status == "Включен").Sum(l => l.DurationSeconds) / totalDuration;

            ChartSeries.Add(new StackedColumnSeries<double> { Values = new double[] { offDuration }, Fill = new SolidColorPaint(SKColors.Black), Name = "Выключен", StackGroup = 0, DataLabelsPaint = new SolidColorPaint(SKColors.White), DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle, DataLabelsFormatter = point => point.Model.ToString("P0") });
            ChartSeries.Add(new StackedColumnSeries<double> { Values = new double[] { loadDuration }, Fill = new SolidColorPaint(SKColors.Orange), Name = "Нагрузка", StackGroup = 0, DataLabelsPaint = new SolidColorPaint(SKColors.Black), DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle, DataLabelsFormatter = point => point.Model.ToString("P0") });
            ChartSeries.Add(new StackedColumnSeries<double> { Values = new double[] { onDuration }, Fill = new SolidColorPaint(SKColors.Green), Name = "Включен", StackGroup = 0, DataLabelsPaint = new SolidColorPaint(SKColors.White), DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle, DataLabelsFormatter = point => point.Model.ToString("P0") });
        }

        private async Task UpdateTimelineDataAsync()
        {
            TimelineSeries.Clear();
            if (string.IsNullOrEmpty(SelectedMachineName)) return;

            var now = DateTime.Now; // Use consistent "now"
            var twentyFourHoursAgo = now.AddHours(-24);

            // Update X-Axis limits for timeline dynamically
            TimelineXAxes = new Axis[]
            {
                new Axis
                {
                    Labeler = value => new DateTime((long)value).ToString("HH:mm"),
                    UnitWidth = TimeSpan.FromHours(1).Ticks,
                    MinLimit = twentyFourHoursAgo.Ticks, // Dynamic start
                    MaxLimit = now.Ticks, // Dynamic end
                }
            };


            var statusLogs = await _dataService.GetTimelineStatusLogsAsync(SelectedMachineName, twentyFourHoursAgo, now);

            if (!statusLogs.Any())
            {
                TimelineSeries.Add(new StackedRowSeries<Models.TimeSpanPoint> { Values = new List<Models.TimeSpanPoint>(), Fill = new SolidColorPaint(SKColors.Gray), Name = "Нет данных" });
                return;
            }

            var offData = new List<Models.TimeSpanPoint>();
            var loadData = new List<Models.TimeSpanPoint>();
            var onData = new List<Models.TimeSpanPoint>();

            foreach (var log in statusLogs)
            {
                DateTime segmentStart = log.LogTime;
                DateTime segmentEnd = log.LogTime.AddSeconds(log.DurationSeconds);

                // Clamp segments to the 24-hour window
                if (segmentStart < twentyFourHoursAgo) segmentStart = twentyFourHoursAgo;
                if (segmentEnd > now) segmentEnd = now;

                if (segmentStart >= segmentEnd) continue; // Skip if segment is outside or zero duration after clamping

                double actualDurationSeconds = (segmentEnd - segmentStart).TotalSeconds;
                
                var point = new Models.TimeSpanPoint(segmentStart.Ticks, segmentEnd.Ticks);
                
                switch (log.Status)
                {
                    case "Выключен":
                        offData.Add(new Models.TimeSpanPoint(segmentStart.Ticks, actualDurationSeconds));
                        break;
                    case "Нагрузка":
                        loadData.Add(new Models.TimeSpanPoint(segmentStart.Ticks, actualDurationSeconds));
                        break;
                    case "Включен":
                        onData.Add(new Models.TimeSpanPoint(segmentStart.Ticks, actualDurationSeconds));
                        break;
                }
            }

            TimelineSeries.Add(new StackedRowSeries<Models.TimeSpanPoint> { Values = offData, Fill = new SolidColorPaint(SKColors.Black), Name = "Выключен", StackGroup = 0 });
            TimelineSeries.Add(new StackedRowSeries<Models.TimeSpanPoint> { Values = loadData, Fill = new SolidColorPaint(SKColors.Orange), Name = "Нагрузка", StackGroup = 0 });
            TimelineSeries.Add(new StackedRowSeries<Models.TimeSpanPoint> { Values = onData, Fill = new SolidColorPaint(SKColors.Green), Name = "Включен", StackGroup = 0 });
        }


        private async Task LoadComponentValuesAsync()
        {
            CurrentComponentValues.Clear();
            if (string.IsNullOrEmpty(SelectedMachineName)) return;

            var values = await _dataService.GetLatestComponentValuesAsync(SelectedMachineName);
            foreach (var value in values)
            {
                CurrentComponentValues.Add(value);
            }
        }
    }
}