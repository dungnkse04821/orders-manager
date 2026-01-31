using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrdersManager.Models;

namespace OrdersManager.Pages.Customers
{
    public class DetailsModel : PageModel
    {
        private readonly GoogleSheetService _service;

        public DetailsModel(GoogleSheetService service)
        {
            _service = service;
        }

        public string CustomerName { get; set; }
        public List<Order> CustomerOrders { get; set; } = new List<Order>();

        // --- CÁC BIẾN SUMMARY (DASHBOARD) ---
        public decimal TotalSpent { get; set; }  // Tổng giá trị đơn (Sau khi lọc)
        public decimal TotalDebt { get; set; }   // Tổng nợ (Sau khi lọc)
        public decimal TotalPaid { get; set; }   // Tổng đã trả (Sau khi lọc)

        // --- CÁC BIẾN BỘ LỌC ---
        [BindProperty(SupportsGet = true)] public string StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string PaymentFilter { get; set; } // "All", "Paid", "Unpaid"

        public void OnGet(string name)
        {
            if (string.IsNullOrEmpty(name)) { RedirectToPage("./Index"); return; }
            CustomerName = name;

            // 1. Lấy tất cả đơn của khách này
            var allOrders = _service.GetAll()
                .Where(o => o.CustomerName != null &&
                       o.CustomerName.Equals(name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // 2. Áp dụng bộ lọc (Trong bộ nhớ)
            var query = allOrders.AsEnumerable();

            // Lọc trạng thái đơn
            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "ALL")
            {
                query = query.Where(o => o.Status == StatusFilter);
            }

            // Lọc trạng thái thanh toán
            if (!string.IsNullOrEmpty(PaymentFilter))
            {
                if (PaymentFilter == "Unpaid") // Chưa thanh toán xong
                    query = query.Where(o => o.RemainingAmount > 0);
                else if (PaymentFilter == "Paid") // Đã thanh toán xong
                    query = query.Where(o => o.RemainingAmount <= 0);
            }

            // 3. Kết quả danh sách
            CustomerOrders = query.OrderByDescending(o => o.OrderDate).ToList();

            // 4. Tính toán Dashboard (Dựa trên danh sách ĐÃ LỌC)
            TotalSpent = CustomerOrders.Sum(o => o.TotalAmount);
            TotalDebt = CustomerOrders.Sum(o => o.RemainingAmount > 0 ? o.RemainingAmount : 0);
            TotalPaid = TotalSpent - TotalDebt;
        }
    }
}