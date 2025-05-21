using AppCleanRoom;
using AppCleanRoom.Models;
using AppCleanRoom.PLC;
using AppCleanRoom.Utilities;

using CleanroomMonitoring.Software.DataContext;
using CleanroomMonitoring.Software.Models;
using CleanroomMonitoring.Software.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;

using NModbus;
using OfficeOpenXml.Style.XmlAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

using Timer = System.Windows.Forms.Timer;

namespace CleanroomMonitoring.Software
{
    public partial class MainForm : Form
    {
        private CleanroomDbContext _dbContext;
        private CancellationTokenSource _cancellationTokenSource;
        private Timer monitoringTimer;
        private DateTime startTime;
        private bool isMonitoring = false;
        private bool _isMonitoring;
        private int _readIntervalSeconds = 60; // Chu kỳ đọc dữ liệu (giây)
        private readonly int _connectionTimeoutMs = 5000; // Timeout kết nối (ms)
        private readonly int _modbusTimeoutMs = 5000; // Timeout cho đọc Modbus (ms)
        private readonly int _maxRetries = 3; // Số lần thử lại khi kết nối thất bại
        private   List<string> _plcAddresses = new List<string> { "10.33.0.110", "10.33.0.111", "10.33.0.112" }; //Khởi tạo sản list địa chỉ IP
      
        private readonly int _plcPort = 502;
        private readonly object _logLock = new object(); // Đối tượng khóa cho việc ghi log
        private Dictionary<string, PlcStatus> _plcStatusMap = new Dictionary<string, PlcStatus>(); // Bản đồ trạng thái PLC
        private readonly string ERROR = "ERROR";
        private readonly string WARNING = "WARNING";


        public MainForm()
        {
            InitializeComponent();
            _dbContext = new CleanroomDbContext();
            InitializeControls();
            //Khoi tao trang thai cho tat ca PLC
            foreach (var ip in _plcAddresses)
            {
                _plcStatusMap[ip] = new PlcStatus(ip);
            }
        }
        private void InitializeControls()
        {
            // Khởi tạo Timer để cập nhật thời gian đang chạy
            monitoringTimer = new Timer();
            monitoringTimer.Interval = 1000; // Cập nhật mỗi giây
            monitoringTimer.Tick += MonitoringTimer_Tick;

            // Khởi tạo trạng thái ban đầu
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            lblStatus.Text = "Stopped";
            lblStatus.BackColor = Color.LightCoral;
            lblStartTime.Text = "---";

            // Thêm tooltip cho các nút



        }

        //Ham cap nhat trang thai cho PLC
        private void UpdatePlcStatus(string ipAddress, bool isSuccessful)
        {
            if (!_plcStatusMap.ContainsKey(ipAddress))
            {
                _plcStatusMap[ipAddress] = new PlcStatus(ipAddress);
            }
            if (isSuccessful)
            {
                _plcStatusMap[ipAddress].MarkAvailable();
                // WriteLog($"PLC {ipAddress} đã khôi phục kết nối và hoạt động bình thường.");
            }
            else
            {
                _plcStatusMap[ipAddress].MarkAsFailed();
                WriteLog($"PLC {ipAddress} không khả dụng. Thử lại sau {(_plcStatusMap[ipAddress].NextRetryTime - DateTime.Now).TotalSeconds:F0} giây.");
            }
        }
        // Hàm kiểm tra PLC có thể sử dụng hay không
        private bool IsPLCAvailable(string ipAddress)
        {
            if (!_plcStatusMap.ContainsKey(ipAddress))
            {
                _plcStatusMap[ipAddress] = new PlcStatus(ipAddress);
                return true;
            }

            if (_plcStatusMap[ipAddress].IsAvailable)
            {
                return true;
            }

            return _plcStatusMap[ipAddress].ShouldRetry();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (txtCodeExit.Text.Trim() != "4012")
            {
                MessageBox.Show("Bạn cần nhập code để thoát");
                return;
            }
            // Hỏi người dùng xác nhận
            if (MessageBox.Show("Xác nhận thoát ứng dụng? Chọn Yes để thoát hoàn toàn. Chọn No để thu nhỏ ứng dụng", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {

                if (txtCodeExit.Text == "4012")
                {
                    StopMonitoring();
                    _dbContext.Dispose();

                    WriteLog("Đóng phần mềm");
                    Application.Exit();
                    //Environment.Exit(0);
                    //Application.Exit(): Thường được sử dụng để đóng toàn bộ ứng dụng một cách đơn giản và hiệu quả.
                    //this.Close(): Sử dụng khi bạn muốn đóng form hiện tại.
                    //Environment.Exit(0): Sử dụng khi bạn muốn kết thúc quá trình một cách cứng nhắc và không cần thực hiện các thao tác dọn dẹp khác.
                    //FormClosing event: Sử dụng khi bạn muốn thực hiện các xử lý trước khi form đóng, ví dụ như lưu dữ liệu, hỏi người dùng xác nhận, v.v.
                }
                else
                {
                    MessageBox.Show("Bạn cần nhập code Exit để thoát ứng dụng");
                    e.Cancel = true;
                    MinimizeToCorner();
                }

            }
            else
            {
                // Khi nhấn nút đóng, thu nhỏ ứng dụng về vùng thông báo thay vì đóng
                e.Cancel = true;
                MinimizeToCorner();
                return;
            }

        }

        private void BtnMinimize_Click_1(object sender, EventArgs e)
        {

            // Khi nhấn nút thu nhỏ, thu nhỏ ứng dụng về góc phải màn hình
            MinimizeToCorner();
        }
        private void Form1_Load(object sender, EventArgs e)
        {


        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            if (txtCodeExit.Text.Trim() != "4012")
            {
                MessageBox.Show("Bạn cần nhập code để thoát");
                return;
            }
            Application.Exit();

        }
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private void MinimizeToCorner()
        {
            // Lấy kích thước của màn hình
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            // Thu nhỏ ứng dụng về góc phải dưới của màn hình
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = true;
            ShowWindow(this.Handle, SW_SHOWMINIMIZED);
            SetWindowPos(this.Handle, IntPtr.Zero, screenWidth - 200, screenHeight - 200, 200, 200, 0);
        }
        private void RegisterStartupItem(bool isChecked)
        {
            try
            {
                // Lấy đường dẫn của ứng dụng
                string appPath = Application.ExecutablePath;

                // Đăng ký ứng dụng khởi động cùng Windows
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                // key.SetValue("CleanroomMonitoring.Software", appPath);
                if (isChecked)
                {
                    key.SetValue("CleanroomMonitoring.Software", appPath);
                }
                else
                {
                    key.DeleteValue("CleanroomMonitoring.Software", false);
                }
                key.Close();
            }
            catch (Exception ex)
            {
                // MessageBox.Show($"Error registering startup item: {ex.Message}");

            }
        }

        /// <summary>
        /// Chạy service kiểm tra sensor có hoạt động không. Nếu không hoạt động thì gửi mail
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>


        /// <summary>
        /// Gửi mail: Lấy ra những user có vai trò giám sát và gọi hàm gửi mail
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="emailContent"></param>
        //public void SendEmail(string subject, string emailContent)
        //{
        //    try {
        //        ///Lấy email của người giám sát và chỉ gửi mail cho những người có quyền giám sát thôi
        //        var listUsers = _dbContext.UserRoleMappings
        //        .Where(urm => urm.RoleID == 3)
        //        .Include(urm => urm.User)
        //        .Select(urm => urm.User)
        //        .ToList();
        //        foreach (var item in listUsers) {
        //            string fromMail = "";
        //            string toMail = "";
        //            string ccMail = "";
        //            fromMail = item.Email;
        //            toMail = item.Email;
        //            ccMail = item.Email;
        //            bool result = mailContent(fromMail, toMail, ccMail, emailContent, subject);

        //            //Lưu lại lịch sử gử mail có thành công hay không
        //            if (result == true) {
        //                //lưu đatabase bảng Emaillog gửi được
        //                //Hiển thị lên text
        //                WriteLog(" Đã gửi email");

        //            }
        //            else {
        //                //Không gửi được
        //                //
        //                txtLogEmail.Text = txtLogEmail.Text + Environment.NewLine + DateTime.Now.ToString() + " lỗi gửi mail ";
        //                WriteLog(" Lỗi gửi email");
        //            }
        //        }
        //    }
        //    catch (Exception) {
        //        if (txtLogEmail.InvokeRequired) // Check if an invoke is required
        //        {
        //            // Create a delegate (a method signature) that matches the method you want to execute
        //            MethodInvoker action = delegate { txtLogEmail.Text = txtLogEmail.Text + Environment.NewLine + DateTime.Now.ToString() + " không gửi được mail "; };

        //            // Asynchronously invoke the delegate on the UI thread
        //            txtLogEmail.BeginInvoke(action);

        //            // Or synchronously invoke:
        //            // txtLogEmail.Invoke(action);
        //        }
        //        else
        //        {
        //            // We are already on the UI thread, so we can directly access the control
        //            txtLogEmail.Text = txtLogEmail.Text + Environment.NewLine + DateTime.Now.ToString() + " không gửi được mail ";
        //        }

        //    }

        //}
        ///// <summary>
        ///// Gửi mail
        ///// </summary>
        ///// <param name="fromMail"></param>
        ///// <param name="toMail"></param>
        ///// <param name="ccMail"></param>
        ///// <param name="content"></param>
        ///// <param name="subject"></param>
        ///// <returns></returns>
        //public bool mailContent(string fromMail, string toMail, string ccMail, string content, string subject)
        //{
        //    try {
        //        string currentDate = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
        //        string strBody = $@"
        //    <html>
        //    <body>
        //        Dear all!
        //        </ br>
        //        <p style='color: red; font-size: 14px;'>{content}</p>
        //         </ br>
        //       <p> Gửi từ hệ thống Clean Room ngày: {currentDate} </p>
        //    </body>
        //    </html>";

        //        MailMessage mail = new MailMessage();
        //        mail.From = new MailAddress("CleanroomSW@asahi-intecc.com", "CleanroomSoftware");
        //        mail.To.Add(new MailAddress(toMail));
        //        mail.CC.Add(new MailAddress(ccMail));
        //        mail.Subject = subject;
        //        mail.Body = strBody;
        //        mail.IsBodyHtml = true;

        //        SmtpClient smtp = new SmtpClient("sbox.asahi-intecc.com");
        //        smtp.Send(mail);

        //        return true;
        //    }
        //    catch (Exception ex) {
        //        // Log lỗi (có thể dùng Logging Service)
        //        Console.WriteLine(ex.ToString());
        //        return false;
        //    }
        //}

        private async void btnStart_Click(object sender, EventArgs e)
        {
            // Cập nhật trạng thái UI
            isMonitoring = true;
            startTime = DateTime.Now;
            // Bắt đầu timer để cập nhật thời gian chạy
            monitoringTimer.Start();

            // Cập nhật giao diện
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            lblStatus.Text = "Running";
            lblStatus.BackColor = Color.LightGreen;
            lblStartTime.Text = startTime.ToString("dd/MM/yyyy HH:mm:ss");

            _readIntervalSeconds = int.Parse(txtChuKydocdulieu.Text);
            if (_isMonitoring)
                return;

            try
            {



                //Nếu database không định nghĩa thì sử dụng list IP mặc định

                _isMonitoring = true;
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                lblStatus.Text = "Running...";
                lblStartTime.Text = DateTime.Now.ToString();
                // Thêm thông báo (optional)
                statusStrip1.Items[0].Text = "Đang giám sát dữ liệu...";
                _cancellationTokenSource = new CancellationTokenSource();
                WriteLog("Bắt đầu giám sát sensor...");

                // Bắt đầu task monitoring trong try-catch riêng để xử lý ngoại lệ
                try
                {
                    await StartMonitoring(_cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    WriteLog("Quá trình giám sát đã được dừng bởi người dùng.");
                }
                catch (Exception ex)
                {
                    WriteLog($"Lỗi trong quá trình giám sát: {ex.Message}", ERROR);
                    StopMonitoring();
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Lỗi khởi động giám sát: {ex.Message}", ERROR);
                StopMonitoring();
            }
        }
        private void btnStop_Click(object sender, EventArgs e)
        {
            // Cập nhật trạng thái UI
            isMonitoring = false;
            // Dừng timer
            monitoringTimer.Stop();
            // Tính toán thời gian đã chạy
            TimeSpan runningTime = DateTime.Now - startTime;
            lblStartTime.Text = $"Thời gian đã chạy: {runningTime.Hours:D2}:{runningTime.Minutes:D2}:{runningTime.Seconds:D2}";
            // Thêm thông báo (optional)
            statusStrip1.Items[0].Text = "Giám sát đã dừng";

            // Cập nhật giao diện
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            lblStatus.Text = "Stopped";
            lblStatus.BackColor = Color.LightCoral;

            StopMonitoring();
            WriteLog("Đã dừng giám sát sensor.");
        }
        private void MonitoringTimer_Tick(object sender, EventArgs e)
        {
            if (isMonitoring)
            {
                // Cập nhật thời gian đang chạy
                TimeSpan runningTime = DateTime.Now - startTime;
                lblStartTime.Text = $"Thời gian đang chạy: {runningTime.Hours:D2}:{runningTime.Minutes:D2}:{runningTime.Seconds:D2}";

                // Tạo hiệu ứng nhấp nháy cho label status (chỉ thay đổi độ đậm của màu)
                if (lblStatus.BackColor == Color.LightGreen)
                    lblStatus.BackColor = Color.Green;
                else
                    lblStatus.BackColor = Color.LightGreen;
            }
        }
        /// <summary>
        /// Dừng đọc dữ liệu
        /// </summary>
        private void StopMonitoring()
        {
            if (!_isMonitoring)
                return;

            _cancellationTokenSource?.Cancel();
            _isMonitoring = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            lblStatus.Text = "Stopped";

        }

        /// <summary>
        /// Bắt đầu quá trình giám sát monitoring
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        // Cập nhật hàm StartMonitoring để thêm kiểm tra sức khỏe sensor định kỳ
        private async Task StartMonitoring(CancellationToken cancellationToken)
        {
            int cycleCount = 1;
            int healthCheckCounter = 0; // Đếm số chu kỳ để thực hiện kiểm tra sức khỏe
            
            _plcAddresses = _dbContext.SensorInfos
            .Where(s => !string.IsNullOrEmpty(s.IpAddress))
            .Select(s => s.IpAddress)
            .Distinct()
            .ToList();
            while (!cancellationToken.IsCancellationRequested)
            {
                DateTime cycleStartTime = DateTime.Now;
                WriteLog($"--- Bắt đầu chu kỳ đọc #{cycleCount} lúc {cycleStartTime.ToString("HH:mm:ss")} ---");


                try
                {
                    // Lấy danh sách cấu hình sensor từ database
                    List<SensorInfo> sensorInfos = await Task.Run(() => _dbContext.Set<SensorInfo>()
                    .Where(s => s.IpAddress != null && s.IsActive == true)
                    .ToList(), cancellationToken);

                    // Nhóm các sensor theo địa chỉ IP
                    var sensorsByIp = sensorInfos.GroupBy(s => s.IpAddress).ToDictionary(g => g.Key, g => g.ToList());

                    // Danh sách các task đọc dữ liệu PLC
                    var plcTasks = new List<Task<List<PlcData>>>();
                    var plcTaskMap = new Dictionary<string, Task<List<PlcData>>>();

                    // Tạo task cho mỗi PLC được cấu hình
                    foreach (string ipAddress in _plcAddresses)
                    {
                        //Bo qua PLC khong kha dung va chua den thoi gian thu lai
                        if (!IsPLCAvailable(ipAddress))
                        {
                            WriteLog($"Bỏ qua PLC {ipAddress} vì trạng thái không khả dụng. Thử lại sau: {(_plcStatusMap[ipAddress].NextRetryTime - DateTime.Now).TotalSeconds:F0} giây");
                            continue;
                        }

                        // Chỉ đọc PLC nếu có sensor được cấu hình cho nó
                        if (sensorsByIp.ContainsKey(ipAddress) && sensorsByIp[ipAddress].Any())
                        {
                            var task = ReadPlcDataAsync(ipAddress, _plcPort, sensorsByIp[ipAddress], cancellationToken);
                            plcTasks.Add(task);
                            plcTaskMap[ipAddress] = task;
                        }
                    }

                    // Đợi tất cả các task hoàn thành - không block nếu một PLC bị timeout
                    while (plcTasks.Count > 0)
                    {
                        //cho task dau tien hoan thanh
                        var completeTask = await Task.WhenAny(plcTasks);
                        plcTasks.Remove(completeTask);
                        try
                        {
                            //Xu ly ket qua tu PLC da doc duoc
                            var plcDataList = await completeTask;
                            if (plcDataList != null && plcDataList.Any())
                            {
                                string ipAddress = plcDataList.First().IpAddress;
                                // Đánh dấu PLC này là khả dụng
                                UpdatePlcStatus(ipAddress, true);
                                List<SensorInfo> relatedSensors = sensorsByIp[ipAddress];
                                foreach (var sensor in relatedSensors)
                                {
                                    await ProcessSensorReading(sensor, plcDataList);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteLog($"Lỗi khi đọc dữ liệu từ PLC: {ex.Message}", ERROR);
                            // Ta không biết chính xác PLC nào lỗi ở đây, vì vậy không cập nhật trạng thái
                        }
                    }
                    // Kiểm tra các PLC đã bị lỗi
                    foreach (var pair in plcTaskMap)
                    {
                        string ipAddress = pair.Key;
                        var task = pair.Value;

                        if (task.IsFaulted || task.IsCanceled)
                        {
                            UpdatePlcStatus(ipAddress, false);

                            // Cập nhật trạng thái kết nối của các sensor liên quan
                            if (sensorsByIp.ContainsKey(ipAddress))
                            {
                                foreach (var sensor in sensorsByIp[ipAddress])
                                {
                                    await RecordSensorConnectionIssue(sensor.SensorInfoID, "PLC_CONNECTION_ERROR",
                                        $"Lỗi kết nối tới PLC có địa chỉ IP {ipAddress}");
                                }
                            }
                        }
                    }

                    // Thực hiện kiểm tra sức khỏe sensor định kỳ (mỗi 10 chu kỳ)
                    healthCheckCounter++;
                    if (healthCheckCounter >= 10)
                    { // Khoảng 10 * _readIntervalSeconds giây
                        await PerformSensorHealthCheck();
                        healthCheckCounter = 0;
                    }
                }
                catch (Exception ex)
                {
                    WriteLog($"Lỗi trong chu kỳ đọc dữ liệu #{cycleCount}: {ex.Message}", ERROR);
                }
                finally
                {

                    // Tính toán thời gian chờ cho đủ chu kỳ
                    TimeSpan elapsedTime = DateTime.Now - cycleStartTime;
                    int delayTimeMs = Math.Max(100, (int)(_readIntervalSeconds * 1000 - elapsedTime.TotalMilliseconds));

                    WriteLog($"--- Kết thúc chu kỳ đọc #{cycleCount}. Thời gian thực hiện: {elapsedTime.TotalSeconds:F1}s. Đợi {delayTimeMs / 1000.0:F1}s cho chu kỳ tiếp theo... ---");

                    // Đợi cho đến chu kỳ đọc tiếp theo
                    try
                    {
                        await Task.Delay(delayTimeMs, cancellationToken);
                    }
                    catch (TaskCanceledException ex)
                    {
                        WriteLog("Quá trình đợi chu kỳ kế tiếp đã bị hủy.");
                        throw ex;
                    }

                    cycleCount++;
                }
            }
        }



        // Hàm kiểm tra xem có phải lỗi liên quan đến kết nối không
        private async Task<List<PlcData>> ReadPlcDataAsync(string ipAddress, int port, List<SensorInfo> sensors, CancellationToken cancellationToken)
        {
            var result = new List<PlcData>();

            // Kiểm tra kết nối PLC trước khi thực hiện đọc dữ liệu
            if (!await TestPlcConnectionAsync(ipAddress, port, cancellationToken))
            {
                UpdatePlcStatus(ipAddress, false);
                return result;
            }

            // Biến để lưu trữ tài nguyên
            TcpClient client = null;
            IModbusMaster master = null;

            try
            {
                // Lấy danh sách ModbusAddress cần đọc từ sensors
                var modbusAddresses = sensors
                    .Where(s => s.ModbusAddress.HasValue)
                    .Select(s => s.ModbusAddress.Value)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToList();

                // Số lần thử lại tối đa cho mỗi thanh ghi
                const int maxRegisterRetries = 5;
                // Thời gian chờ giữa các lần thử lại (ms)
                const int retryDelayMs = 500;

                // Tạo kết nối ban đầu
                client = CreateNewConnection(ipAddress, port, out master);

                // Danh sách các thanh ghi cần đọc lại
                var addressesToRetry = new List<int>();

                // Lần đọc đầu tiên cho tất cả các thanh ghi
                foreach (int address in modbusAddresses)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    bool readSuccess = false;
                    decimal value = 0;

                    try
                    {
                        // Đọc giá trị thanh ghi
                        var readTask = Task.Run(() =>
                        {
                            if (client != null && client.Connected)
                            {
                                return ReadRegisterValue(master, (ushort)address);
                            }
                            throw new InvalidOperationException("Socket không được kết nối");
                        }, cancellationToken);

                        WriteLog($"Đang đọc thanh ghi {address} từ PLC {ipAddress} (lần đầu)");

                        if (await Task.WhenAny(readTask, Task.Delay(_modbusTimeoutMs, cancellationToken)) == readTask)
                        {
                            value = await readTask;
                            // Đánh dấu readSuccess = true chỉ khi giá trị khác 0
                            if (value != 0)
                            {
                                readSuccess = true;
                                WriteLog($"Đọc thành công thanh ghi {address} từ PLC {ipAddress}: {value}");
                            }
                            else
                            {
                                WriteLog($"Đọc được giá trị 0 tại thanh ghi {address} từ PLC {ipAddress}, thêm vào danh sách đọc lại");
                                addressesToRetry.Add(address);
                            }
                        }
                        else
                        {
                            WriteLog($"Timeout khi đọc thanh ghi {address} từ PLC {ipAddress}, thêm vào danh sách đọc lại");
                            addressesToRetry.Add(address);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLog($"Lỗi khi đọc thanh ghi {address} từ PLC {ipAddress}: {ex.Message}", ERROR);
                        addressesToRetry.Add(address);
                    }

                    // Ghi nhận kết quả nếu đọc thành công
                    if (readSuccess)
                    {
                        result.Add(new PlcData
                        {
                            IpAddress = ipAddress,
                            ThanhGhi = address,
                            DuLieu = value,
                            HasError = false
                        });
                    }
                }

                // Đóng kết nối ban đầu nếu còn thanh ghi cần đọc lại
                if (addressesToRetry.Count > 0)
                {
                    CleanupResources(client, master, ipAddress);
                    client = null;
                    master = null;
                }

                // Xử lý các thanh ghi cần đọc lại
                foreach (int address in addressesToRetry)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    bool readSuccess = false;
                    decimal value = 0;
                    int retryCount = 0;

                    while (!readSuccess && retryCount < maxRegisterRetries && !cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            // Tạo kết nối mới cho mỗi lần đọc lại
                            if (client == null || !client.Connected)
                            {
                                CleanupResources(client, master, ipAddress);
                                client = CreateNewConnection(ipAddress, port, out master);
                            }

                            // Đọc giá trị thanh ghi
                            var readTask = Task.Run(() =>
                            {
                                if (client != null && client.Connected)
                                {
                                    return ReadRegisterValue(master, (ushort)address);
                                }
                                throw new InvalidOperationException("Socket không được kết nối");
                            }, cancellationToken);

                            WriteLog($"Đang đọc lại thanh ghi {address} từ PLC {ipAddress} (lần thử {retryCount + 1})");

                            if (await Task.WhenAny(readTask, Task.Delay(_modbusTimeoutMs, cancellationToken)) == readTask)
                            {
                                value = await readTask;
                                if (value != 0)
                                {
                                    readSuccess = true;
                                    WriteLog($"Đọc lại thành công thanh ghi {address} từ PLC {ipAddress}: {value}");
                                }
                                else
                                {
                                    WriteLog($"Đọc lại vẫn nhận giá trị 0 tại thanh ghi {address} từ PLC {ipAddress}, sẽ thử lần {retryCount + 2}/{maxRegisterRetries}");
                                }
                            }
                            else
                            {
                                WriteLog($"Timeout khi đọc lại thanh ghi {address} từ PLC {ipAddress} (lần thử {retryCount + 1})");
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteLog($"Lỗi khi đọc lại thanh ghi {address} từ PLC {ipAddress} (lần thử {retryCount + 1}): {ex.Message}", ERROR);

                            // Đóng kết nối cũ nếu là lỗi kết nối
                            if (IsConnectionError(ex))
                            {
                                CleanupResources(client, master, ipAddress);
                                client = null;
                                master = null;
                            }
                        }

                        if (!readSuccess)
                        {
                            retryCount++;
                            if (retryCount < maxRegisterRetries)
                            {
                                await Task.Delay(retryDelayMs, cancellationToken);
                            }
                        }
                    }

                    // Ghi nhận kết quả cho thanh ghi đã đọc lại
                    bool hasError = !readSuccess;
                    result.Add(new PlcData
                    {
                        IpAddress = ipAddress,
                        ThanhGhi = address,
                        DuLieu = value,
                        HasError = hasError
                    });

                    if (hasError)
                    {
                        WriteLog($"Không thể đọc thanh ghi {address} từ PLC {ipAddress} sau {maxRegisterRetries} lần thử");
                    }
                }

                // Cập nhật trạng thái PLC thành công nếu có ít nhất một thanh ghi đọc thành công
                UpdatePlcStatus(ipAddress, result.Any(r => !r.HasError));
            }
            catch (Exception ex)
            {
                WriteLog($"Lỗi khi đọc dữ liệu từ PLC {ipAddress}: {ex.Message}", ERROR);
                UpdatePlcStatus(ipAddress, false);
            }
            finally
            {
                // Đảm bảo giải phóng tài nguyên
                CleanupResources(client, master, ipAddress);
            }

            return result;
        }

        // Phương thức hỗ trợ để tạo kết nối mới
        private TcpClient CreateNewConnection(string ipAddress, int port, out IModbusMaster master)
        {
            WriteLog($"Tạo kết nối mới đến PLC {ipAddress}:{port}");

            var client = new TcpClient();
            client.Connect(ipAddress, port);

            var factory = new ModbusFactory();
            master = factory.CreateMaster(client);
            master.Transport.ReadTimeout = _modbusTimeoutMs;
            master.Transport.WriteTimeout = _modbusTimeoutMs;

            return client;
        }
        private bool IsConnectionError(Exception ex)
        {
            return ex is SocketException ||
                   ex is IOException ||
                   ex is ObjectDisposedException ||
                   ex.Message.Contains("connection") ||
                   ex.Message.Contains("socket") ||
                   ex.Message.Contains("non-connected") ||
                   ex.Message.Contains("broken pipe");
        }

        // Hàm kiểm tra kết nối với PLC
        private async Task<bool> TestPlcConnectionAsync(string ipAddress, int port, CancellationToken cancellationToken)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    var connectTask = client.ConnectAsync(ipAddress, port);
                    if (await Task.WhenAny(connectTask, Task.Delay(_connectionTimeoutMs, cancellationToken)) != connectTask)
                    {
                        return false; // Timeout kết nối
                    }
                    return client.Connected;
                }
                catch
                {
                    return false;
                }
            }
        }

        // Hàm riêng để giải phóng tài nguyên
        private void CleanupResources(TcpClient client, IModbusMaster master, string ipAddress)
        {
            try
            {
                // Đóng master (nếu có)
                if (master != null && master is IDisposable disposableMaster)
                {
                    disposableMaster.Dispose();
                }

                // Đóng kết nối TCP
                if (client != null)
                {
                    try
                    {
                        if (client.Connected)
                        {
                            client.GetStream().Close();
                        }
                    }
                    catch (Exception) { /* Bỏ qua lỗi khi đóng stream */ }

                    try
                    {
                        client.Close();
                        client.Dispose();
                    }
                    catch (Exception) { /* Bỏ qua lỗi khi đóng client */ }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Lỗi khi đóng kết nối: {ex.Message}", ERROR);
            }
        }


        /// <summary>
        /// Hàm đọc giá trị thanh ghi từ PLC
        /// </summary>
        /// <param name="master"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        private decimal ReadRegisterValue(IModbusMaster master, ushort address)
        {
            try
            {

                // Thử thực hiện đọc thanh ghi với retry
                for (int retry = 0; retry < _maxRetries; retry++)
                {
                    try
                    {
                        // Kiểm tra xem master có liên kết với TcpClient hợp lệ không
                        if (master == null)
                        {
                            // Ghi log an toàn hơn - không gây crash
                            try { WriteLog($"Modbus master is null cho thanh ghi {address}", ERROR); } catch { }
                            return 0m;
                        }

                        // Đọc giá trị từ thanh ghi
                        ushort[] response = master.ReadHoldingRegisters(1, address, 1);
                        return response[0];
                    }
                    catch (IOException ioEx)
                    {
                        // Ghi log an toàn hơn
                        string logMessage = $"Lỗi IO (lần thử {retry + 1}/{_maxRetries}) khi đọc thanh ghi {address}: {ioEx.Message}";
                        Task.Run(() => { try { WriteLog(logMessage); } catch { } });

                        // Nếu đây là lần thử cuối cùng, không ném lại ngoại lệ
                        if (retry == _maxRetries - 1)
                            return 0m; // Trả về giá trị mặc định thay vì ném lỗi

                        // Ngủ một chút trước khi thử lại
                        Thread.Sleep(300 * (retry + 1));
                    }
                    catch (InvalidOperationException ioEx)
                    {
                        // Ghi log an toàn bằng cách chạy trên thread riêng
                        string logMessage = $"Lỗi kết nối không hợp lệ khi đọc thanh ghi {address}: {ioEx.Message}";
                        Task.Run(() => { try { WriteLog(logMessage); } catch { } });
                        return 0m;
                    }
                    catch (Exception ex)
                    {
                        // Ghi log an toàn
                        string logMessage = $"Lỗi không xác định (lần thử {retry + 1}/{_maxRetries}) khi đọc thanh ghi {address}: {ex.Message}";
                        Task.Run(() => { try { WriteLog(logMessage); } catch { } });

                        // Nếu đây là lần thử cuối cùng, không ném lại ngoại lệ
                        if (retry == _maxRetries - 1)
                            return 0m;

                        // Ngủ một chút trước khi thử lại
                        Thread.Sleep(100 * (retry + 1));
                    }
                }


                return 0;
            }
            catch (Exception ex)
            {
                // Ghi log an toàn với Task.Run
                Task.Run(() => { try { WriteLog($"Lỗi không thể phục hồi khi đọc thanh ghi {address}: {ex.Message}"); } catch { } });
                return 0;
            }
        }

        /// <summary>
        /// Kiểm tra giá trị hợp lệ
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool IsValidValue(SensorInfo sensorInfo, decimal value)
        {
            var config = _dbContext.SensorConfigs.SingleOrDefault(p => p.SensorInfoID == sensorInfo.SensorInfoID);
            if (config == null)
            {
                WriteLog($"Không tìm thấy cấu hình cho sensor ID {sensorInfo.SensorInfoID} - {sensorInfo.IpAddress} -  {sensorInfo.ModbusAddress} value: {value}", ERROR);
                return false;
            }
            else
            {
                // Nếu ít nhất một trong hai giá trị MinValidValue hoặc MaxValidValue là null, điều đó có thể ngụ ý rằng không có giới hạn hợp lệ nào được cấu hình cho cảm biến này. Trong trường hợp đó, hàm trả về true, coi giá trị là hợp lệ.
                if (!config.MinValidValue.HasValue || !config.MaxValidValue.HasValue)
                    return true;
            }
            return value >= config.MinValidValue.Value &&
                  value <= config.MaxValidValue.Value;


        }

        /// <summary>
        /// Xu ly du lieu Sensor
        /// </summary>
        /// <param name="sensorConfig"></param>
        /// <param name="plcDataList"></param>
        /// <returns></returns>
        private async Task ProcessSensorReading(SensorInfo sensor, List<PlcData> plcDataList)
        {
            try
            {
                // Tìm dữ liệu tương ứng với sensor
                var matchingData = plcDataList.FirstOrDefault(d =>
                    d.IpAddress == sensor.IpAddress &&
                    d.ThanhGhi == sensor.ModbusAddress);

                if (matchingData == null) {
                    WriteLog($"Không tìm thấy dữ liệu cho sensor ID {sensor.SensorInfoID}, địa chỉ {sensor.ModbusAddress}", ERROR);
                    await RecordSensorIssue(sensor.SensorInfoID, SensorIssueType.MissingData,
                        "Không tìm thấy dữ liệu từ PLC", SensorStatus.Error);
                    return;
                }

                // Xử lý giá trị đọc được
                decimal processedValue = ProcessRawValue(sensor, matchingData);

                // Lấy cấu hình của sensor
                var sensorConfig = await _dbContext.SensorConfigs
                    .FirstOrDefaultAsync(s => s.SensorInfoID == sensor.SensorInfoID);

                if (sensorConfig == null) {
                    WriteLog($"Không tìm thấy cấu hình cho sensor {sensor.SensorName} (ID: {sensor.SensorInfoID})");
                    await RecordSensorIssue(sensor.SensorInfoID, SensorIssueType.NoConfig,
                        "Không tìm thấy cấu hình sensor", SensorStatus.Error);
                    return;
                }

                // Kiểm tra giá trị có hợp lệ không
                bool isValid = IsValidValue(sensor, processedValue);

                DateTime now = DateTime.Now;

                // Lưu dữ liệu vào database
                await SaveSensorData(sensor.SensorInfoID, processedValue, isValid);

                // Cập nhật trạng thái kết nối - sensor đang hoạt động
                await UpdateSensorConnectionStatus(sensor.SensorInfoID, true, now);

                // Kiểm tra giá trị bất thường (out of range, vượt ngưỡng cảnh báo)
                await CheckAbnormalValue(sensor, sensorConfig, processedValue, now);

                // Cập nhật thống kê cho phát hiện hoạt động chập chờn
                await UpdateSensorHealthStatistics(sensor.SensorInfoID, now);


            }
            catch (Exception ex)
            {
                WriteLog($"Lỗi xử lý dữ liệu cho sensor ID {sensor.SensorInfoID}: {ex.Message}", ERROR);
                await RecordSensorConnectionIssue(sensor.SensorInfoID, "PROCESSING_ERROR", $"Lỗi xử lý: {ex.Message}");
            }
        }
        // Xử lý giá trị thô từ PLC thành giá trị thực
        private decimal ProcessRawValue(SensorInfo sensor, PlcData matchingData)
        {
            if (sensor.SensorTypeID != 3) // Không phải cảm biến áp suất
            {
                return matchingData.DuLieu / 100.0m;
            }
            else // Cảm biến áp suất
            {
                string hexString = matchingData.DuLieu.ToString();
                int decimalNumber = Convert.ToInt32(hexString, 16);
                return (decimal)decimalNumber;
            }
        }
        /// <summary>
        /// Lưu dữ liệu Sensor vào database
        /// </summary>
        private async Task<long> SaveSensorData(int sensorInfoId, decimal value, bool isValid)
        {
            try {
                var sensorData = new SensorReading {
                    SensorInfoID = sensorInfoId,
                    ReadingValue = value,
                    ReadingTime = DateTime.Now,
                    IsValid = isValid
                };

                _dbContext.Set<SensorReading>().Add(sensorData);
                await _dbContext.SaveChangesAsync();

                WriteLog($"Đã lưu dữ liệu: SensorInfoID = {sensorData.SensorInfoID}, Value = {sensorData.ReadingValue}, IsValid = {isValid}");

                return sensorData.ReadingID;
            }
            catch (Exception ex) {
                WriteLog($"Lỗi lưu dữ liệu sensor {sensorInfoId}: {ex.Message}", ERROR);
                return -1;
            }
        }

        // Phương thức thống nhất để ghi nhận các vấn đề của sensor
        private async Task RecordSensorIssue(int sensorId, SensorIssueType issueType, string description, SensorStatus status)
        {
            try {
                DateTime now = DateTime.Now;
                string issueTypeStr = issueType.ToString().ToUpper();

                // Chuyển đổi trạng thái sensor sang chuỗi
                string statusStr = status.ToString().ToUpper();

                // Thêm vào lịch sử kiểm tra sức khỏe sensor
                var healthCheck = new SensorHealthCheckHistory {
                    SensorInfoID = sensorId,
                    CheckTime = now,
                    Status = statusStr,
                    IssueType = issueTypeStr,
                    Description = description
                };
                _dbContext.SensorHealthCheckHistorys.Add(healthCheck);

                // Nếu là lỗi kết nối, cập nhật trạng thái kết nối
                if (issueType == SensorIssueType.ConnectionLost ||
                    issueType == SensorIssueType.MissingData ||
                    issueType == SensorIssueType.NoData) {
                    await UpdateSensorConnectionStatus(sensorId, false, now);
                }
                else if (issueType == SensorIssueType.ConnectionRestored) {
                    await UpdateSensorConnectionStatus(sensorId, true, now);
                }

                // Nếu là lỗi nghiêm trọng, thêm vào bảng ErrorLog
                if (status == SensorStatus.Error) {
                    var errorLog = new ErrorLog {
                        ErrorTime = now,
                        ErrorSource = "SensorMonitoring",
                        ErrorMessage = $"Sensor ID {sensorId}: {description}",
                        ErrorType = issueTypeStr,
                        StackTrace = null
                    };
                    _dbContext.ErrorLogs.Add(errorLog);
                }

                await _dbContext.SaveChangesAsync();

                // Kiểm tra và gửi cảnh báo (trừ trường hợp phục hồi)
                if (status != SensorStatus.Recovered && status != SensorStatus.Normal) {
                    await CheckAndSendSensorAlert(sensorId, issueTypeStr, description);
                }
            }
            catch (Exception ex) {
                WriteLog($"Lỗi khi ghi nhận vấn đề sensor {sensorId}: {ex.Message}", ERROR);
            }
        }


        // Cập nhật trạng thái kết nối của sensor
        // Cập nhật trạng thái kết nối của sensor
        private async Task UpdateSensorConnectionStatus(int sensorId, bool isConnected, DateTime timestamp)
        {
            try {
                var connectionStatus = await _dbContext.SensorConnectionStatuss
                    .FirstOrDefaultAsync(s => s.SensorInfoID == sensorId);

                if (connectionStatus == null) {
                    connectionStatus = new SensorConnectionStatus {
                        SensorInfoID = sensorId,
                        IsConnected = isConnected,
                        LastConnectionTime = isConnected ? timestamp : (DateTime?)null,
                        LastDisconnectionTime = !isConnected ? timestamp : (DateTime?)null,
                        DisconnectionCount = !isConnected ? 1 : 0,
                        LastIssueType = null,
                        LastIssueDescription = null
                    };
                    _dbContext.SensorConnectionStatuss.Add(connectionStatus);
                }
                else {
                    // Nếu trạng thái thay đổi từ mất kết nối sang có kết nối
                    if (!connectionStatus.IsConnected && isConnected) {
                        connectionStatus.LastConnectionTime = timestamp;
                        connectionStatus.IsConnected = true;

                        // Ghi nhận việc phục hồi kết nối
                        await RecordSensorIssue(
                            sensorId,
                            SensorIssueType.ConnectionRestored,
                            "Kết nối sensor đã được khôi phục",
                            SensorStatus.Recovered
                        );
                    }
                    // Nếu trạng thái thay đổi từ có kết nối sang mất kết nối
                    else if (connectionStatus.IsConnected && !isConnected) {
                        connectionStatus.LastDisconnectionTime = timestamp;
                        connectionStatus.DisconnectionCount++;
                        connectionStatus.IsConnected = false;
                    }
                    else {
                        // Cập nhật trạng thái nếu không có thay đổi
                        connectionStatus.IsConnected = isConnected;
                    }
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex) {
                WriteLog($"Lỗi khi cập nhật trạng thái kết nối sensor {sensorId}: {ex.Message}", ERROR);
            }
        }


        // Kiểm tra giá trị bất thường
        private async Task CheckAbnormalValue(SensorInfo sensor, SensorConfig config, decimal value, DateTime timestamp)
        {
            try {
                SensorIssueType? issueType = null;
                string description = null;
                SensorStatus status = SensorStatus.Normal;

                // Kiểm tra giá trị có nằm trong khoảng hợp lệ không
                if (config.MinValidValue.HasValue && value < config.MinValidValue.Value) {
                    issueType = SensorIssueType.BelowValidRange;
                    description = $"Giá trị {value} thấp hơn ngưỡng hợp lệ tối thiểu {config.MinValidValue.Value}";
                    status = SensorStatus.Warning;
                }
                else if (config.MaxValidValue.HasValue && value > config.MaxValidValue.Value) {
                    issueType = SensorIssueType.AboveValidRange;
                    description = $"Giá trị {value} cao hơn ngưỡng hợp lệ tối đa {config.MaxValidValue.Value}";
                    status = SensorStatus.Warning;
                }
                // Kiểm tra giá trị vượt ngưỡng cảnh báo
                else if (config.LowAlertThreshold.HasValue && value < config.LowAlertThreshold.Value) {
                    issueType = SensorIssueType.BelowThreshold;
                    description = $"Giá trị {value} thấp hơn ngưỡng cảnh báo {config.LowAlertThreshold.Value}";
                    status = SensorStatus.Warning;
                }
                else if (config.HighAlertThreshold.HasValue && value > config.HighAlertThreshold.Value) {
                    issueType = SensorIssueType.AboveThreshold;
                    description = $"Giá trị {value} cao hơn ngưỡng cảnh báo {config.HighAlertThreshold.Value}";
                    status = SensorStatus.Warning;
                }

                // Lấy cờ cảnh báo hiện tại
                var sensorFlag = await _dbContext.SensorFlagss
                    .FirstOrDefaultAsync(f => f.SensorInfoID == sensor.SensorInfoID);

                if (issueType.HasValue) {
                    // Cập nhật hoặc tạo mới cờ cảnh báo
                    if (sensorFlag == null) {
                        sensorFlag = new SensorFlags {
                            SensorInfoID = sensor.SensorInfoID,
                            HasAbnormalValue = true,
                            AbnormalValueTime = timestamp,
                            AbnormalValueType = issueType.ToString(),
                            AbnormalValueDescription = description
                        };
                        _dbContext.SensorFlagss.Add(sensorFlag);
                    }
                    else {
                        sensorFlag.HasAbnormalValue = true;
                        sensorFlag.AbnormalValueTime = timestamp;
                        sensorFlag.AbnormalValueType = issueType.ToString();
                        sensorFlag.AbnormalValueDescription = description;
                    }

                    // Thêm vào lịch sử cảnh báo
                    var alert = new AlertHistory {
                        SensorInfoID = sensor.SensorInfoID,
                        AlertTime = timestamp,
                        AlertType = issueType.ToString(),
                        AlertMessage = description,
                        AlertValue = value,
                        IsHandled = false
                    };
                    _dbContext.AlertHistorys.Add(alert);

                    await _dbContext.SaveChangesAsync();

                    // Ghi nhận sự cố
                    await RecordSensorIssue(sensor.SensorInfoID, issueType.Value, description, status);
                }
                else if (sensorFlag != null && sensorFlag.HasAbnormalValue) {
                    // Đã trở về giá trị bình thường
                    sensorFlag.HasAbnormalValue = false;
                    sensorFlag.NormalizedTime = timestamp;
                    await _dbContext.SaveChangesAsync();

                    // Ghi nhận phục hồi
                    await RecordSensorIssue(
                        sensor.SensorInfoID,
                        SensorIssueType.ValueNormalized,
                        $"Giá trị đã trở về mức bình thường: {value}",
                        SensorStatus.Recovered
                    );
                }
            }
            catch (Exception ex) {
                WriteLog($"Lỗi khi kiểm tra giá trị bất thường cho sensor {sensor.SensorInfoID}: {ex.Message}", ERROR);
            }
        }


        // Cập nhật thống kê sức khỏe sensor để phát hiện hoạt động chập chờn
        private async Task UpdateSensorHealthStatistics(int sensorId, DateTime timestamp)
        {
            try {
                // Lấy thông tin sensor
                var sensor = await _dbContext.SensorInfos.FindAsync(sensorId);
                if (sensor == null) return;

                // Thời điểm bắt đầu thống kê
                DateTime hourAgo = timestamp.AddHours(-1);
                DateTime dayAgo = timestamp.AddDays(-1);

                // Thống kê số liệu
                int recordsInLastHour = await _dbContext.SensorReadings
                    .CountAsync(r => r.SensorInfoID == sensorId && r.ReadingTime >= hourAgo);

                int recordsInLastDay = await _dbContext.SensorReadings
                    .CountAsync(r => r.SensorInfoID == sensorId && r.ReadingTime >= dayAgo);

                int disconnectsInLastDay = await _dbContext.SensorHealthCheckHistorys
                    .CountAsync(h => h.SensorInfoID == sensorId && h.CheckTime >= dayAgo &&
                              (h.IssueType == "CONNECTION_LOST" || h.IssueType == "MISSING_DATA" || h.IssueType == "NO_DATA"));

                // Tính toán tần suất dự kiến
                double expectedRecordsPerHour = 3600 / _readIntervalSeconds;
                double expectedRecordsPerDay = 86400 / _readIntervalSeconds;

                double hourlyRatio = recordsInLastHour / expectedRecordsPerHour;
                double dailyRatio = recordsInLastDay / expectedRecordsPerDay;

                // Xác định tình trạng cảm biến
                bool isFlickering = (hourlyRatio < 0.8 && hourlyRatio > 0) || (dailyRatio < 0.8 && dailyRatio > 0.4);
                bool isFrequentlyDisconnected = disconnectsInLastDay > 5;

                // Cập nhật cờ sensor
                var sensorFlag = await _dbContext.SensorFlagss
                    .FirstOrDefaultAsync(f => f.SensorInfoID == sensorId);

                if (sensorFlag == null) {
                    sensorFlag = new SensorFlags {
                        SensorInfoID = sensorId,
                        IsFlickering = isFlickering,
                        LastHealthCheckTime = timestamp,
                        RecordsInLastHour = recordsInLastHour,
                        RecordsInLastDay = recordsInLastDay,
                        DisconnectsInLastDay = disconnectsInLastDay
                    };
                    _dbContext.SensorFlagss.Add(sensorFlag);
                }
                else {
                    sensorFlag.IsFlickering = isFlickering;
                    sensorFlag.LastHealthCheckTime = timestamp;
                    sensorFlag.RecordsInLastHour = recordsInLastHour;
                    sensorFlag.RecordsInLastDay = recordsInLastDay;
                    sensorFlag.DisconnectsInLastDay = disconnectsInLastDay;
                }

                await _dbContext.SaveChangesAsync();

                // Ghi nhận sự cố nếu phát hiện hoạt động bất thường
                if (isFlickering || isFrequentlyDisconnected) {
                    SensorIssueType issueType = isFlickering
                        ? SensorIssueType.Flickering
                        : SensorIssueType.FrequentDisconnection;

                    string description = isFlickering
                        ? $"Sensor hoạt động chập chờn: chỉ ghi nhận {recordsInLastHour}/{(int)expectedRecordsPerHour} bản ghi trong 1h và {recordsInLastDay}/{(int)expectedRecordsPerDay} bản ghi trong 24h"
                        : $"Sensor mất kết nối nhiều lần: {disconnectsInLastDay} lần trong 24h qua";

                    await RecordSensorIssue(sensorId, issueType, description, SensorStatus.Warning);
                }
            }
            catch (Exception ex) {
                WriteLog($"Lỗi khi cập nhật thống kê sức khỏe sensor {sensorId}: {ex.Message}", ERROR);
            }
        }


        // Kiểm tra và gửi cảnh báo (email, notification, v.v.)
        private async Task CheckAndSendSensorAlert(int sensorId, string alertType, string description)
        {
            try {
                // Lấy thông tin sensor
                var sensor = await _dbContext.SensorInfos.FindAsync(sensorId);
                if (sensor == null) return;

                // Kiểm tra xem cảnh báo đã được gửi gần đây chưa để tránh spam
                var recentAlert = await _dbContext.EmailNotificationHistorys
                    .OrderByDescending(e => e.SentTime)
                    .FirstOrDefaultAsync(e => e.SensorInfoID == sensorId && e.NotificationType == alertType);

                DateTime now = DateTime.Now;
                bool shouldSendAlert = true;

                // Nếu đã gửi cảnh báo cùng loại trong 30 phút qua, không gửi lại
                if (recentAlert != null && now - recentAlert.SentTime < TimeSpan.FromMinutes(30)) {
                    shouldSendAlert = false;
                }

                if (shouldSendAlert) {
                    // TODO: Thực hiện gửi email thông báo
                    // SendAlertEmail(sensor, alertType, description);

                    // Ghi nhận vào lịch sử thông báo email
                    var emailNotification = new EmailNotificationHistory {
                        SensorInfoID = sensorId,
                        SentTime = now,
                        RecipientEmail = "hanoi-eng40@asahi-intecc.com", // Email thực tế
                        NotificationType = alertType,
                        NotificationContent = description,
                        SentSuccessfully = true
                    };
                    _dbContext.EmailNotificationHistorys.Add(emailNotification);

                    await _dbContext.SaveChangesAsync();

                    WriteLog($"Đã gửi cảnh báo cho sensor {sensor.SensorName}: {description}");
                }
            }
            catch (Exception ex) {
                WriteLog($"Lỗi khi gửi cảnh báo cho sensor {sensorId}: {ex.Message}", ERROR);
            }
        }


        // Ghi nhận vấn đề kết nối của sensor
        private async Task RecordSensorConnectionIssue(int sensorId, string issueType, string description)
        {
            try
            {
                var now = DateTime.Now;
                 

                // Thêm vào lịch sử kiểm tra sức khỏe sensor
                var healthCheck = new SensorHealthCheckHistory
                {
                    SensorInfoID = sensorId,
                    CheckTime = now,
                    Status = "ERROR",
                    IssueType = issueType,
                    Description = description
                };
                _dbContext.SensorHealthCheckHistorys.Add(healthCheck);

                // Log lỗi
                var errorLog = new ErrorLog
                {
                    ErrorTime = now,
                    ErrorSource = "SensorMonitoring",
                    ErrorMessage = $"Sensor ID {sensorId}: {description}",
                    ErrorType = issueType,
                    StackTrace = null
                };
                _dbContext.ErrorLogs.Add(errorLog);

                await _dbContext.SaveChangesAsync();

                // Kiểm tra nếu cần gửi cảnh báo
                await CheckAndSendSensorAlert(sensorId, issueType, description);
            }
            catch (Exception ex)
            {
                WriteLog($"Lỗi khi ghi nhận vấn đề kết nối sensor {sensorId}: {ex.Message}", ERROR);
            }
        }

         
        // Hàm này được gọi định kỳ để kiểm tra sức khỏe toàn bộ hệ thống sensor
        private async Task PerformSensorHealthCheck()
        {
            try
            {
                // Kiểm tra các sensor không gửi dữ liệu
                await CheckInactiveSensors();

                // Các kiểm tra sức khỏe khác có thể được thêm ở đây
            }
            catch (Exception ex)
            {
                WriteLog($"Lỗi khi thực hiện kiểm tra sức khỏe sensor: {ex.Message}", ERROR);
            }
        }


        // Kiểm tra nếu có sensor không gửi dữ liệu lâu
        private async Task CheckInactiveSensors()
        {
            try {
                // Lấy tất cả sensor đang hoạt động
                var activeSensors = await _dbContext.SensorInfos
                    .Where(s => s.IsActive)
                    .ToListAsync();

                DateTime now = DateTime.Now;
                TimeSpan inactivityThreshold = TimeSpan.FromMinutes(5); // Ngưỡng 5 phút không có dữ liệu

                foreach (var sensor in activeSensors) {
                    // Lấy bản ghi cuối cùng
                    var lastReading = await _dbContext.SensorReadings
                        .Where(r => r.SensorInfoID == sensor.SensorInfoID)
                        .OrderByDescending(r => r.ReadingTime)
                        .FirstOrDefaultAsync();

                    if (lastReading == null || now - lastReading.ReadingTime > inactivityThreshold) {
                        string description = lastReading == null
                            ? $"Sensor {sensor.SensorName} chưa từng gửi dữ liệu"
                            : $"Sensor {sensor.SensorName} không gửi dữ liệu trong {(int)(now - lastReading.ReadingTime).TotalMinutes} phút";

                        // Ghi nhận vấn đề và cập nhật trạng thái
                        await RecordSensorIssue(
                            sensor.SensorInfoID,
                            SensorIssueType.NoData,
                            description,
                            SensorStatus.Warning
                        );
                    }
                }
            }
            catch (Exception ex) {
                WriteLog($"Lỗi khi kiểm tra sensor không hoạt động: {ex.Message}", ERROR);
            }
        }


        private void WriteLog(string message, string messageType = "OK")
        {
            try
            {
                string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";


                // Nếu form đã bị dispose hoặc đang đóng, không cập nhật UI
                if (this.IsDisposed || this.Disposing)
                    return;
                if (cboShowlog.Checked == true)
                {
                    // Cập nhật UI một cách an toàn
                    if (txtLog.InvokeRequired)
                    {
                        try
                        {
                            // Sử dụng BeginInvoke thay vì Invoke để tránh block thread hiện tại
                            txtLog.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    AppendLogText(timestampedMessage);
                                }
                                catch
                                {
                                    // Bỏ qua lỗi nếu có khi cập nhật UI
                                }
                            }));
                        }
                        catch
                        {
                            // Bỏ qua nếu không thể invoke lên UI thread
                        }
                    }
                    else
                    {
                        try
                        {
                            AppendLogText(timestampedMessage);
                        }
                        catch
                        {
                            // Bỏ qua lỗi nếu có khi cập nhật UI
                        }
                    }
                }
                else
                {
                }
                // Thêm mới: Lưu vào database
                SaveSensorReadingToDatabase(message, messageType);

                // Ghi ra log file CSV
                WriteLogToFile(timestampedMessage, message);
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không làm crash ứng dụng

                var error = new ErrorLog
                {
                    ErrorMessage = "WriteLog error: " + ex.Message,
                    ErrorTime = DateTime.Now,
                    ErrorSource = "SensorReader",
                    ErrorType = ERROR,
                    StackTrace = ERROR,
                };
                // Không làm gì nữa - đảm bảo không có ngoại lệ nào thoát ra ngoài phương thức WriteLog
            }
        }
        private void AppendLogText(string message)
        {
            // Giới hạn kích thước log hiển thị
            const int maxLogSize = 10000;

            if (txtLog.TextLength > maxLogSize)
            {
                txtLog.Text = txtLog.Text.Substring(txtLog.TextLength - maxLogSize);
            }

            txtLog.AppendText(message + Environment.NewLine);
            txtLog.ScrollToCaret();
        }
        private void WriteLogToFile(string timestamp, string message)
        {
            try
            {
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // Tên file theo ngày
                string datePart = DateTime.Now.ToString("yyyyMMdd");
                string logFileName = $"log_{datePart}.csv";
                string logFilePath = Path.Combine(logDirectory, logFileName);

                string logLine = $"{timestamp},{message}";

                // Ghi nối dòng, dùng UTF8
                using (StreamWriter sw = new StreamWriter(logFilePath, true, Encoding.UTF8))
                {
                    sw.WriteLine(logLine);
                }
            }
            catch
            {
                // Bỏ qua lỗi ghi log file
            }
        }

        private void SaveSensorReadingToDatabase(string message, string messageType)
        {
            try
            {
                using (var db = new CleanroomDbContext())
                {
                    var logReadSensor = new LogReadSensor
                    {
                        Message = message,
                        MessageType = messageType,
                        LogTime = DateTime.Now,
                    };

                    db.LogReadSensors.Add(logReadSensor);
                    db.SaveChanges();
                    if (logReadSensor.MessageType == ERROR)
                    {
                        var error = new ErrorLog
                        {
                            ErrorMessage = "WriteLog error: " + message,
                            ErrorTime = DateTime.Now,
                            ErrorSource = "ApplicationSensorReader",
                            ErrorType = ERROR,
                            StackTrace = ERROR,
                        };
                        LogError(error);
                    }
                }
            }
            catch (Exception ex)
            {

                //var error = new ErrorLog {
                //    ErrorMessage = "Database save error: " + ex.ToString(),
                //    ErrorTime = DateTime.Now,
                //    ErrorSource = "ApplicationSensorReader",
                //    ErrorType = ERROR,
                //    StackTrace = ERROR,
                //};
                //LogError(error);
                // Nếu không thể lưu lỗi vào DB thì ghi ra file

                string errorFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", $"errors_{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(errorFilePath, $"[{DateTime.Now}] {ex.ToString()}\r\n");
            }
        }

        private void LogError(ErrorLog errorLog)
        {
            try
            {
                using (var db = new CleanroomDbContext())
                {

                    db.ErrorLogs.Add(errorLog);
                    db.SaveChanges();
                }
            }
            catch
            {
                // Nếu không thể lưu lỗi vào DB thì ghi ra file
                string errorFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", $"errors_{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(errorFilePath, $"[{DateTime.Now}] {errorLog.ErrorMessage}\r\n");
            }
        }
        private void btnFormTest_Click(object sender, EventArgs e)
        {
            frmTest form = new frmTest();
            form.Show();
        }

        private void btnOpenReport_Click(object sender, EventArgs e)
        {
            FrmReport form = new FrmReport();
            form.Show();
        }
    }
}
