using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrdersManager.Models;

namespace OrdersManager.Pages.Orders
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
        public List<Product> ProductList { get; set; }
        public List<Customer> CustomerList { get; set; }
        public void OnGet()
        {
            // Khởi tạo giá trị mặc định
            Order.OrderDate = DateTime.Now;
            Order.Status = "Chờ đặt";
            LoadDropdowns();
            //ProductList = _service.GetProducts();
        }

        public IActionResult OnPost()
        {
            ModelState.Remove("Order.Id");
            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return Page();
            }

            // Kiểm tra xem khách này đã có trong DB chưa?
            var existingCust = _service.GetCustomerByPhone(Order.PhoneNumber);

            if (existingCust == null && !string.IsNullOrEmpty(Order.PhoneNumber))
            {
                // Chưa có -> Tạo mới khách hàng
                var newCustomer = new Customer
                {
                    Id = Guid.NewGuid().ToString().Substring(0, 8),
                    PhoneNumber = Order.PhoneNumber,
                    FullName = Order.CustomerName,
                    Address = Order.ShippingAddress,
                    Note = "Tự động thêm từ đơn hàng"
                };

                // Lưu vào Sheet KhachHang
                _service.AddCustomer(newCustomer);
            }

            // --- 2. LOGIC TỰ ĐỘNG THÊM SẢN PHẨM (MỚI THÊM) ---
            // Chỉ thực hiện nếu có nhập Mã Code
            if (!string.IsNullOrEmpty(Order.Code))
            {
                // Chuẩn hóa SKU về chữ in hoa để so sánh chính xác
                string sku = Order.Code.Trim().ToUpper();
                Order.Code = sku; // Lưu lại mã in hoa vào đơn hàng luôn cho đẹp

                // Lấy danh sách sản phẩm hiện có để kiểm tra
                var allProducts = _service.GetProducts();

                // Kiểm tra xem SKU này đã có chưa?
                bool productExists = allProducts.Any(p => p.Sku == sku);

                if (!productExists)
                {
                    // Chưa có -> Tạo mới Product từ thông tin Đơn hàng
                    var newProduct = new Product
                    {
                        Sku = sku,
                        Name = Order.ProductName,
                        Category = Order.Category ?? "Chưa phân loại", // Nếu quên chọn loại thì để mặc định
                        SellingPrice = Order.SellingPrice,
                        ImportPrice = Order.ImportPrice,
                        Source = Order.Source,
                        Warehouse = Order.Warehouse
                    };

                    // Lưu vào Sheet SanPham
                    _service.AddProduct(newProduct);
                }
            }
            // -----------------------------------------------------

            // Sinh ID và lưu
            Order.Id = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            _service.Add(Order);

            return RedirectToPage("./Index");
        }

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
                    importPrice = product.ImportPrice,
                    source = product.Source,
                    warehouse = product.Warehouse
                });
            }
            return new JsonResult(new { success = false });
        }

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
            ProductList = _service.GetProducts();
            CustomerList = _service.GetCustomers();
        }

    }
}
