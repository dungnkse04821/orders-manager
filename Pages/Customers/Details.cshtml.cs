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
        public decimal TotalSpent { get; set; }

        public void OnGet(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                RedirectToPage("./Index");
                return;
            }

            CustomerName = name;

            // Lấy tất cả và lọc theo tên khách hàng
            var allOrders = _service.GetAll();

            CustomerOrders = allOrders
                .Where(o => o.CustomerName != null &&
                            o.CustomerName.Equals(name, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            TotalSpent = CustomerOrders.Sum(o => o.TotalAmount);
        }
    }
}
