using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;

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
        public ObservableCollection<string> MachineTypes { get; set; } = new();
        public ObservableCollection<string> MachineNames { get; set; } = new();

        public MainWindowViewModel()
        {
            LoadMachineData();
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

            var cmdNames = new SqliteCommand("SELECT DISTINCT name FROM machines", connection);
            using var reader2 = cmdNames.ExecuteReader();
            while (reader2.Read())
            {
                MachineNames.Add(reader2.GetString(0));
            }
        }
    }
}
