using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
namespace CleanroomMonitoring.Software
{
    /// <summary>
    /// Đọc ghi App.config
    /// </summary>
    public class ConfigUpdater
    {
        public static void UpdateAppSettings(string key, string value)
        {
            // Mở tệp cấu hình
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // Kiểm tra nếu khóa đã tồn tại
            if (config.AppSettings.Settings[key] != null)
            {
                // Cập nhật giá trị
                config.AppSettings.Settings[key].Value = value;
            }
            else
            {
                // Thêm khóa mới
                config.AppSettings.Settings.Add(key, value);
            }

            // Lưu thay đổi và làm mới phần cấu hình trong bộ nhớ
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static string GetAppSetting(string key)
        {
            // Đọc giá trị từ appSettings
            return ConfigurationManager.AppSettings[key];
        }
    }
}
