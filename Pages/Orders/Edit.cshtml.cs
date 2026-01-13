using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrdersManager.Models;

namespace OrdersManager.Pages.Orders
{
    public class EditModel : PageModel
    {
        private readonly GoogleSheetService _service;
        public EditModel(GoogleSheetService service) { _service = service; }

        [BindProperty]
        public Order Order { get; set; }

        // Các list cho dropdown
        public List<string> Sources { get; set; }
        public List<string> Warehouses { get; set; }
        public List<string> Categories { get; set; }
        public List<Product> ProductList { get; set; }
        public IActionResult OnGet(string id)
        {
            if (id == null) return RedirectToPage("./Index");

            // Tìm đơn hàng theo ID
            Order = _service.GetAll().FirstOrDefault(o => o.Id == id);

            if (Order == null) return RedirectToPage("./Index");

            // Load dropdown giống trang Create
            Sources = _service.GetConfigData("Config_NguonHang");
            Warehouses = _service.GetConfigData("Config_Kho");
            Categories = _service.GetConfigData("Config_LoaiHang");
            ProductList = _service.GetProducts();
            //Order.Status = "Chờ đặt";

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                // Load lại dropdown nếu lỗi
                Sources = _service.GetConfigData("Config_NguonHang");
                Warehouses = _service.GetConfigData("Config_Kho");
                Categories = _service.GetConfigData("Config_LoaiHang");
                return Page();
            }

            _service.Update(Order);
            return RedirectToPage("./Index");
        }
    }
}
