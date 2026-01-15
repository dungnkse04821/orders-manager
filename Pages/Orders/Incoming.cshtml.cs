using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OrdersManager.Pages.Orders
{
    public class IncomingModel : PageModel
    {
        private readonly GoogleSheetService _service;
        public IncomingModel(GoogleSheetService service) { _service = service; }
        public string BankId { get; set; } = "VPBank";
        public string AccountNo { get; set; } = "137571668";
        public string AccountName { get; set; } = "LE THUY DUNG";
        public class OrderItem
        {
            public string Id { get; set; }
            public bool IsSelected { get; set; }
            public string Code { get; set; }
            public string Status { get; set; }
            public string ProductName { get; set; }
            public string CustomerName { get; set; }
            public string PhoneNumber { get; set; }
            public int Quantity { get; set; }

            // --- CÁC TRƯỜNG MỚI ---
            public DateTime? OrderDate { get; set; }
            public decimal TotalAmount { get; set; } // Tổng giá trị đơn
            public decimal Deposit { get; set; }     // Đã cọc
            public decimal RemainingAmount => TotalAmount - Deposit; // Số tiền cần thanh toán

            // Input cho Hàng về
            public decimal ImportPrice { get; set; }
            public DateTime ArrivalDate { get; set; } = DateTime.Now;

            // Input cho Đã giao
            public decimal PaidAmount { get; set; } // Số tiền thanh toán
        }

        [BindProperty]
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)] // Cho phép nhận từ URL
        public string SearchTerm { get; set; } // Biến tìm kiếm

        [BindProperty]
        public string TargetStatus { get; set; }

        public void OnGet()
        {
            if (string.IsNullOrEmpty(StatusFilter)) StatusFilter = "ALL";

            var query = _service.GetAll().AsQueryable();

            // 1. Filter theo Trạng thái
            if (StatusFilter != "ALL")
            {
                query = query.Where(o => o.Status == StatusFilter);
            }

            // 2. Filter theo Tên hoặc SĐT (MỚI)
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                string term = SearchTerm.ToLower();
                query = query.Where(o =>
                    (o.CustomerName != null && o.CustomerName.ToLower().Contains(term)) ||
                    (o.PhoneNumber != null && o.PhoneNumber.Contains(term)) ||
                    (o.Code != null && o.Code.ToLower().Contains(term))
                );
            }

            Items = query.Select(o => new OrderItem
            {
                Id = o.Id,
                Code = o.Code,
                ProductName = $"{o.ProductName}",
                CustomerName = o.CustomerName,
                PhoneNumber = o.PhoneNumber,
                Quantity = o.Quantity,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount, 
                Deposit = o.Deposit,
                ImportPrice = o.ImportPrice > 0 ? o.ImportPrice : 0,
                PaidAmount = o.TotalAmount - o.Deposit,
                IsSelected = false
            }).ToList();
        }

        public IActionResult OnPost()
        {
            var selectedItems = Items.Where(x => x.IsSelected).ToList();

            if (selectedItems.Count == 0 || string.IsNullOrEmpty(TargetStatus))
            {
                return RedirectToPage(new { StatusFilter, SearchTerm });
            }

            foreach (var item in selectedItems)
            {
                DateTime? dateToUpdate = null;
                decimal? priceToUpdate = null;
                decimal? paidToUpdate = null;

                // Logic phân loại hành động
                if (TargetStatus == "Đã về")
                {
                    dateToUpdate = item.ArrivalDate;
                    priceToUpdate = item.ImportPrice;
                }
                else if (TargetStatus == "Đã giao")
                {
                    paidToUpdate = item.PaidAmount;
                }

                _service.BulkUpdateOrder(item.Id, TargetStatus, dateToUpdate, priceToUpdate, paidToUpdate);
            }

            return RedirectToPage(new { StatusFilter, SearchTerm });
        }
    }
}
