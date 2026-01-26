using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OrdersManager.Pages.Orders
{
    public class IncomingModel : PageModel
    {
        private readonly GoogleSheetService _service;
        private readonly IConfiguration _configuration;
        public IncomingModel(GoogleSheetService service, IConfiguration configuration) { _service = service; _configuration = configuration; }
        public string BankId { get; set; }
        public string AccountNo { get; set; }
        public string AccountName { get; set; }
        public class OrderItem
        {
            public string Id { get; set; }
            public bool IsSelected { get; set; }
            public string Code { get; set; }
            public string Status { get; set; }
            public string ProductName { get; set; }
            public string Source { get; set; }
            public string Warehouse { get; set; }
            public decimal SellingPrice { get; set; }
            public string CustomerName { get; set; }
            public string PhoneNumber { get; set; }
            public int Quantity { get; set; }
            public DateTime? OrderDate { get; set; }
            public decimal TotalAmount { get; set; } // Tổng giá trị đơn
            public decimal Deposit { get; set; }     // Đã cọc
            public decimal RemainingAmount => TotalAmount - Deposit; // Số tiền cần thanh toán
            public decimal ImportPrice { get; set; }
            public DateTime ArrivalDate { get; set; } = DateTime.Now;
            public decimal PaidAmount { get; set; } // Số tiền thanh toán
        }

        // --- DỮ LIỆU HIỂN THỊ ---
        [BindProperty]
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        // List Dropdown cho bộ lọc
        public List<string> Sources { get; set; }
        public List<string> Warehouses { get; set; }

        // --- CÁC THAM SỐ BỘ LỌC (GET) ---
        [BindProperty(SupportsGet = true)] public string StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public string SourceFilter { get; set; }    // MỚI
        [BindProperty(SupportsGet = true)] public string WarehouseFilter { get; set; } // MỚI
        [BindProperty(SupportsGet = true)] public string FromDate { get; set; }        // MỚI
        [BindProperty(SupportsGet = true)] public string ToDate { get; set; }          // MỚI

        // --- CÁC CHỈ SỐ TỔNG HỢP (DASHBOARD) ---
        public int TotalCount { get; set; }
        public decimal TotalImportCapital { get; set; } // Tổng vốn (Giá nhập)
        public decimal TotalRevenue { get; set; }       // Tổng doanh thu (Giá bán)

        // --- BIẾN FORM POST ---
        [BindProperty] public string TargetStatus { get; set; }

        public void OnGet()
        {
            BankId = _configuration["BankConfig:BankId"];
            AccountNo = _configuration["BankConfig:AccountNo"];
            AccountName = _configuration["BankConfig:AccountName"];
            Sources = _service.GetConfigData("Config_NguonHang");
            Warehouses = _service.GetConfigData("Config_Kho");

            if (string.IsNullOrEmpty(StatusFilter)) StatusFilter = "ALL";

            var query = _service.GetAll().AsQueryable();

            // 1. Filter theo Trạng thái
            if (StatusFilter != "ALL")
            {
                query = query.Where(o => o.Status == StatusFilter);
            }

            // 2. Filter theo Tên hoặc SĐT (MỚI)
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                string term = SearchTerm.ToLower();
                query = query.Where(o =>
                    (o.CustomerName != null && o.CustomerName.ToLower().Contains(term)) ||
                    (o.PhoneNumber != null && o.PhoneNumber.Contains(term)) ||
                    (o.Code != null && o.Code.ToLower().Contains(term))
                );
            }

            // Filter Nâng cao (MỚI)
            if (!string.IsNullOrEmpty(SourceFilter)) query = query.Where(o => o.Source == SourceFilter);
            if (!string.IsNullOrEmpty(WarehouseFilter)) query = query.Where(o => o.Warehouse == WarehouseFilter);
            if (!string.IsNullOrEmpty(FromDate) && DateTime.TryParse(FromDate, out DateTime from)) query = query.Where(o => o.OrderDate >= from);
            if (!string.IsNullOrEmpty(ToDate) && DateTime.TryParse(ToDate, out DateTime to)) query = query.Where(o => o.OrderDate <= to);

            // 3. TÍNH TOÁN DASHBOARD
            TotalCount = query.Count();
            TotalRevenue = query.Where(x => x.Status != "Hủy").Sum(o => o.TotalAmount);
            // Tổng vốn = Giá nhập * Số lượng (Nếu chưa nhập giá vốn thì tính là 0)
            TotalImportCapital = query.Sum(o => (o.ImportPrice > 0 ? o.ImportPrice : 0) * o.Quantity);

            Items = query.Select(o => new OrderItem
            {
                Id = o.Id,
                Code = o.Code,
                ProductName = $"{o.ProductName}",
                CustomerName = o.CustomerName,
                PhoneNumber = o.PhoneNumber,
                Source = o.Source,
                Warehouse = o.Warehouse,
                SellingPrice = o.SellingPrice,
                Quantity = o.Quantity,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount, 
                Deposit = o.Deposit,
                ImportPrice = o.ImportPrice > 0 ? o.ImportPrice : 0,
                PaidAmount = 0,
                IsSelected = false
            }).OrderByDescending(o => o.OrderDate).ToList();
        }

        public IActionResult OnPost()
        {
            var selectedItems = Items.Where(x => x.IsSelected).ToList();

            if (selectedItems.Count == 0 || string.IsNullOrEmpty(TargetStatus))
            {
                return RedirectToPage(new { StatusFilter, SearchTerm });
            }

            foreach (var item in selectedItems)
            {
                DateTime? dateToUpdate = null;
                decimal? priceToUpdate = null;
                decimal? paidToUpdate = null;

                // Logic phân loại hành động
                if (TargetStatus == "Đã về")
                {
                    dateToUpdate = item.ArrivalDate;
                    priceToUpdate = item.ImportPrice;
                }
                else if (TargetStatus == "Đã giao")
                {
                    paidToUpdate = item.PaidAmount;
                }

                _service.BulkUpdateOrder(item.Id, TargetStatus, dateToUpdate, priceToUpdate, paidToUpdate);
            }

            return RedirectToPage(new { StatusFilter, SearchTerm });
        }
    }
}
