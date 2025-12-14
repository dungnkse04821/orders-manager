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

        public void OnGet()
        {
            // Khởi tạo giá trị mặc định
            Order.OrderDate = DateTime.Now;
            PopulateDropdowns();
        }

        public IActionResult OnPost()
        {
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
                new SelectListItem("Thái", "Thái")
            };

            CategoryOptions = new List<SelectListItem> {
                new SelectListItem("Quần nữ", "Quần nữ"),
                new SelectListItem("Áo nữ", "Áo nữ"),
                new SelectListItem("Váy nữ", "Váy nữ"),
                new SelectListItem("Túi ví", "Túi ví")
            };
        }
    }
}
