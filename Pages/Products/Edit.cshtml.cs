using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrdersManager.Models;

namespace OrdersManager.Pages.Products
{
    public class EditModel : PageModel
    {
        private readonly GoogleSheetService _service;
        public EditModel(GoogleSheetService service) { _service = service; }

        [BindProperty]
        public Product Product { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Sources { get; set; }
        public List<string> Warehouses { get; set; }

        public IActionResult OnGet(string sku)
        {
            if (sku == null) return RedirectToPage("./Index");

            Product = _service.GetProducts().FirstOrDefault(p => p.Sku == sku);
            if (Product == null) return RedirectToPage("./Index");

            Categories = _service.GetConfigData("Config_LoaiHang");
            Sources = _service.GetConfigData("Config_NguonHang");
            Warehouses = _service.GetConfigData("Config_Kho");
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            _service.UpdateProduct(Product);
            return RedirectToPage("./Index");
        }
    }
}
