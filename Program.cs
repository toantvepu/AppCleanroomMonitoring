using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using AppCleanRoom;
namespace CleanroomMonitoring.Software
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            //Khi chạy ứng dụng
            string logFilePath = "application_log.txt";
            string startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(logFilePath, $"Application started at: {startTime}{Environment.NewLine}");

            // Đăng ký sự kiện khi ứng dụng kết thúc
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                string exitTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.AppendAllText(logFilePath, $"Application exited at: {exitTime}{Environment.NewLine}");
            };

            // Đăng ký sự kiện khi ứng dụng gặp ngoại lệ chưa được xử lý
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                string crashTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.AppendAllText(logFilePath, $"Application crashed at: {crashTime}{Environment.NewLine}");
                File.AppendAllText(logFilePath, $"Exception: {e.ExceptionObject.ToString()}{Environment.NewLine}");


            };
             
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
