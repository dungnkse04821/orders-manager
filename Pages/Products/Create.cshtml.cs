using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrdersManager.Models;

namespace OrdersManager.Pages.Products
{
    public class CreateModel : PageModel
    {
        private readonly GoogleSheetService _service;
        public CreateModel(GoogleSheetService service) { _service = service; }

        [BindProperty]
        public Product Product { get; set; }

        public List<string> Categories { get; set; }
        public List<string> Sources { get; set; }
        public List<string> Warehouses { get; set; }

        public void OnGet()
        {
            Categories = _service.GetConfigData("Config_LoaiHang");
            Sources = _service.GetConfigData("Config_NguonHang");
            Warehouses = _service.GetConfigData("Config_Kho");
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            // Check trùng SKU
            var exists = _service.GetProducts().Any(p => p.Sku == Product.Sku.ToUpper());
            if (exists)
            {
                ModelState.AddModelError("Product.Sku", "Mã SKU này đã tồn tại!");
                Categories = _service.GetConfigData("Config_LoaiHang");
                return Page();
            }
            Product.SellingPrice *= 1000;
            Product.ImportPrice *= 1000;
            _service.AddProduct(Product);
            return RedirectToPage("./Index");
        }
    }
}
