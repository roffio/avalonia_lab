using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using avalonia_test.Models;

namespace avalonia_test.Services
{
    public interface IDataService
    {
        Task<List<string>> GetMachineTypesAsync();
        Task<List<Machine>> GetAllMachinesAsync();
        Task<List<MachineStatusLog>> GetMachineStatusLogsAsync(string machineName, DateTime startDateUtc, DateTime endDateUtc);
        Task<List<MachineStatusLog>> GetTimelineStatusLogsAsync(string machineName, DateTime startTimeUtc, DateTime endTimeUtc);
        Task<List<ComponentValue>> GetLatestComponentValuesAsync(string machineName);
    }
}