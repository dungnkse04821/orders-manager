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

        public List<CustomerSummary> CustomerStatsView { get; set; } = new List<CustomerSummary>();

        [BindProperty(SupportsGet = true)]
        public string PaymentFilter { get; set; }

        public void OnGet()
        {
            var allOrders = _service.GetAll();

            // 1. Gom nhóm và tính toán sơ bộ
            var query = allOrders
                .Where(o => !string.IsNullOrEmpty(o.CustomerName))
                .GroupBy(o => o.CustomerName)
                .Select(g => new CustomerSummary
                {
                    CustomerName = g.Key,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount),
                    TotalDebt = g.Sum(o => o.RemainingAmount > 0 ? o.RemainingAmount : 0),
                    LastOrderDate = g.Max(o => o.OrderDate)
                });

            // 2. Áp dụng bộ lọc Tình trạng thanh toán
            if (!string.IsNullOrEmpty(PaymentFilter))
            {
                if (PaymentFilter == "Debt")
                {
                    query = query.Where(c => c.TotalDebt > 0);
                }
                else if (PaymentFilter == "Paid")
                {
                    query = query.Where(c => c.TotalDebt <= 0);
                }
            }

            // 3. Sắp xếp: Ai nợ nhiều nhất lên đầu, sau đó đến ai mua nhiều nhất
            CustomerStatsView = query
                .OrderByDescending(x => x.TotalDebt)
                .ThenByDescending(x => x.TotalSpent)
                .ToList();
        }
    }
}
