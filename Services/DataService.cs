using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using avalonia_test.Models; // Ensure this using statement is present

namespace avalonia_test.Services
{
    public class DataService : IDataService
    {
        private readonly string _connectionString = "Data Source=app.db";

        public async Task<List<string>> GetMachineTypesAsync()
        {
            var machineTypes = new List<string>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqliteCommand("SELECT DISTINCT type FROM machines", connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                machineTypes.Add(reader.GetString(0));
            }
            return machineTypes;
        }

        public async Task<List<Machine>> GetAllMachinesAsync()
        {
            var machines = new List<Machine>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqliteCommand("SELECT name, type FROM machines", connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                machines.Add(new Machine
                {
                    Name = reader.GetString(0),
                    Type = reader.GetString(1)
                });
            }
            return machines;
        }

        public async Task<List<MachineStatusLog>> GetMachineStatusLogsAsync(string machineName, DateTime startDateUtc, DateTime endDateUtc)
        {
            var logs = new List<MachineStatusLog>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqliteCommand(
                "SELECT Status, DurationSeconds FROM MachineStatusLogs " +
                "WHERE MachineName = @machineName AND LogTime BETWEEN @startDate AND @endDate",
                connection);

            command.Parameters.AddWithValue("@machineName", machineName);
            command.Parameters.AddWithValue("@startDate", startDateUtc.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@endDate", endDateUtc.ToString("yyyy-MM-dd HH:mm:ss"));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new MachineStatusLog
                {
                    Status = reader.GetString(0),
                    DurationSeconds = reader.GetDouble(1)
                    // LogTime is not selected in this specific query as per original,
                    // but good to have in the model if other queries need it.
                });
            }
            return logs;
        }

        public async Task<List<MachineStatusLog>> GetTimelineStatusLogsAsync(string machineName, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            var logs = new List<MachineStatusLog>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqliteCommand(
                "SELECT Status, LogTime, DurationSeconds FROM MachineStatusLogs " +
                "WHERE MachineName = @machineName AND LogTime BETWEEN @startDate AND @endDate " +
                "ORDER BY LogTime ASC",
                connection);

            command.Parameters.AddWithValue("@machineName", machineName);
            command.Parameters.AddWithValue("@startDate", startTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@endDate", endTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new MachineStatusLog
                {
                    Status = reader.GetString(0),
                    LogTime = DateTime.Parse(reader.GetString(1)), // Ensure proper parsing
                    DurationSeconds = reader.GetDouble(2)
                });
            }
            return logs;
        }

        public async Task<List<ComponentValue>> GetLatestComponentValuesAsync(string machineName)
        {
            var componentValues = new List<ComponentValue>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

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

            command.Parameters.AddWithValue("@machineName", machineName);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                componentValues.Add(new ComponentValue
                {
                    Name = reader.GetString(0),
                    Value = reader.GetDouble(1)
                    // Timestamp is selected but not used in the ComponentValue model in the original code
                });
            }
            return componentValues;
        }
    }
}