using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrdersManager.Models;

namespace OrdersManager.Pages.Customers
{
    public class EditModel : PageModel
    {
        private readonly GoogleSheetService _service;
        public EditModel(GoogleSheetService service) { _service = service; }

        [BindProperty]
        public Customer Customer { get; set; }

        public IActionResult OnGet(string id)
        {
            if (id == null) return RedirectToPage("./Index");

            // Tìm khách hàng trong danh sách
            var list = _service.GetCustomers();
            Customer = list.FirstOrDefault(c => c.Id == id);

            if (Customer == null) return RedirectToPage("./Index");

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            _service.UpdateCustomer(Customer);
            return RedirectToPage("./Index");
        }
    }
}
