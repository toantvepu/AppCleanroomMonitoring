using AppCleanRoom.Models;
using CleanroomMonitoring.Software.DataContext;
using CleanroomMonitoring.Software.Models;
using CleanroomMonitoring.Software.Utilities;
using NModbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data.Entity;
using System.Threading;
using System.Data.Entity.Migrations;

namespace AppCleanRoom.PLC
{
    public class SensorReadDataService
    {
        /*
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<SensorReadDataService> _logger;
        private readonly SemaphoreSlim _semaphore;
        private const int MaxConcurrentConnections = 10;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly CleanroomDbContext _dbContext;
        public SensorReadDataService(IServiceScopeFactory serviceScopeFactory, ILogger<SensorReadDataService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _semaphore = new SemaphoreSlim(MaxConcurrentConnections);
            _cancellationTokenSource = new CancellationTokenSource();
            _dbContext = new CleanroomDbContext();
        }

        public void Start()
        {
            Task.Run(() => ExecuteAsync(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting sensor data service.");
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    await ReadAndSaveSensorDataAsync(stoppingToken);
                    _logger.LogInformation("Sensor data collected successfully at {Time}", DateTimeOffset.Now);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "An error occurred while collecting sensor data.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ReadAndSaveSensorDataAsync(CancellationToken stoppingToken)
        {
            var scope = _serviceScopeFactory.CreateScope(); 

            var sensorConfigs = await _dbContext.SensorConfigs.ToListAsync();
            var groupedConfigs = GroupSensorConfigs(sensorConfigs);

            var tasks = groupedConfigs.Select(group => ProcessSensorGroupAsync(group, stoppingToken));
            await Task.WhenAll(tasks);

            await _dbContext.SaveChangesAsync(stoppingToken);
        }

        private IEnumerable<List<SensorConfig>> GroupSensorConfigs(IEnumerable<SensorConfig> sensorConfigs)
        {
            return sensorConfigs
                .OrderBy(c => c.ModbusAddress)
                .GroupBy(c => c.ModbusAddress / 10)
                .Select(group => group.ToList());
        }

        private async Task ProcessSensorGroupAsync(List<SensorConfig> sensorGroup, CancellationToken stoppingToken)
        {
            await _semaphore.WaitAsync(stoppingToken);
            try {
                  var client = new TcpClient();
                var factory = new ModbusFactory();
                var master = factory.CreateMaster(client);

                var connectTask = client.ConnectAsync(sensorGroup.First().IpAddress, sensorGroup.First().Port);
                if (await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(5), stoppingToken)) != connectTask) {
                    throw new TimeoutException("Connection to Modbus server timed out.");
                }

                ushort startAddress = (ushort)sensorGroup.Min(c => c.ModbusAddress);
                ushort numRegisters = (ushort)(sensorGroup.Max(c => c.ModbusAddress) - startAddress + 1);
                ushort[] response = master.ReadHoldingRegisters(1, startAddress, numRegisters);

                foreach (var config in sensorGroup) {
                    ushort offset = (ushort)(config.ModbusAddress - startAddress);
                    decimal readingValue = response[offset] / (config.RequestConvertData ? 100.0m : 1.0m);
                    bool isValid = await ValidateReadingAsync(config.SensorConfigID, readingValue);

                    using var scope = _serviceScopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<dbDataContext>();
                    dbContext.SensorReadings.Add(new SensorReading {
                        SensorInfoID = config.SensorInfoID.Value,
                        ReadingValue = readingValue,
                        ReadingTime = DateTime.Now,
                        IsValid = isValid
                    });

                    await UpdateSensorStatusAsync(config, isSuccess: true, errorMessage: null, retryCount: 0);
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error processing sensor group.");
                foreach (var config in sensorGroup) {
                    await UpdateSensorStatusAsync(config, isSuccess: false, errorMessage: ex.Message, retryCount: 0);
                }
            }
            finally {
                _semaphore.Release();
            }
        }

        private async Task<bool> ValidateReadingAsync(int sensorConfigID, decimal readingValue)
        {
              var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<dbDataContext>();

            var sensorConfig = await dbContext.SensorConfigs
                .FirstOrDefaultAsync(c => c.SensorConfigID == sensorConfigID);

            if (sensorConfig == null) {
                _logger.LogWarning("No thresholds found for SensorConfigID {SensorConfigID}", sensorConfigID);
                return true;
            }

            return readingValue >= sensorConfig.MinValidValue && readingValue <= sensorConfig.MaxValidValue;
        }

        private async Task UpdateSensorStatusAsync(SensorConfig sensorConfig, bool isSuccess, string errorMessage, int retryCount)
        {
              var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<dbDataContext>();
            var sensorStatus = await dbContext.SensorConnectionStatuss.SingleOrDefaultAsync(s => s.SensorConfigID == sensorConfig.SensorConfigID);

            if (sensorStatus == null) {
                sensorStatus = new SensorConnectionStatus {
                    SensorConfigID = sensorConfig.SensorConfigID,
                    IPAddress = sensorConfig.IpAddress,
                    Port = int.Parse(sensorConfig.Port),
                    LastConnectionAttemptTime = DateTime.Now,
                    IsReachable = isSuccess,
                    LastErrorMessage = errorMessage,
                    ConsecutiveFailureCount = retryCount,
                    LastSuccessfulReadTime = isSuccess ? DateTime.Now : (DateTime?)null,
                    Status = isSuccess ? 1 : 0
                };
                dbContext.SensorConnectionStatuss.Add(sensorStatus);
            }
            else {
                sensorStatus.LastConnectionAttemptTime = DateTime.Now;
                sensorStatus.IsReachable = isSuccess;
                sensorStatus.LastErrorMessage = errorMessage;
                sensorStatus.ConsecutiveFailureCount = retryCount;
                sensorStatus.LastSuccessfulReadTime = isSuccess ? DateTime.Now : sensorStatus.LastSuccessfulReadTime;
                sensorStatus.Status = isSuccess ? 1 : 0;
            }

            await dbContext.SaveChangesAsync();
        }
        */
    }
}
