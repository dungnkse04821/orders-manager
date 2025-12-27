using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrdersManager.Models;

namespace OrdersManager.Pages.Customers
{
    public class CustomerModel : PageModel
    {
        private readonly GoogleSheetService _service;

        public CustomerModel(GoogleSheetService service)
        {
            _service = service;
        }

        public List<Customer> Customers { get; set; } = new List<Customer>();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public void OnGet()
        {
            var data = _service.GetCustomers();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                // Lọc theo Tên hoặc Số điện thoại hoặc email
                data = data.Where(c =>
                    c.FullName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.PhoneNumber.Contains(SearchTerm) ||
                    !string.IsNullOrEmpty(c.Email) && c.Email.Contains(SearchTerm)
                ).ToList();
            }

            Customers = data;
        }

        public IActionResult OnPostDelete(string id)
        {
            if (id != null)
            {
                _service.DeleteCustomer(id);
            }
            return RedirectToPage();
        }
    }
}
