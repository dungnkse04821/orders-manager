using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrdersManager.Models;

namespace OrdersManager.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly GoogleSheetService _service;
        public List<Order> Orders { get; set; } = new List<Order>();

        public IndexModel(ILogger<IndexModel> logger, GoogleSheetService service)
        {
            _logger = logger;
            _service = service;
        }

        public void OnGet()
        {
            Orders = _service.GetAll();
            Orders = Orders.OrderByDescending(o => o.OrderDate).ToList();
        }
    }
}
