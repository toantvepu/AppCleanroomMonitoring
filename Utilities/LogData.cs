using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CleanroomMonitoring.Software.Utilities
{
    public class LogData
    {
        
        /// <summary>
     /// Lưu dữ liệu check vào logfile
     /// </summary>
     /// <param name="entry"></param>
     /// <param name="filePath"></param>
        public static void WriteLog(string entry)
        {
            try
            {
                //Tạo đường dẫn thư mục log
                string directoryPath = "Log";
                //Kiểm tra và tạo thưmục nếu chưa tồn tại.
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                // Tạo tên tệp CSV với ngày và giờ hiện tại
                string fileName = Path.Combine(directoryPath, $"Log_{DateTime.Now:yyyyMMdd}.csv");

                // Ghi dữ liệu vào tệp CSV
                using (StreamWriter writer = new StreamWriter(fileName, true)) // true để thêm vào cuối tệp
                {
                    // Ghi tiêu đề cột chỉ khi tệp mới được tạo
                    if (new FileInfo(fileName).Length == 0)
                    {
                        writer.WriteLine("CreateDate,Description");
                    }

                    // Ghi dữ liệu
                    writer.WriteLine($"{DateTime.Now},{entry}");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Hãy đóng file log lại.", "Lỗi ghi file log", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // throw;
            }


        }
        
        /// <summary>
     /// Lưu dữ liệu check vào logfile
     /// </summary>
     /// <param name="entry"></param>
     /// <param name="filePath"></param>
        public static void SaveLog(LogEntry entry)
        {
            try
            {
                //Tạo đường dẫn thư mục log
                string directoryPath = "Log";
                //Kiểm tra và tạo thưmục nếu chưa tồn tại.
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                // Tạo tên tệp CSV với ngày và giờ hiện tại
                string fileName = Path.Combine(directoryPath, $"Log_{DateTime.Now:yyyyMMdd}.csv");

                // Ghi dữ liệu vào tệp CSV
                using (StreamWriter writer = new StreamWriter(fileName, true)) // true để thêm vào cuối tệp
                {
                    // Ghi tiêu đề cột chỉ khi tệp mới được tạo
                    if (new FileInfo(fileName).Length == 0)
                    {
                        writer.WriteLine("CreateDate,Description");
                    }

                    // Ghi dữ liệu
                    writer.WriteLine($"{entry.CreateDate:yyyy-MM-dd HH:mm:ss},{entry.EventDescription}");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Hãy đóng file log lại.", "Lỗi ghi file log", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // throw;
            }


        }
    }
}
