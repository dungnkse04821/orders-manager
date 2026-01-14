using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrdersManager.Models;

namespace OrdersManager.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly GoogleSheetService _service;
        public IndexModel(GoogleSheetService service) { _service = service; }

        public List<Product> Products { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public void OnGet()
        {
            var list = _service.GetProducts();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                list = list.Where(p =>
                    p.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Sku.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            Products = list;
        }

        public IActionResult OnPostDelete(string sku)
        {
            _service.DeleteProduct(sku);
            return RedirectToPage();
        }
    }
}
