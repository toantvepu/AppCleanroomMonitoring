using OfficeOpenXml;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using LicenseContext = OfficeOpenXml.LicenseContext;

namespace AppCleanRoom
{
    public partial class FrmReport : Form
    {
     
        // Cấu hình kết nối database
        private string connectionString = @"data source=172.16.33.123;initial catalog=dbCleanRoomAsahi;user id=aih;password=123456;MultipleActiveResultSets=True;App=EntityFramework";

        // Các biến phân trang
        private int currentPage = 1;
        private int pageSize = 50;
        private int totalRecords = 0;
        private int totalPages = 0;

        // Các bộ lọc
        private int? selectedSensorId = null;
        private DateTime? startDate = null;
        private DateTime? endDate = null;
        private string ipAddressFilter = null;
        private string modbusAddressFilter = null;

        public FrmReport()
        {
            InitializeComponent();
            SetupDataGridView();
            LoadSensorDropdown();
           
        }
     
        private void SetupDataGridView()
        {
            // Cấu hình DataGridView
            dataGridView1.AutoGenerateColumns = false;

            // Thêm các cột
            dataGridView1.Columns.Add("SensorName", "Serial Number");
            
            dataGridView1.Columns.Add("ReadingValue", "Giá trị");
            dataGridView1.Columns.Add("ReadingTime", "Thời gian");
            dataGridView1.Columns.Add("ModbusAddress", "Modbus Address");
            dataGridView1.Columns.Add("IpAddress", "IP Address");
            dataGridView1.Columns.Add("TypeName", "Loại cảm biến");
            dataGridView1.Columns.Add("Unit", "Đơn vị");
            
            dataGridView1.Columns.Add("IsValid", "Hợp lệ");

            // Định dạng cột thời gian
            dataGridView1.Columns["ReadingTime"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";

            // Định dạng cột giá trị
            dataGridView1.Columns["ReadingValue"].DefaultCellStyle.Format = "0.0";

            // Đặt thuộc tính ReadOnly
            dataGridView1.ReadOnly = true;

            // Tự động điều chỉnh kích thước
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void LoadSensorDropdown()
        {
            try {
                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    connection.Open();
                    SqlCommand command = new SqlCommand(
                        "SELECT SensorInfoID, SensorName AS DisplayName " +
                        "FROM SensorInfo " +
                        "WHERE IsActive = 1 " +
                        "ORDER BY SensorName", connection);

                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable sensorTable = new DataTable();
                    adapter.Fill(sensorTable);

                    // Thêm mục "Tất cả" vào đầu dropdown
                    DataRow allRow = sensorTable.NewRow();
                    allRow["SensorInfoID"] = DBNull.Value;
                    allRow["DisplayName"] = "-- Tất cả cảm biến --";
                    sensorTable.Rows.InsertAt(allRow, 0);

                    // Gán nguồn dữ liệu cho combobox
                    cboSensor.DataSource = sensorTable;
                    cboSensor.DisplayMember = "DisplayName";
                    cboSensor.ValueMember = "SensorInfoID";
                }
            }
            catch (Exception ex) {
                MessageBox.Show("Lỗi khi tải danh sách cảm biến: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void FrmReport_Load(object sender, EventArgs e)
        {
            dtpStartDate.Format = DateTimePickerFormat.Long; // Hoặc DateTimePickerFormat.Short
            dtpEndDate.Format = DateTimePickerFormat.Long;   // Hoặc DateTimePickerFormat.Short
            // Đặt giá trị mặc định cho dtpEndDate là ngày giờ hiện tại
            dtpEndDate.Value = DateTime.Now;

            // Đặt giá trị mặc định cho dtpStartDate là ngày giờ hiện tại trừ đi 5 giờ
            dtpStartDate.Value = DateTime.Now.AddHours(-1);


            // Đặt lại trang về 1 khi tải dữ liệu mới
            currentPage = 1;

            // Cập nhật bộ lọc
            UpdateFilters();

            // Tải dữ liệu
            LoadData();
        }
        private void UpdateFilters()
        {
            // Cập nhật bộ lọc sensor ID
            if (cboSensor.SelectedValue != null && cboSensor.SelectedValue != DBNull.Value)
                selectedSensorId = Convert.ToInt32(cboSensor.SelectedValue);
            else
                selectedSensorId = null;

            // Cập nhật bộ lọc ngày tháng
            startDate = dtpStartDate.Checked ? dtpStartDate.Value : (DateTime?)null;
            endDate = dtpEndDate.Checked ? dtpEndDate.Value.AddDays(1).AddSeconds(-1) : (DateTime?)null;

            // Cập nhật bộ lọc IP Address và Modbus Address
            TextBox txtIpAddress = this.Controls.Find("txtIpAddress", true).FirstOrDefault() as TextBox;
            if (txtIpAddress != null && !string.IsNullOrWhiteSpace(txtIpAddress.Text))
                ipAddressFilter = txtIpAddress.Text.Trim();
            else
                ipAddressFilter = null;

            TextBox txtModbusAddress = this.Controls.Find("txtModbusAddress", true).FirstOrDefault() as TextBox;
            if (txtModbusAddress != null && !string.IsNullOrWhiteSpace(txtModbusAddress.Text))
                modbusAddressFilter = txtModbusAddress.Text.Trim();
            else
                modbusAddressFilter = null;
        }
        private void btnLoadData_Click(object sender, EventArgs e)
        {
            // Đặt lại trang về 1 khi tải dữ liệu mới
            currentPage = 1;

            // Cập nhật bộ lọc
            UpdateFilters();

            // Tải dữ liệu
            LoadData();
        }

        private void LoadData()
        {
            try {
                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    connection.Open();

                    // Tạo command cho stored procedure
                    SqlCommand command = new SqlCommand("sp_GetSensorReadingsPaged", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    // Thêm tham số
                    command.Parameters.AddWithValue("@PageNumber", currentPage);
                    command.Parameters.AddWithValue("@PageSize", pageSize);
                    command.Parameters.AddWithValue("@SensorInfoID", selectedSensorId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@StartDate", startDate ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EndDate", endDate ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IpAddress", ipAddressFilter ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@ModbusAddress", modbusAddressFilter ?? (object)DBNull.Value);

                    // Tạo DataAdapter và DataSet
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataSet dataSet = new DataSet();
                    adapter.Fill(dataSet);

                    // Bảng đầu tiên chứa tổng số bản ghi
                    if (dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0) {
                        totalRecords = Convert.ToInt32(dataSet.Tables[0].Rows[0]["TotalRecords"]);
                        totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                        // Cập nhật UI phân trang
                        UpdatePaginationUI();
                    }

                    // Bảng thứ hai chứa dữ liệu
                    if (dataSet.Tables.Count > 1) {
                        // Xóa dữ liệu cũ
                        dataGridView1.Rows.Clear();

                        // Thêm dữ liệu mới
                        foreach (DataRow row in dataSet.Tables[1].Rows) {
                            int rowIndex = dataGridView1.Rows.Add();
                            DataGridViewRow gridRow = dataGridView1.Rows[rowIndex];

                            gridRow.Cells["ReadingTime"].Value = row["ReadingTime"];
                            gridRow.Cells["TypeName"].Value = row["TypeName"];
                            gridRow.Cells["ReadingValue"].Value = row["ReadingValue"];
                            gridRow.Cells["Unit"].Value = row["Unit"];
                            gridRow.Cells["SensorName"].Value = row["SensorName"];
                            gridRow.Cells["IpAddress"].Value = row["IpAddress"];
                            gridRow.Cells["ModbusAddress"].Value = row["ModbusAddress"];
                            gridRow.Cells["IsValid"].Value = Convert.ToBoolean(row["IsValid"]) ? "Hợp lệ" : "Không hợp lệ";

                            // Đánh dấu màu cho dữ liệu không hợp lệ
                            if (!Convert.ToBoolean(row["IsValid"])) {
                                gridRow.DefaultCellStyle.BackColor = Color.LightPink;
                            }

                            // Đánh dấu màu cho cảnh báo nếu giá trị vượt ngưỡng
                            decimal readingValue = Convert.ToDecimal(row["ReadingValue"]);
                            decimal? lowThreshold = row["LowAlertThreshold"] as decimal?;
                            decimal? highThreshold = row["HighAlertThreshold"] as decimal?;

                            if ((lowThreshold.HasValue && readingValue < lowThreshold.Value) ||
                                (highThreshold.HasValue && readingValue > highThreshold.Value)) {
                                gridRow.Cells["ReadingValue"].Style.BackColor = Color.Yellow;
                                gridRow.Cells["ReadingValue"].Style.ForeColor = Color.Red;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                MessageBox.Show("Lỗi khi tải dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdatePaginationUI()
        {
            // Cập nhật label hiển thị thông tin phân trang
            lblPagination.Text = $"Trang {currentPage}/{totalPages} (Tổng: {totalRecords} bản ghi)";

            // Cập nhật trạng thái các nút điều hướng
            btnFirst.Enabled = currentPage > 1;
            btnPrevious.Enabled = currentPage > 1;
            btnNext.Enabled = currentPage < totalPages;
            btnLast.Enabled = currentPage < totalPages;
        }

        private void btnFirst_Click(object sender, EventArgs e)
        {
            if (currentPage > 1) {
                currentPage = 1;
                LoadData();
            }
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            if (currentPage > 1) {
                currentPage--;
                LoadData();
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (currentPage < totalPages) {
                currentPage++;
                LoadData();
            }
        }

        private void btnLast_Click(object sender, EventArgs e)
        {
            if (currentPage < totalPages) {
                currentPage = totalPages;
                LoadData();
            }
        }

        private void btnExportExcel_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0) {
                MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try {
                // Tạo SaveFileDialog để chọn vị trí lưu file
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Filter = "Excel Files|*.xlsx";
                saveDialog.Title = "Xuất dữ liệu ra Excel";
                saveDialog.FileName = $"SensorData_{DateTime.Now:yyyyMMdd_HHmmss}";

                if (saveDialog.ShowDialog() == DialogResult.OK) {
                    // Sử dụng thư viện xuất Excel (bạn cần thêm tham chiếu đến thư viện Excel)
                    // Đây là đoạn code mẫu, bạn cần cài đặt thư viện Excel hoặc EPPlus
                    ExportToExcel(saveDialog.FileName);
                    MessageBox.Show("Xuất dữ liệu thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex) {
                MessageBox.Show("Lỗi khi xuất dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void ExportToExcel(string filePath)
        {
            try {
                // Đảm bảo đã thêm tham chiếu đến thư viện EPPlus
                // NuGet: Install-Package EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new OfficeOpenXml.ExcelPackage(new FileInfo(filePath))) {
                    // Tạo một worksheet mới
                    var worksheet = package.Workbook.Worksheets.Add("Sensor Data");

                    // Định dạng tiêu đề
                    using (var range = worksheet.Cells[1, 1, 1, dataGridView1.Columns.Count]) {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        range.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                    }

                    // Thêm tiêu đề cột
                    for (int i = 0; i < dataGridView1.Columns.Count; i++) {
                        worksheet.Cells[1, i + 1].Value = dataGridView1.Columns[i].HeaderText;
                    }

                    // Thêm dữ liệu từ DataGridView
                    for (int i = 0; i < dataGridView1.Rows.Count; i++) {
                        for (int j = 0; j < dataGridView1.Columns.Count; j++) {
                            var value = dataGridView1.Rows[i].Cells[j].Value;

                            // Xử lý trường hợp đặc biệt
                            if (dataGridView1.Columns[j].Name == "ReadingTime" && value != null) {
                                // Đối với cột thời gian, lưu dạng DateTime để Excel có thể định dạng đúng
                                if (DateTime.TryParse(value.ToString(), out DateTime dateValue)) {
                                    worksheet.Cells[i + 2, j + 1].Value = dateValue;
                                    worksheet.Cells[i + 2, j + 1].Style.Numberformat.Format = "dd/MM/yyyy HH:mm:ss";
                                }
                                else {
                                    worksheet.Cells[i + 2, j + 1].Value = value;
                                }
                            }
                            else if (dataGridView1.Columns[j].Name == "ReadingValue" && value != null) {
                                // Đối với cột giá trị, lưu dạng số để Excel có thể định dạng đúng
                                if (decimal.TryParse(value.ToString(), out decimal numValue)) {
                                    worksheet.Cells[i + 2, j + 1].Value = numValue;
                                    worksheet.Cells[i + 2, j + 1].Style.Numberformat.Format = "0.0";
                                }
                                else {
                                    worksheet.Cells[i + 2, j + 1].Value = value;
                                }

                                // Nếu ô có màu nền vàng (cảnh báo), giữ nguyên định dạng trong Excel
                                if (dataGridView1.Rows[i].Cells[j].Style.BackColor == System.Drawing.Color.Yellow) {
                                    worksheet.Cells[i + 2, j + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    worksheet.Cells[i + 2, j + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                                    worksheet.Cells[i + 2, j + 1].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                                }
                            }
                            else if (dataGridView1.Columns[j].Name == "IsValid") {
                                worksheet.Cells[i + 2, j + 1].Value = value?.ToString();

                                // Đánh dấu các dòng không hợp lệ với màu hồng nhạt
                                if (value?.ToString() == "Không hợp lệ") {
                                    var rowRange = worksheet.Cells[i + 2, 1, i + 2, dataGridView1.Columns.Count];
                                    rowRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    rowRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightPink);
                                }
                            }
                            else {
                                // Các cột khác giữ nguyên giá trị
                                worksheet.Cells[i + 2, j + 1].Value = value?.ToString();
                            }
                        }
                    }

                    // Tự động điều chỉnh độ rộng cột
                    //worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Đặt viền cho toàn bộ bảng
                    using (var range = worksheet.Cells[1, 1, dataGridView1.Rows.Count + 1, dataGridView1.Columns.Count]) {
                        range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    }

                    // Thêm thông tin đầu trang
                    int lastRow = dataGridView1.Rows.Count + 4;
                    worksheet.Cells[lastRow, 1].Value = "Báo cáo dữ liệu cảm biến";
                    worksheet.Cells[lastRow, 1].Style.Font.Bold = true;

                    worksheet.Cells[lastRow + 1, 1].Value = $"Thời gian xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

                    // Thêm thông tin bộ lọc đã sử dụng
                    worksheet.Cells[lastRow + 2, 1].Value = "Bộ lọc áp dụng:";
                    worksheet.Cells[lastRow + 2, 1].Style.Font.Bold = true;

                    if (selectedSensorId.HasValue)
                        worksheet.Cells[lastRow + 3, 1].Value = $"Cảm biến: {cboSensor.Text}";

                    if (startDate.HasValue)
                        worksheet.Cells[lastRow + 4, 1].Value = $"Từ ngày: {startDate:dd/MM/yyyy HH:mm:ss}";

                    if (endDate.HasValue)
                        worksheet.Cells[lastRow + 5, 1].Value = $"Đến ngày: {endDate:dd/MM/yyyy HH:mm:ss}";

                    if (!string.IsNullOrEmpty(ipAddressFilter))
                        worksheet.Cells[lastRow + 6, 1].Value = $"IP Address: {ipAddressFilter}";

                    if (!string.IsNullOrEmpty(modbusAddressFilter))
                        worksheet.Cells[lastRow + 7, 1].Value = $"Modbus Address: {modbusAddressFilter}";

                    // Lưu file Excel
                    package.Save();
                }
            }
            catch (Exception ex) {
                MessageBox.Show($"Lỗi khi xuất Excel: {ex.ToString()}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                string str = ex.ToString();
                return;
            }
        }
    }
}
