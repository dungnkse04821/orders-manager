using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OrdersManager.Models;

namespace OrdersManager.Controllers
{
    public class OrderController : Controller
    {
        private readonly GoogleSheetService _service;

        public OrderController()
        {
            _service = new GoogleSheetService();
        }

        private void PopulateDropdowns()
        {
            ViewBag.Sources = new List<SelectListItem> {
                new SelectListItem("Trung", "Trung"),
                new SelectListItem("Hàn", "Hàn"),
                new SelectListItem("Nhật", "Nhật"),
                new SelectListItem("Thái", "Thái"),
                new SelectListItem("Mỹ", "Mỹ"),
                new SelectListItem("TBN", "TBN")
            };

            ViewBag.Categories = new List<SelectListItem> {
                new SelectListItem("Quần nữ", "Quần nữ"),
                new SelectListItem("Áo nữ", "Áo nữ"),
                new SelectListItem("Váy nữ", "Váy nữ"),
                new SelectListItem("Quần nam", "Quần nam"),
                new SelectListItem("Túi ví", "Túi ví"),
                new SelectListItem("Phụ kiện", "Phụ kiện"),
                new SelectListItem("Mỹ phẩm", "Mỹ phẩm")
            };

            // Bạn có thể thêm list cho Kho, Size, v.v.
        }

        public IActionResult Index()
        {
            var data = _service.GetAll();
            // Có thể tính tổng doanh thu/lợi nhuận để hiển thị trên Dashboard ở đây
            return View(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        public IActionResult Create(Order order)
        {
            // Tự sinh ID
            order.Id = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            // Nếu không chọn ngày đặt thì mặc định hôm nay
            if (!order.OrderDate.HasValue) order.OrderDate = DateTime.Now;

            _service.Add(order);
            return RedirectToAction("Index");
        }

        // ... Edit và Delete
    }
}
