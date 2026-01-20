using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrdersManager.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OrdersManager.Pages.Orders
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly GoogleSheetService _service;
        public List<Order> Orders { get; set; } = new List<Order>();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } // Tìm chung (Tên, SĐT, SP, Code)

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } // Lọc trạng thái

        [BindProperty(SupportsGet = true)]
        public string FromDate { get; set; } // Từ ngày

        [BindProperty(SupportsGet = true)]
        public string ToDate { get; set; }   // Đến ngày

        public IndexModel(ILogger<IndexModel> logger, GoogleSheetService service)
        {
            _logger = logger;
            _service = service;
        }

        public void OnGet()
        {
            var data = _service.GetAll();

            var query = data.AsQueryable();

            // a. Lọc theo từ khóa (Tìm trong: Mã đơn, Tên khách, SĐT, Tên SP)
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                string term = SearchTerm.ToLower().Trim();
                query = query.Where(o =>
                    (o.Code != null && o.Code.ToLower().Contains(term)) ||
                    (o.CustomerName != null && o.CustomerName.ToLower().Contains(term)) ||
                    (o.PhoneNumber != null && o.PhoneNumber.Contains(term)) ||
                    (o.ProductName != null && o.ProductName.ToLower().Contains(term))
                );
            }

            // b. Lọc theo trạng thái
            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "All")
            {
                query = query.Where(o => o.Status == StatusFilter);
            }

            // c. Lọc theo ngày (Từ ngày... Đến ngày...)
            if (!string.IsNullOrEmpty(FromDate) && DateTime.TryParse(FromDate, out DateTime from))
            {
                query = query.Where(o => o.OrderDate >= from);
            }
            if (!string.IsNullOrEmpty(ToDate) && DateTime.TryParse(ToDate, out DateTime to))
            {
                query = query.Where(o => o.OrderDate <= to);
            }
            Orders = query.OrderByDescending(o => o.OrderDate).ToList();
        }

        public IActionResult OnPostDelete(string id)
        {
            if (id != null)
            {
                _service.Delete(id);
            }
            return RedirectToPage();
        }
    }
}
