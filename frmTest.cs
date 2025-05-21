using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AppCleanRoom.Models;
using AppCleanRoom.PLC;
using AppCleanRoom.Utilities;
using CleanroomMonitoring.Software.DataContext;
using CleanroomMonitoring.Software.Models;
using CleanroomMonitoring.Software.Utilities;
using NModbus;
using NModbus.Device;
using NModbus.IO;

namespace AppCleanRoom
{
    public partial class frmTest : Form
    {
        private readonly CleanroomDbContext _dbContext;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isMonitoring;
        private readonly int _readIntervalSeconds = 60; // Chu kỳ đọc dữ liệu (giây)
        private readonly int _connectionTimeoutMs = 4000; // Timeout kết nối (ms)
        private readonly int _modbusTimeoutMs = 5000; // Timeout cho đọc Modbus (ms)
        private readonly int _maxRetries = 5; // Số lần thử lại khi kết nối thất bại
        private readonly List<string> _plcAddresses = new List<string> { "10.33.0.110", "10.33.0.111", "10.33.0.112" };
        private readonly int _plcPort = 502;
        private readonly object _logLock = new object(); // Đối tượng khóa cho việc ghi log
                                                         // Lớp để lưu dữ liệu đọc từ PLC
        public frmTest()
        {
            InitializeComponent();
            _dbContext = new CleanroomDbContext();
            InitializeControls();

        }
        private void InitializeControls()
        {
            // Thiết lập các control ban đầu
            
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void WriteLog(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

            lock (_logLock)
            {
           
                // Hiển thị trên giao diện
                if (txtLog.InvokeRequired)
                {
                    txtLog.Invoke(new Action(() => AppendLogText(timestampedMessage)));
                }
                else
                {
                    AppendLogText(timestampedMessage);
                }
            }
        }

        private void AppendLogText(string message)
        {
            // Giới hạn kích thước log hiển thị
            const int maxLogSize = 500000;

            if (txtLog.TextLength > maxLogSize)
            {
                txtLog.Text = txtLog.Text.Substring(txtLog.TextLength - maxLogSize);
            }
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(AppendLogText), message);
                return;
            }
            txtLog.AppendText(message + Environment.NewLine); 
            txtLog.ScrollToCaret();
        }
        
        /// <summary>
        /// 2. Cải tiến phương thức đọc dữ liệu với tối ưu hóa yêu cầu
        /// </summary>
        /// <param name="IpAddress"></param>
        /// <param name="Port"></param>
        /// <returns></returns>
        private async Task<List<PlcData>> ProcessSensorGroup(string IpAddress, int Port)
        {
            try
            {
                ushort startAddress = 110;
                ushort totalRegisters = 300; // Đọc 300 giá trị từ 110 đến 410
                ushort maxRegistersPerRequest = 125; // Giới hạn số thanh ghi mỗi lần đọc

                WriteLog($"Starting to read data from {IpAddress}...");

                List<PlcData> listDataPLC = new List<PlcData>();
                TcpClient client = null;

                try
                {
                    // Tạo kết nối một lần cho tất cả các yêu cầu
                    client = new TcpClient();
                    await client.ConnectAsync(IpAddress, Port);

                    var factory = new ModbusFactory();
                    var master = factory.CreateMaster(client);
                    master.Transport.ReadTimeout = 5000; // Đặt timeout 5 giây

                    // Đọc dữ liệu theo từng phần
                    for (ushort offset = 0; offset < totalRegisters; offset += maxRegistersPerRequest)
                    {
                        ushort registersToRead = (ushort)Math.Min(maxRegistersPerRequest, totalRegisters - offset);
                        ushort currentAddress = (ushort)(startAddress + offset);

                        WriteLog($"Reading {registersToRead} registers from {IpAddress} starting at {currentAddress}");

                        // Đọc dữ liệu với retry
                        ushort[] response = await ReadRegistersWithRetryAsync(master, 1, currentAddress, registersToRead);

                        // Chuyển đổi dữ liệu thành danh sách PlcData
                        for (int i = 0; i < response.Length; i++)
                        {
                            var data = new PlcData
                            {
                                DuLieu = response[i],
                                ThanhGhi = (ushort)(currentAddress + i),
                                IpAddress = IpAddress
                            };
                            listDataPLC.Add(data);
                            WriteLog($"\t{IpAddress}\t{data.ThanhGhi}\t{data.DuLieu}\t");
                        }
                    }

                    WriteLog($"Successfully read {listDataPLC.Count} registers from {IpAddress}");
                    return listDataPLC;
                }
                finally
                {
                    // Đảm bảo đóng kết nối
                    if (client != null && client.Connected)
                    {
                        client.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Error processing PLC {IpAddress}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 3. Phương thức đọc thanh ghi với retry và timeout tốt hơn
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="numberOfPoints"></param>
        /// <param name="maxRetries"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        private async Task<ushort[]> ReadRegistersWithRetryAsync(IModbusMaster master, byte slaveAddress,
    ushort startAddress, ushort numberOfPoints, int maxRetries = 3)
        {
            int attempt = 0;
            Exception lastException = null;

            while (attempt < maxRetries)
            {
                try
                {
                    // Sử dụng semaphore để giới hạn số lượng truy cập đồng thời
                    var result = await master.ReadHoldingRegistersAsync(slaveAddress, startAddress, numberOfPoints);
                    return result;
                }
                catch (Exception ex)
                {
                    attempt++;
                    lastException = ex;
                    WriteLog($"Error reading registers (attempt {attempt}/{maxRetries}): {ex.Message}");

                    if (attempt >= maxRetries)
                    {
                        break;
                    }

                    // Tăng thời gian chờ giữa các lần thử
                    int delay = 1000 * attempt; // 1s, 2s, 3s...
                    await Task.Delay(delay);
                }
            }

            throw new IOException($"Failed to read registers after {maxRetries} attempts", lastException);
        }


        /// <summary>
        /// 4. Thêm lớp quản lý kết nối để cải thiện hiệu suất
        /// </summary>
        public class ModbusConnectionManager
        {
            private static Dictionary<string, IModbusMaster> _connections = new Dictionary<string, IModbusMaster>();
            private static object _lock = new object();

            public static async Task<IModbusMaster> GetConnectionAsync(string ipAddress, int port)
            {
                string key = $"{ipAddress}:{port}";

                lock (_lock)
                {
                    if (_connections.ContainsKey(key) && IsMasterConnected(_connections[key]))
                    {
                        return _connections[key];
                    }
                }

                // Tạo kết nối mới
                var client = new TcpClient();
                await client.ConnectAsync(ipAddress, port);

                var factory = new ModbusFactory();
                var master = factory.CreateMaster(client);
                master.Transport.ReadTimeout = 5000;

                lock (_lock)
                {
                    _connections[key] = master;
                }

                return master;
            }

            private static bool IsMasterConnected(IModbusMaster master)
            {
                // Kiểm tra xem kết nối còn sống không
                try
                {
                    var tcpClient = GetTcpClientFromMaster(master);
                    if (tcpClient != null)
                    {
                        return tcpClient.Connected;
                    }
                }
                catch
                {
                    // Nếu có lỗi, coi như kết nối đã đóng
                }

                return false;
            }

            private static TcpClient GetTcpClientFromMaster(IModbusMaster master)
            {
                // Đây là cách thay thế để lấy TcpClient từ IModbusMaster
                // Lưu ý: Điều này phụ thuộc vào cách triển khai cụ thể của NModbus
                // Chúng ta cần truy cập qua reflection hoặc kiểm tra kiểu cụ thể

                // Thử lấy qua ModbusIpTransport trước
                if (master is ModbusIpMaster ipMaster)
                {
                    // Truy cập transport và stream resource
                    var transport = ipMaster.Transport;
                    if (transport?.StreamResource is IStreamResource streamResource)
                    {
                        // Nếu StreamResource là một wrapper của TcpClient
                        var fieldInfo = streamResource.GetType().GetField("_tcpClient",
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance);

                        if (fieldInfo != null)
                        {
                            return fieldInfo.GetValue(streamResource) as TcpClient;
                        }
                    }
                }

                return null;
            }

            public static void CloseAll()
            {
                lock (_lock)
                {
                    foreach (var master in _connections.Values)
                    {
                        try
                        {
                            var transport = master.Transport as ModbusIpTransport;
                            if (transport != null)
                            {
                                transport.StreamResource?.Dispose();
                            }
                        }
                        catch
                        {
                            // Bỏ qua lỗi khi đóng kết nối
                        }
                    }

                    _connections.Clear();
                }
            }
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            if (cboThanhGhi.Text.Length<1) {
                MessageBox.Show("Vị trí ô nhớ trên PLC không được để trống");
                return;
            }
            try
            {
                //ipAddress=10.33.0.111
                string ipAddress = txtIP.Text.Trim();
                //port=502
                int port = int.Parse(txtPort.Text.Trim());
                using (TcpClient client = new TcpClient(ipAddress, port))
                {
                    var factory = new ModbusFactory();
                    var master = factory.CreateMaster(client);
                    //thanhGhi=110
                    int thanhGhi = int.Parse(cboThanhGhi.Text);
                    txtGiaTriNhanDuoc.Text = ReadDataAsDouble(master, (ushort)thanhGhi).ToString();

                }
            }
            catch (Exception ex)
            {
                txtGiaTriNhanDuoc.Text = "0";
                //   MessageBox.Show(ex.ToString(), "Không kết nối được Sensor");
            }

        }

        private decimal ReadDataAsDouble(IModbusMaster master, ushort address)
        {
            try
            {
                ushort[] response = master.ReadHoldingRegisters(1, address, 1);
                return (decimal)response[0] / 100.0m;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to read data at address {address}: {ex.Message}");
            }
        }

        private async void btnTestvoiMotdiachiIP_Click(object sender, EventArgs e)
        {
            if (txtIPForTest.Text.Length<2)
            {
                MessageBox.Show("Dải IP không được để trống");
                return;
            }
            txtLog.Text = "";
           
            int Port = int.Parse(txtPortForTest.Text);

            WriteLog("# CONNECTED PLC" + Environment.NewLine);
            List<string> plcAddresses = new List<string>();
            plcAddresses.Add(txtIPForTest.Text);

              // Đọc dữ liệu từ tất cả PLC song song
              var tasks = plcAddresses.Select(ip => ProcessSensorGroup(ip, Port)).ToList();
            var results = await Task.WhenAll(tasks);

            // Kết hợp kết quả từ tất cả PLC
            var allPlcData = results.Where(r => r != null).SelectMany(r => r).ToList();

            // Hiển thị thông báo hoàn thành
            WriteLog($"Completed reading data from all PLCs. Total records: {allPlcData.Count}");
        }
    }
}
