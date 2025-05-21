using AppCleanRoom.Models;

using CleanroomMonitoring.Software.Models;

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace CleanroomMonitoring.Software.DataContext
{
    public partial class CleanroomDbContext : DbContext
    {
        public CleanroomDbContext()
            : base("name=dbContext")
        {
        }
  
        public DbSet<SensorReading> SensorReadings { get; set; }
        public DbSet<SensorConfig> SensorConfigs { get; set; }
        public DbSet<SensorInfo> SensorInfos { get; set; } 

        public DbSet<AlertHistory> AlertHistorys { get; set; } 
        public DbSet<AlertThreshold> AlertThresholds { get; set; } 
        public DbSet<AuditTrail> AuditTrails { get; set; } 
        public DbSet<EmailNotificationHistory> EmailNotificationHistorys { get; set; } 
        public DbSet<ErrorLog> ErrorLogs { get; set; } 
        public DbSet<SensorConnectionStatus> SensorConnectionStatuss { get; set; } 
        public DbSet<SensorFlags> SensorFlagss { get; set; } 
        public DbSet<SensorHealthCheckHistory> SensorHealthCheckHistorys { get; set; } 
        public DbSet<LogReadSensor> LogReadSensors { get; set; } 
         

         

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
           

            modelBuilder.Entity<SensorConfig>()
                .Property(e => e.MinValidValue)
                .HasPrecision(7, 1);

            modelBuilder.Entity<SensorConfig>()
                .Property(e => e.MaxValidValue)
                .HasPrecision(7, 1);

            modelBuilder.Entity<SensorConfig>()
                .Property(e => e.LowAlertThreshold)
                .HasPrecision(7, 1);

            modelBuilder.Entity<SensorConfig>()
                .Property(e => e.HighAlertThreshold)
                .HasPrecision(7, 1);

          
            modelBuilder.Entity<SensorReading>()
           .Property(e => e.ReadingValue)
           .HasPrecision(7, 1);

            

        }
    }
}
