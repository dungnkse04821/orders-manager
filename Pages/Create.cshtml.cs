using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrdersManager.Models;

namespace OrdersManager.Pages
{
    public class CreateModel : PageModel
    {
        private readonly GoogleSheetService _service;

        public CreateModel(GoogleSheetService service)
        {
            _service = service;
        }

        [BindProperty]
        public Order Order { get; set; } = new Order();

        // Danh sách dữ liệu cho Dropdown
        public List<string> Sources { get; set; }
        public List<string> Warehouses { get; set; }
        public List<string> Categories { get; set; }

        public void OnGet()
        {
            // Khởi tạo giá trị mặc định
            Order.OrderDate = DateTime.Now;
            LoadDropdowns();
        }

        public IActionResult OnPost()
        {
            ModelState.Remove("Order.Id");
            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return Page();
            }

            // Sinh ID và lưu
            Order.Id = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            _service.Add(Order);

            return RedirectToPage("./Index");
        }

        // --- API XỬ LÝ AJAX THÊM MỚI ---
        // type: "Source", "Warehouse", hoặc "Category"
        public IActionResult OnPostAddAttribute(string type, string value)
        {
            string sheetName = "";
            switch (type)
            {
                case "Source": sheetName = "Config_NguonHang"; break;
                case "Warehouse": sheetName = "Config_Kho"; break;
                case "Category": sheetName = "Config_LoaiHang"; break;
            }

            if (!string.IsNullOrEmpty(sheetName) && !string.IsNullOrEmpty(value))
            {
                _service.AddConfigData(sheetName, value);
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false });
        }

        private void LoadDropdowns()
        {
            Sources = _service.GetConfigData("Config_NguonHang");
            Warehouses = _service.GetConfigData("Config_Kho");
            Categories = _service.GetConfigData("Config_LoaiHang");
        }

    }
}
