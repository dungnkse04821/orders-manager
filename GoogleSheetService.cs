using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
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

        public GoogleSheetService()
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
        }

        // 3. Hàm Xóa (Delete)
        public void Delete(string id)
        {
            int rowId = FindRowId(SheetName, id);
            if (rowId == -1) return;

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
        }

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

        // 2. Hàm Cập nhật (Update)
        public void Update(Order order)
        {
            int rowId = FindRowId(SheetName, order.Id);
            if (rowId == -1) return;

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

            // 2. Update Tài chính & Khách (M->O)
            UpdateRange(rowId, "M", "O", new List<object> {
                order.CustomerName, order.Deposit, order.Discount
            });

            // 3. Update SĐT & Địa chỉ (W->X) <--- QUAN TRỌNG
            UpdateRange(rowId, "V", "X", new List<object> {
                order.Status, phoneNumber, order.ShippingAddress
            });


            //var valueRange = new ValueRange();
            //var objectList = new List<object>() {
            //    order.Id,           // A
            //    orderDate,          // B
            //    order.Source,       // C
            //    order.Warehouse,    // D
            //    order.Code,         // E
            //    order.Category,     // F
            //    order.ProductName,  // G
            //    order.Color,        // H
            //    order.Size,         // I
            //    order.SellingPrice, // J
            //    order.Quantity,     // K
            //    "=J:J*K:K",         // L (Formula: Giá * SL) - Google Sheet tự map dòng
            //    order.CustomerName, // M
            //    order.Deposit,      // N
            //    order.Discount,     // O
            //    "=L:L-N:N-O:O",     // P (Formula: Tổng - Cọc - CK)
            //    arrDate,            // Q
            //    payDate,            // R
            //    order.ImportPrice,  // S
            //    null,               // T (Tổng vốn - Để null)
            //    null,               // U (Lãi - Để null)
            //    order.Status,       // V
            //    order.PhoneNumber,  // W (MỚI)
            //    order.ShippingAddress // X (MỚI)
            //};

            //valueRange.Values = new List<IList<object>> { objectList };
            //var updateRequest = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
            //updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            //updateRequest.Execute();
        }

        private void UpdateRange(int rowId, string colStart, string colEnd, List<object> data)
        {
            var range = $"{SheetName}!{colStart}{rowId}:{colEnd}{rowId}";
            var valueRange = new ValueRange { Values = new List<IList<object>> { data } };
            var request = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            request.Execute();
        }

        // 1. Hàm dùng chung để đọc 1 cột dữ liệu
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

        // 2. Hàm dùng chung để thêm dữ liệu mới vào cột A
        public void AddConfigData(string sheetName, string value)
        {
            var valueRange = new ValueRange();
            valueRange.Values = new List<IList<object>> { new List<object> { value } };

            var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, $"{sheetName}!A:A");
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.Execute();
        }

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
                        Email = row.Count > 3 ? row[3].ToString() : "",
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
            var valueRange = new ValueRange();

            var phoneNumber = "'" + customer.PhoneNumber;

            var objectList = new List<object>() {
                                customer.Id,
                                customer.FullName,
                                phoneNumber,
                                customer.Email,
                                customer.Address,
                                customer.Note
                            };
            valueRange.Values = new List<IList<object>> { objectList };

            // Ghi vào sheet KhachHang
            var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, "KhachHang!A:E");
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.Execute();
        }

        // 3. Hàm Sửa Khách Hàng (Update)
        public void UpdateCustomer(Customer customer)
        {
            // Tìm dòng trong sheet KhachHang
            int rowId = FindRowId("KhachHang", customer.Id);
            if (rowId == -1) return;
            var phoneNumber = "'" + customer.PhoneNumber;
            var range = $"KhachHang!A{rowId}:E{rowId}";
            var valueRange = new ValueRange();
            var objectList = new List<object>() {
                                customer.Id, // Giữ nguyên ID
                                customer.FullName,
                                phoneNumber,
                                customer.Email,
                                customer.Address,
                                customer.Note
                            };
            valueRange.Values = new List<IList<object>> { objectList };

            var updateRequest = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            updateRequest.Execute();
        }

        // 4. Hàm Xóa Khách Hàng (Delete)
        public void DeleteCustomer(string id)
        {
            int rowId = FindRowId("KhachHang", id);
            if (rowId == -1) return;

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
        }

        // 3. THÊM HÀM MỚI: UpdateStatus (Chỉ update các cột cần thiết để tối ưu)
        //    public void UpdateStatusAndFinance(string id, string status, DateTime? arrivalDate, decimal importPrice, DateTime? paymentDate)
        //    {
        //        int rowId = FindRowId("DonHang", id); // Nhớ đổi tên sheet cho đúng
        //        if (rowId == -1) return;

        //        // Ta cần update: 
        //        // Q: Ngày về (ArrivalDate)
        //        // R: Ngày TT (PaymentDate)
        //        // S: Giá nhập (ImportPrice)
        //        // V: Trạng thái (Status)

        //        // Update Ngày về (Q) và Ngày TT (R) và Giá nhập (S) - Range Q:S
        //        var rangeFinance = $"DonHang!Q{rowId}:S{rowId}";
        //        var valFinance = new ValueRange
        //        {
        //            Values = new List<IList<object>> { new List<object> {
        //    arrivalDate.HasValue ? arrivalDate.Value.ToString("dd/MM/yyyy") : "",
        //    paymentDate.HasValue ? paymentDate.Value.ToString("dd/MM/yyyy") : "",
        //    importPrice
        //}}
        //        };
        //        service.Spreadsheets.Values.Update(valFinance, SpreadsheetId, rangeFinance)
        //            .SetInputOption("USER_ENTERED").Execute();

        //        // Update Trạng thái (V) - Range V
        //        var rangeStatus = $"DonHang!V{rowId}";
        //        var valStatus = new ValueRange { Values = new List<IList<object>> { new List<object> { status } } };
        //        service.Spreadsheets.Values.Update(valStatus, SpreadsheetId, rangeStatus)
        //            .SetInputOption("USER_ENTERED").Execute();
        //    }

        public Customer GetCustomerByPhone(string phone)
        {
            var customers = GetCustomers();
            return customers.FirstOrDefault(c => c.PhoneNumber == phone);
        }

        public List<Product> GetProducts()
        {
            var range = "SanPham!A:E";
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
                        SellingPrice = row.Count > 4 ? ParseDecimal(row[4]) : 0
                    });
                }
            }
            return list;
        }

        public void AddProduct(Product p)
        {
            var valueRange = new ValueRange();
            var objectList = new List<object>() {
                p.Sku.ToUpper(),
                p.Name,
                p.Category,
                p.ImportPrice,
                p.SellingPrice
            };
            valueRange.Values = new List<IList<object>> { objectList };

            var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, "SanPham!A:E");
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.Execute();
        }

        // 3. Cập nhật Sản phẩm
        public void UpdateProduct(Product p)
        {
            int rowId = FindRowId("SanPham", p.Sku);
            if (rowId == -1) return;

            // Update từ cột B (Tên) đến E (Giá bán). Cột A (SKU) giữ nguyên để làm khóa.
            var range = $"SanPham!B{rowId}:E{rowId}";

            var valueRange = new ValueRange();
            var objectList = new List<object>() {
                p.Name,
                p.Category,
                p.ImportPrice,
                p.SellingPrice
            };
            valueRange.Values = new List<IList<object>> { objectList };

            var updateRequest = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            updateRequest.Execute();
        }

        // 4. Xóa Sản phẩm
        public void DeleteProduct(string sku)
        {
            int rowId = FindRowId("SanPham", sku);
            if (rowId == -1) return;

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
        }

        // Cập nhật hàm này trong GoogleSheetService.cs

        public void BulkUpdateOrder(string id, string newStatus, DateTime? arrivalDate, decimal? importPrice, decimal? paidAmount)
        {
            int rowId = FindRowId(SheetName, id);
            if (rowId == -1) return;

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
        }
    }
}