using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrdersManager.Models;

namespace OrdersManager.Pages.Customers
{
    public class CreateModel : PageModel
    {
        private readonly GoogleSheetService _service;
        public CreateModel(GoogleSheetService service) { _service = service; }

        [BindProperty]
        public Customer Customer { get; set; }

        public IActionResult OnPost()
        {
            ModelState.Remove("Customer.Id");
            if (!ModelState.IsValid) return Page();

            // Tự sinh ID ngắn gọn (8 ký tự)
            Customer.Id = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            _service.AddCustomer(Customer);
            return RedirectToPage("./Customer");
        }
    }
}
