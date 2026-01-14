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

            LoadDropdowns();
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return Page();
            }

            // =========================================================
            // 1. LOGIC TỰ ĐỘNG THÊM KHÁCH HÀNG (NẾU SĐT CHƯA CÓ)
            // =========================================================
            if (!string.IsNullOrEmpty(Order.PhoneNumber))
            {
                // Kiểm tra xem SĐT này đã có trong sheet KhachHang chưa
                var existingCust = _service.GetCustomerByPhone(Order.PhoneNumber);

                if (existingCust == null)
                {
                    // Chưa có -> Tạo mới
                    var newCustomer = new Customer
                    {
                        Id = Guid.NewGuid().ToString().Substring(0, 8), // Tạo ID ngẫu nhiên
                        PhoneNumber = Order.PhoneNumber,
                        FullName = Order.CustomerName,
                        Address = Order.ShippingAddress,
                        Note = "Tự động thêm khi Sửa đơn hàng"
                    };

                    _service.AddCustomer(newCustomer);
                }
            }
            // =========================================================

            // --- LOGIC TỰ ĐỘNG THÊM SẢN PHẨM ---
            if (!string.IsNullOrEmpty(Order.Code))
            {
                string sku = Order.Code.Trim().ToUpper();
                Order.Code = sku;

                // Check nhanh xem có chưa (Code này tối ưu hơn GetAll nếu list lớn)
                // Nhưng vì GoogleSheetService ta chưa viết hàm CheckExists nên dùng GetAll().Any() tạm
                var exists = _service.GetProducts().Any(p => p.Sku == sku);

                if (!exists)
                {
                    var newProduct = new Product
                    {
                        Sku = sku,
                        Name = Order.ProductName,
                        Category = Order.Category ?? "Khác",
                        SellingPrice = Order.SellingPrice,
                        ImportPrice = Order.ImportPrice
                    };
                    _service.AddProduct(newProduct);
                }
            }
            // ------------------------------------

            _service.Update(Order);
            return RedirectToPage("./Index");
        }

        private void LoadDropdowns()
        {
            Sources = _service.GetConfigData("Config_NguonHang");
            Warehouses = _service.GetConfigData("Config_Kho");
            Categories = _service.GetConfigData("Config_LoaiHang");
            ProductList = _service.GetProducts();
        }

        // 1. API Trả về thông tin sản phẩm (Cho Ajax gọi)
        public IActionResult OnGetProductInfo(string sku)
        {
            var products = _service.GetProducts();
            var product = products.FirstOrDefault(p => p.Sku == sku);

            if (product != null)
            {
                return new JsonResult(new
                {
                    success = true,
                    name = product.Name,
                    category = product.Category,
                    sellPrice = product.SellingPrice,
                    importPrice = product.ImportPrice
                });
            }
            return new JsonResult(new { success = false });
        }

        // 2. API Kiểm tra số điện thoại (Cho Ajax gọi)
        public IActionResult OnGetCheckPhone(string phone)
        {
            var cust = _service.GetCustomerByPhone(phone);
            if (cust != null)
            {
                return new JsonResult(new
                {
                    found = true,
                    name = cust.FullName,
                    address = cust.Address
                });
            }
            return new JsonResult(new { found = false });
        }

        // 3. API Thêm thuộc tính nhanh (Kho, Nguồn, Loại...)
        public IActionResult OnPostAddAttribute(string type, string value)
        {
            // Gọi Service thêm vào Sheet Config tương ứng
            // Lưu ý: Bạn cần đảm bảo Service có hàm AddConfigData hoặc tự xử lý lưu vào Sheet Config
            // Ví dụ đơn giản:
            string sheetName = "";
            if (type == "Source") sheetName = "Config_NguonHang";
            else if (type == "Warehouse") sheetName = "Config_Kho";
            else if (type == "Category") sheetName = "Config_LoaiHang";

            if (!string.IsNullOrEmpty(sheetName))
            {
                _service.AddConfigData(sheetName, value); // Giả sử bạn đã viết hàm này trong Service
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "Loại dữ liệu không hợp lệ" });
        }
    }
}
