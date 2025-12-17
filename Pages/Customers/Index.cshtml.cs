using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrdersManager.Models;

namespace OrdersManager.Pages.Customers
{
    public class IndexModel : PageModel
    {
        private readonly GoogleSheetService _service;

        public IndexModel(GoogleSheetService service)
        {
            _service = service;
        }

        public List<CustomerSummary> CustomerStats { get; set; } = new List<CustomerSummary>();

        public void OnGet()
        {
            // 1. Lấy tất cả đơn hàng
            var allOrders = _service.GetAll();

            // 2. Gom nhóm theo Tên khách hàng (LINQ)
            CustomerStats = allOrders
                .Where(o => !string.IsNullOrEmpty(o.CustomerName)) // Bỏ đơn không có tên khách
                .GroupBy(o => o.CustomerName)
                .Select(g => new CustomerSummary
                {
                    CustomerName = g.Key,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount), // Cộng tổng cột TotalAmount
                    LastOrderDate = g.Max(o => o.OrderDate) // Lấy ngày mua mới nhất
                })
                .OrderByDescending(x => x.TotalSpent) // Sắp xếp khách VIP (mua nhiều) lên đầu
                .ToList();
        }
    }
}
