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
            var range = $"{SheetName}!A:U";
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

                    list.Add(order);
                }
            }
            return list;
        }

        public void Add(Order order)
        {
            var valueRange = new ValueRange();

            // Xử lý ngày tháng về string
            string orderDate = order.OrderDate.HasValue ? order.OrderDate.Value.ToString("dd/MM/yyyy") : "";
            string arrDate = order.ArrivalDate.HasValue ? order.ArrivalDate.Value.ToString("dd/MM/yyyy") : "";
            string payDate = order.PaymentDate.HasValue ? order.PaymentDate.Value.ToString("dd/MM/yyyy") : "";

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
                "=R:R*K:K",         // T (Formula: Giá Nhập * SL)
                "=L:L-T:T"          // U (Formula: Tổng tiền bán - Thành tiền nhập
            };

            valueRange.Values = new List<IList<object>> { objectList };
            var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, $"{SheetName}!A:U");
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.Execute();
        }

        // 4. DELETE (Xóa)
        public void Delete(string id)
        {
            int rowId = FindRowId(id);
            if (rowId == -1) return;

            // Trong Sheets API, xóa dòng thật sự khá phức tạp (dùng BatchUpdate), 
            // cách đơn giản nhất là xóa trắng dữ liệu dòng đó.
            var range = $"{SheetName}!A{rowId}:C{rowId}";
            var clearRequest = service.Spreadsheets.Values.Clear(new ClearValuesRequest(), SpreadsheetId, range);
            clearRequest.Execute();
        }

        // 1. Hàm tìm dòng dựa trên ID (Helper)
        private int FindRowId(string id)
        {
            // Đọc cột A (chứa ID) để tìm dòng
            var range = $"{SheetName}!A:A";
            var request = service.Spreadsheets.Values.Get(SpreadsheetId, range);
            var response = request.Execute();
            var values = response.Values;

            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    // i là index (0, 1, 2...), row thực tế trong sheet là i + 1
                    if (values[i].Count > 0 && values[i][0].ToString() == id)
                    {
                        return i + 1;
                    }
                }
            }
            return -1; // Không tìm thấy
        }

        // 2. Hàm Cập nhật (Update)
        public void Update(Order order)
        {
            int rowId = FindRowId(order.Id);
            if (rowId == -1) return;

            var range = $"{SheetName}!A{rowId}:S{rowId}";

            // Xử lý ngày tháng về string
            string orderDate = order.OrderDate.HasValue ? order.OrderDate.Value.ToString("dd/MM/yyyy") : "";
            string arrDate = order.ArrivalDate.HasValue ? order.ArrivalDate.Value.ToString("dd/MM/yyyy") : "";
            string payDate = order.PaymentDate.HasValue ? order.PaymentDate.Value.ToString("dd/MM/yyyy") : "";

            var valueRange = new ValueRange();
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
                order.ImportPrice  // S
            };

            valueRange.Values = new List<IList<object>> { objectList };
            var updateRequest = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            updateRequest.Execute();
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
    }
}