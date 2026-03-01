using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;
using OrdersManager.Models;
using System.Globalization;

namespace OrdersManager
{
    public class GoogleSheetService
    {
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static readonly string ApplicationName = "OrdersManager";
        static readonly string SpreadsheetId = "1ZGfgkiU-C9YbRWKA0FaDzF179MPkVdC7YH4HiZUAYIE";
        static readonly string SheetName = "Order";
        static SheetsService service;

        private readonly ILogger<GoogleSheetService> _logger;

        public GoogleSheetService(ILogger<GoogleSheetService> logger)
        {
            _logger = logger;
            try
            {
                GoogleCredential credential;
                using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
                }

                service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                _logger.LogInformation("Khởi tạo kết nối Google Sheet API thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LỖI NGHIÊM TRỌNG: Không thể khởi tạo Google Sheet Service. Vui lòng kiểm tra file credentials.json");
            }
        }

        public List<string> GetConfigData(string sheetName)
        {
            var range = $"{sheetName}!A2:A"; // Chỉ đọc cột A
            var request = service.Spreadsheets.Values.Get(SpreadsheetId, range);
            var response = request.Execute();
            var values = response.Values;

            var list = new List<string>();
            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row.Count > 0) list.Add(row[0].ToString());
                }
            }
            return list;
        }

        public void AddConfigData(string sheetName, string value)
        {
            var valueRange = new ValueRange();
            valueRange.Values = new List<IList<object>> { new List<object> { value } };

            var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, $"{sheetName}!A:A");
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.Execute();
            _logger.LogInformation("Đã THÊM cấu hình vào {Sheet}: {Value}", sheetName, value);
        }

        #region Orders
        // ==========================================
        // QUẢN LÝ ĐƠN HÀNG (ORDERS)
        // ==========================================
        public List<Order> GetAll()
        {
            var range = $"{SheetName}!A:X";
            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(SpreadsheetId, range);
            request.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMATTEDVALUE;
            var response = request.Execute();
            var values = response.Values;
            var list = new List<Order>();

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row.Count == 0 || row[0].ToString() == "Id") continue; // Bỏ Header

                    var order = new Order();
                    order.Id = row.Count > 0 ? row[0].ToString() : "";
                    order.OrderDate = row.Count > 1 ? ParseDate(row[1]) : null;
                    order.Source = row.Count > 2 ? row[2].ToString() : "";
                    order.Warehouse = row.Count > 3 ? row[3].ToString() : "";
                    order.Code = row.Count > 4 ? row[4].ToString() : "";
                    order.Category = row.Count > 5 ? row[5].ToString() : "";
                    order.ProductName = row.Count > 6 ? row[6].ToString() : "";
                    order.Color = row.Count > 7 ? row[7].ToString() : "";
                    order.Size = row.Count > 8 ? row[8].ToString() : "";
                    order.SellingPrice = row.Count > 9 ? ParseDecimal(row[9]) : 0;
                    order.Quantity = row.Count > 10 ? int.Parse(row[10].ToString()) : 0;

                    // Cột 11 (L) là Công thức: Tổng tiền
                    order.TotalAmount = row.Count > 11 ? ParseDecimal(row[11]) : 0;

                    order.CustomerName = row.Count > 12 ? row[12].ToString() : "";
                    order.Deposit = row.Count > 13 ? ParseDecimal(row[13]) : 0;
                    order.Discount = row.Count > 14 ? ParseDecimal(row[14]) : 0;

                    // Cột 15 (P) là Công thức: Còn lại
                    order.RemainingAmount = row.Count > 15 ? ParseDecimal(row[15]) : 0;

                    order.ArrivalDate = row.Count > 16 ? ParseDate(row[16]) : null;
                    order.PaymentDate = row.Count > 17 ? ParseDate(row[17]) : null;
                    order.ImportPrice = row.Count > 18 ? ParseDecimal(row[18]) : 0;

                    // Cột 19 (T), 20 (U) là công thức
                    order.TotalImportCost = row.Count > 19 ? ParseDecimal(row[19]) : 0;
                    order.Profit = row.Count > 20 ? ParseDecimal(row[20]) : 0;
                    order.Status = row.Count > 21 ? row[21].ToString() : null;

                    order.PhoneNumber = row.Count > 22 ? row[22].ToString() : "";
                    order.ShippingAddress = row.Count > 23 ? row[23].ToString() : "";
                    list.Add(order);
                }
            }
            return list;
        }

        public void Add(Order order)
        {
            try
            {
                var valueRange = new ValueRange();

                // Xử lý ngày tháng về string
                string orderDate = order.OrderDate.HasValue ? order.OrderDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "";
                string arrDate = order.ArrivalDate.HasValue ? order.ArrivalDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "";
                string payDate = order.PaymentDate.HasValue ? order.PaymentDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "";
                var phoneNumber = "'" + order.PhoneNumber;
                // QUAN TRỌNG: Với các cột công thức, ta truyền null hoặc string rỗng để Google Sheet tự tính (nếu bạn set ArrayFormula trong sheet)
                // Hoặc ta truyền chính xác công thức vào (ví dụ "=J2*K2"). 
                // Ở đây tôi dùng cách set công thức R1C1 notation (tương đối) để đơn giản.

                var objectList = new List<object>() {
                order.Id,           // A
                orderDate,          // B
                order.Source,       // C
                order.Warehouse,    // D
                order.Code,         // E
                order.Category,     // F
                order.ProductName,  // G
                order.Color,        // H
                order.Size,         // I
                order.SellingPrice, // J
                order.Quantity,     // K
                "=J:J*K:K",         // L (Formula: Giá * SL) - Google Sheet tự map dòng
                order.CustomerName, // M
                order.Deposit,      // N
                order.Discount,     // O
                "=L:L-N:N-O:O",     // P (Formula: Tổng - Cọc - CK)
                arrDate,            // Q
                payDate,            // R
                order.ImportPrice,  // S
                "=S:S*K:K",         // T (Formula: Giá Nhập * SL)
                "=L:L-T:T",          // U (Formula: Tổng tiền bán - Thành tiền nhập
                order.Status,       // V
                phoneNumber,  // W (Mới)
                order.ShippingAddress // X (Mới)
            };

                valueRange.Values = new List<IList<object>> { objectList };
                var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, $"{SheetName}!A:U");
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                appendRequest.Execute();

                _logger.LogInformation("Đã THÊM đơn hàng mới: Mã={Code}, Khách={Customer}, SĐT={Phone}, SP={Product}, SL=x{Quantity}, Đơn giá={Price}",
                    order.Code, order.CustomerName, order.PhoneNumber, order.ProductName, order.Quantity, order.SellingPrice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi THÊM đơn hàng Mã={Code}", order.Code);
            }
        }

        // 3. Hàm Xóa (Delete)
        public void Delete(string id)
        {
            try
            {
                int rowId = FindRowId(SheetName, id);
                if (rowId == -1)
                {
                    _logger.LogWarning("Cảnh báo: Không tìm thấy đơn hàng ID={Id} để XÓA", id);
                    return;
                }

                var requestBody = new BatchUpdateSpreadsheetRequest();
                requestBody.Requests = new List<Request>();

                requestBody.Requests.Add(new Request
                {
                    DeleteDimension = new DeleteDimensionRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = 732019451,
                            Dimension = "ROWS",
                            StartIndex = rowId - 1, // API tính từ 0
                            EndIndex = rowId        // API xóa range [Start, End)
                        }
                    }
                });

                var batchRequest = service.Spreadsheets.BatchUpdate(requestBody, SpreadsheetId);
                batchRequest.Execute();
                _logger.LogInformation("Đã XÓA đơn hàng dòng {RowId}, ID={Id}", rowId, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi XÓA đơn hàng ID={Id}", id);
            }
        }

        // 2. Hàm Cập nhật (Update)
        public void Update(Order order)
        {
            try
            {
                int rowId = FindRowId(SheetName, order.Id);
                if (rowId == -1)
                {
                    _logger.LogWarning("Cảnh báo: Không tìm thấy đơn hàng ID={Id} để CẬP NHẬT", order.Id);
                    return;
                }

                var range = $"{SheetName}!A{rowId}:X{rowId}";

                // Xử lý ngày tháng về string
                string orderDate = order.OrderDate.HasValue ? order.OrderDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "";
                string arrDate = order.ArrivalDate.HasValue ? order.ArrivalDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "";
                string payDate = order.PaymentDate.HasValue ? order.PaymentDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "";
                var phoneNumber = "'" + order.PhoneNumber;

                // 1. Update Hàng hóa (A->K)
                UpdateRange(rowId, "A", "K", new List<object> {
                order.Id, orderDate, order.Source, order.Warehouse, order.Code,
                order.Category, order.ProductName, order.Color, order.Size,
                order.SellingPrice, order.Quantity
                });
                _logger.LogInformation("Đã SỬA thông tin hàng hóa đơn hàng dòng {RowId}: Mã={Code}, Khách={Customer}, SP={Product}, SL=x{Quantity}, Đơn giá={Price}",
                    rowId, order.Code, order.CustomerName, order.ProductName, order.Quantity, order.SellingPrice);
                // 2. Update Tài chính & Khách (M->O)
                UpdateRange(rowId, "M", "O", new List<object> {
                    order.CustomerName, order.Deposit, order.Discount
                });
                _logger.LogInformation("Đã SỬA thông tin tài chính đơn hàng dòng {RowId}: Mã={Code}, Khách={Customer}, Đã trả={Deposit}, Ưu đãi={Discount}",
                    rowId, order.Code, order.CustomerName, order.Deposit, order.Discount);
                // 3. Update SĐT & Địa chỉ (W->X) <--- QUAN TRỌNG
                UpdateRange(rowId, "V", "X", new List<object> {
                    order.Status, phoneNumber, order.ShippingAddress
                });
                _logger.LogInformation("Đã SỬA thông tin KH của đơn hàng dòng {RowId}: Mã={Code}, Khách={Customer}, SĐT={PhoneNumber}, Địa chỉ={Address}, Trạng thái={Status}",
                    rowId, order.Code, order.CustomerName, phoneNumber, order.ShippingAddress, order.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi SỬA đơn hàng ID={Id}", order.Id);
            }
        }

        public void BulkUpdateOrder(string id, string newStatus, DateTime? arrivalDate, decimal? importPrice, decimal? paidAmount)
        {
            try
            {
                int rowId = FindRowId(SheetName, id);
                if (rowId == -1)
                {
                    _logger.LogWarning("Bulk Update: Không tìm thấy đơn hàng ID={Id}", id);
                    return;
                }

                // 1. Update Trạng thái (Cột V)
                var rangeStatus = $"{SheetName}!V{rowId}";
                var reqStatus = service.Spreadsheets.Values.Update(
                    new ValueRange { Values = new List<IList<object>> { new List<object> { newStatus } } },
                    SpreadsheetId, rangeStatus);
                reqStatus.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                reqStatus.Execute();

                // 2. Xử lý logic HÀNG VỀ (Cập nhật Ngày về & Giá vốn)
                if (arrivalDate.HasValue)
                {
                    var reqDate = service.Spreadsheets.Values.Update(
                        new ValueRange { Values = new List<IList<object>> { new List<object> { arrivalDate.Value.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture) } } },
                        SpreadsheetId, $"{SheetName}!Q{rowId}");
                    reqDate.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    reqDate.Execute();
                }
                if (importPrice.HasValue)
                {
                    var reqPrice = service.Spreadsheets.Values.Update(
                        new ValueRange { Values = new List<IList<object>> { new List<object> { importPrice.Value } } },
                        SpreadsheetId, $"{SheetName}!S{rowId}");
                    reqPrice.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    reqPrice.Execute();
                }

                // 3. Xử lý logic ĐÃ GIAO (Cập nhật Tiền đã trả & Ngày thanh toán)
                if (newStatus == "Đã giao" && paidAmount.HasValue)
                {
                    // Cập nhật Cột N (Deposit/Đã thanh toán)
                    var reqPaid = service.Spreadsheets.Values.Update(
                        new ValueRange { Values = new List<IList<object>> { new List<object> { paidAmount.Value } } },
                        SpreadsheetId, $"{SheetName}!N{rowId}");
                    reqPaid.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    reqPaid.Execute();

                    // Cập nhật Cột R (Ngày thanh toán - PaymentDate) -> Set là hôm nay
                    var today = DateTime.Now.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    var reqPayDate = service.Spreadsheets.Values.Update(
                        new ValueRange { Values = new List<IList<object>> { new List<object> { today } } },
                        SpreadsheetId, $"{SheetName}!R{rowId}");
                    reqPayDate.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    reqPayDate.Execute();
                }
                _logger.LogInformation("Đã CẬP NHẬT TRẠNG THÁI đơn hàng dòng {RowId} -> {Status}. Giá nhập: {Import}, TT: {Paid}",
                    rowId, newStatus, importPrice, paidAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi Bulk Update đơn hàng ID={Id}", id);
            }
        }
        #endregion

        #region Customers
        // ==========================================
        // QUẢN LÝ KHÁCH HÀNG (CUSTOMERS)
        // ==========================================
        public List<Customer> GetCustomers()
        {
            var range = "KhachHang!A:F";

            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(SpreadsheetId, range);
            var response = request.Execute();
            var values = response.Values;
            var list = new List<Customer>();

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row.Count == 0 || row[0].ToString() == "ID") continue;

                    list.Add(new Customer
                    {
                        Id = row.Count > 0 ? row[0].ToString() : "",
                        FullName = row.Count > 1 ? row[1].ToString() : "",
                        PhoneNumber = row.Count > 2 ? row[2].ToString() : "",
                        Reference = row.Count > 3 ? row[3].ToString() : "",
                        Address = row.Count > 4 ? row[4].ToString() : "",
                        Note = row.Count > 5 ? row[5].ToString() : ""
                    });
                }
            }
            return list;
        }

        // 2. Hàm Thêm Khách Hàng (Create)
        public void AddCustomer(Customer customer)
        {
            try
            {
                var valueRange = new ValueRange();

                var phoneNumber = "'" + customer.PhoneNumber;

                var objectList = new List<object>() {
                                customer.Id,
                                customer.FullName,
                                phoneNumber,
                                customer.Reference?? String.Empty,
                                customer.Address,
                                customer.Note?? String.Empty
                            };
                valueRange.Values = new List<IList<object>> { objectList };

                // Ghi vào sheet KhachHang
                var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, "KhachHang!A:E");
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                appendRequest.Execute();
                _logger.LogInformation("Đã THÊM khách hàng: {Name} - SĐT: {Phone}", customer.FullName, customer.PhoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi THÊM khách hàng SĐT={Phone}", customer.PhoneNumber);
            }
        }

        // 3. Hàm Sửa Khách Hàng (Update)
        public void UpdateCustomer(Customer customer)
        {
            try
            {
                // Tìm dòng trong sheet KhachHang
                int rowId = FindRowId("KhachHang", customer.Id);
                if (rowId == -1)
                {
                    _logger.LogWarning("Không tìm thấy khách hàng ID={Id} để CẬP NHẬT", customer.Id);
                    return;
                }
                var phoneNumber = "'" + customer.PhoneNumber;
                var range = $"KhachHang!A{rowId}:E{rowId}";
                var valueRange = new ValueRange();
                var objectList = new List<object>() {
                                customer.Id, // Giữ nguyên ID
                                customer.FullName,
                                phoneNumber,
                                customer.Reference?? String.Empty,
                                customer.Address,
                                customer.Note?? String.Empty
                            };
                valueRange.Values = new List<IList<object>> { objectList };

                var updateRequest = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                updateRequest.Execute();
                _logger.LogInformation("Đã SỬA khách hàng dòng {RowId}: {Name} - {Phone}", rowId, customer.FullName, customer.PhoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi SỬA khách hàng ID={Id}", customer.Id);
            }
        }

        // 4. Hàm Xóa Khách Hàng (Delete)
        public void DeleteCustomer(string id)
        {
            try
            {
                int rowId = FindRowId("KhachHang", id);
                if (rowId == -1)
                {
                    _logger.LogWarning("Không tìm thấy khách hàng ID={Id} để XÓA", id);
                    return;
                }

                var requestBody = new BatchUpdateSpreadsheetRequest();
                requestBody.Requests = new List<Request>();

                requestBody.Requests.Add(new Request
                {
                    DeleteDimension = new DeleteDimensionRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = 2138276695,
                            Dimension = "ROWS",
                            StartIndex = rowId - 1,
                            EndIndex = rowId
                        }
                    }
                });

                var batchRequest = service.Spreadsheets.BatchUpdate(requestBody, SpreadsheetId);
                batchRequest.Execute();
                _logger.LogInformation("Đã XÓA khách hàng dòng {RowId}, ID={Id}", rowId, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi XÓA khách hàng ID={Id}", id);
            }
        }

        public Customer GetCustomerByPhone(string phone)
        {
            var customers = GetCustomers();
            return customers.FirstOrDefault(c => c.PhoneNumber == phone);
        }
        #endregion

        #region Products
        // ==========================================
        // QUẢN LÝ SẢN PHẨM (PRODUCTS)
        // ==========================================
        public List<Product> GetProducts()
        {
            var range = "SanPham!A:G";
            var request = service.Spreadsheets.Values.Get(SpreadsheetId, range);
            var response = request.Execute();
            var values = response.Values;
            var list = new List<Product>();

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row.Count == 0 || row[0].ToString() == "SKU") continue;

                    list.Add(new Product
                    {
                        Sku = row.Count > 0 ? row[0].ToString() : "",
                        Name = row.Count > 1 ? row[1].ToString() : "",
                        Category = row.Count > 2 ? row[2].ToString() : "",
                        ImportPrice = row.Count > 3 ? ParseDecimal(row[3]) : 0,
                        SellingPrice = row.Count > 4 ? ParseDecimal(row[4]) : 0,
                        Source = row.Count > 5 ? row[5].ToString() : "",
                        Warehouse = row.Count > 6 ? row[6].ToString() : "",
                    });
                }
            }
            return list;
        }

        public void AddProduct(Product p)
        {
            try
            {
                var valueRange = new ValueRange();
                var objectList = new List<object>() {
                    p.Sku.ToUpper(),
                    p.Name,
                    p.Category,
                    p.ImportPrice,
                    p.SellingPrice,
                    p.Source,
                    p.Warehouse
                };
                valueRange.Values = new List<IList<object>> { objectList };

                var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, "SanPham!A:E");
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                appendRequest.Execute();
                _logger.LogInformation("Đã THÊM sản phẩm mới: SKU={SKU}, Tên={Name}", p.Sku, p.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi THÊM sản phẩm SKU={SKU}", p.Sku);
            }
        }

        // 3. Cập nhật Sản phẩm
        public void UpdateProduct(Product p)
        {
            try
            {
                int rowId = FindRowId("SanPham", p.Sku);
                if (rowId == -1)
                {
                    _logger.LogWarning("Không tìm thấy sản phẩm SKU={SKU} để CẬP NHẬT", p.Sku);
                    return;
                }

                // Update từ cột B (Tên) đến E (Giá bán). Cột A (SKU) giữ nguyên để làm khóa.
                var range = $"SanPham!B{rowId}:E{rowId}";

                var valueRange = new ValueRange();
                var objectList = new List<object>() {
                    p.Name,
                    p.Category,
                    p.ImportPrice,
                    p.SellingPrice,
                    p.Source,
                    p.Warehouse
                };
                valueRange.Values = new List<IList<object>> { objectList };

                var updateRequest = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                updateRequest.Execute();
                _logger.LogInformation("Đã SỬA sản phẩm dòng {RowId}: SKU={SKU}, Tên={Name}", rowId, p.Sku, p.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi SỬA sản phẩm SKU={SKU}", p.Sku);
            }
        }

        // 4. Xóa Sản phẩm
        public void DeleteProduct(string sku)
        {
            try
            {
                int rowId = FindRowId("SanPham", sku);
                if (rowId == -1)
                {
                    _logger.LogWarning("Không tìm thấy sản phẩm SKU={SKU} để XÓA", sku);
                    return;
                }

                var requestBody = new BatchUpdateSpreadsheetRequest();
                requestBody.Requests = new List<Request>();
                requestBody.Requests.Add(new Request
                {
                    DeleteDimension = new DeleteDimensionRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = 309597087,
                            Dimension = "ROWS",
                            StartIndex = rowId - 1,
                            EndIndex = rowId
                        }
                    }
                });

                var batchRequest = service.Spreadsheets.BatchUpdate(requestBody, SpreadsheetId);
                batchRequest.Execute();
                _logger.LogInformation("Đã XÓA sản phẩm dòng {RowId}, SKU={SKU}", rowId, sku);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi XÓA sản phẩm SKU={SKU}", sku);
            }
        }
        #endregion

        #region helper
        // ==========================================
        // CÁC HÀM HELPER KHÁC
        // ==========================================
        // 1. Hàm tìm dòng dựa trên ID (Helper)
        private int FindRowId(string sheetName, string id)
        {
            var range = $"{sheetName}!A:A"; // Đọc cột A của sheet được truyền vào
            var request = service.Spreadsheets.Values.Get(SpreadsheetId, range);
            var response = request.Execute();
            var values = response.Values;

            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    // Kiểm tra ID khớp (Bỏ qua dòng header nếu cần)
                    if (values[i].Count > 0 && values[i][0].ToString() == id)
                    {
                        return i + 1; // Row index trong Sheet bắt đầu từ 1
                    }
                }
            }
            return -1;
        }

        private void UpdateRange(int rowId, string colStart, string colEnd, List<object> data)
        {
            var range = $"{SheetName}!{colStart}{rowId}:{colEnd}{rowId}";
            var valueRange = new ValueRange { Values = new List<IList<object>> { data } };
            var request = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            request.Execute();
        }

        private decimal ParseDecimal(object value)
        {
            if (value == null) return 0;
            // Xử lý chuỗi kiểu "1.000" hoặc "1,000" tùy locale
            string s = value.ToString().Replace(".", "").Replace(",", "");
            if (decimal.TryParse(s, out decimal result)) return result;
            return 0;
        }

        private DateTime? ParseDate(object value)
        {
            if (value == null) return null;
            if (DateTime.TryParseExact(value.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                return result;
            return null;
        }
        #endregion
    }
}