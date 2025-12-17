using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public List<SelectListItem> SourceOptions { get; set; }
        public List<SelectListItem> CategoryOptions { get; set; }
        public List<SelectListItem> WarehouseOptions { get; set; }

        public void OnGet()
        {
            // Khởi tạo giá trị mặc định
            Order.OrderDate = DateTime.Now;
            PopulateDropdowns();
        }

        public IActionResult OnPost()
        {
            ModelState.Remove("Order.Id");
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(); // Load lại dropdown nếu lỗi validate
                return Page();
            }

            // Sinh ID và lưu
            Order.Id = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            _service.Add(Order);

            return RedirectToPage("./Index");
        }

        private void PopulateDropdowns()
        {
            SourceOptions = new List<SelectListItem> {
                new SelectListItem("Trung", "Trung"),
                new SelectListItem("Hàn", "Hàn"),
                new SelectListItem("Nhật", "Nhật"),
                new SelectListItem("Thái", "Thái"),
                new SelectListItem("Mỹ", "Mỹ"),
                new SelectListItem("Tây Ban Nha", "Tây Ban Nha"),
            };

            WarehouseOptions = new List<SelectListItem> {
                new SelectListItem("Shopee", "Shopee")
            };

            CategoryOptions = new List<SelectListItem>
            {
                new SelectListItem("Quần nữ", "Quần nữ"),
                new SelectListItem("Áo nữ", "Áo nữ"),
                new SelectListItem("Váy nữ", "Váy nữ"),
                new SelectListItem("Quần nam", "Quần nam"),
                new SelectListItem("Áo nam", "Áo nam"),
                new SelectListItem("Giày dép nữ", "Giày dép nữ"),
                new SelectListItem("Giày dép nam", "Giày dép nam"),
                new SelectListItem("Túi ví nữ", "Túi ví nữ"),
                new SelectListItem("Túi ví nam", "Túi ví nam"),
                new SelectListItem("Phụ kiện nữ", "Phụ kiện nữ"),
                new SelectListItem("Phụ kiện nam", "Phụ kiện nam"),
                new SelectListItem("Quần áo trẻ em", "Quần áo trẻ em"),
                new SelectListItem("Giày dép trẻ em", "Giày dép trẻ em"),
                new SelectListItem("Phụ kiện trẻ em", "Phụ kiện trẻ em"),
                new SelectListItem("Thuốc", "Thuốc"),
                new SelectListItem("Mỹ phẩm", "Mỹ phẩm"),
                new SelectListItem("Đồ gia đình", "Đồ gia đình")
            };
        }
    }
}
