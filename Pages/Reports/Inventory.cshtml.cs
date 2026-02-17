using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrdersManager.Models;
using System.Collections.Generic;
using System.Linq;

namespace OrdersManager.Pages.Reports
{
    public class InventoryModel : PageModel
    {
        private readonly GoogleSheetService _service;

        public InventoryModel(GoogleSheetService service)
        {
            _service = service;
        }

        // Class chứa dữ liệu thống kê
        public class SupplierStat
        {
            public string Name { get; set; }
            public int OrderCount { get; set; }     // Số đơn
            public int ProductCount { get; set; }   // Số sản phẩm
            public decimal TotalImportValue { get; set; } // Tổng vốn nhập
        }

        public List<SupplierStat> SourceStats { get; set; } = new List<SupplierStat>();
        public List<SupplierStat> WarehouseStats { get; set; } = new List<SupplierStat>();

        [BindProperty(SupportsGet = true)] public string FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public string ToDate { get; set; }
        [BindProperty(SupportsGet = true)] public string StatusFilter { get; set; }
        public void OnGet()
        {

            // 1. Lấy tất cả dữ liệu
            var query = _service.GetAll().AsQueryable();

            if (!string.IsNullOrEmpty(FromDate) && DateTime.TryParse(FromDate, out DateTime from))
            {
                query = query.Where(o => o.OrderDate >= from);
            }
            if (!string.IsNullOrEmpty(ToDate) && DateTime.TryParse(ToDate, out DateTime to))
            {
                query = query.Where(o => o.OrderDate <= to);
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "ALL")
            {
                query = query.Where(o => o.Status == StatusFilter);
            }
            else
            {
                // Mặc định: Nếu không chọn gì, nên ẩn đơn Hủy để số liệu chính xác
                // (Hoặc tùy bạn muốn hiện cả Hủy thì bỏ dòng này đi)
                query = query.Where(o => o.Status != "Hủy");
            }

            // 3. GOM NHÓM & TÍNH TOÁN (Sau khi đã lọc)

            // Thống kê Nguồn hàng
            SourceStats = query
                .Where(o => !string.IsNullOrEmpty(o.Source))
                .GroupBy(o => o.Source)
                .Select(g => new SupplierStat
                {
                    Name = g.Key,
                    OrderCount = g.Count(),
                    ProductCount = g.Sum(o => o.Quantity),
                    TotalImportValue = g.Sum(o => (o.ImportPrice == 0 ? 0 : o.ImportPrice) * o.Quantity)
                })
                .OrderByDescending(x => x.TotalImportValue)
                .ToList();

            // Thống kê Kho hàng
            WarehouseStats = query
                .Where(o => !string.IsNullOrEmpty(o.Warehouse))
                .GroupBy(o => o.Warehouse)
                .Select(g => new SupplierStat
                {
                    Name = g.Key,
                    OrderCount = g.Count(),
                    ProductCount = g.Sum(o => o.Quantity),
                    TotalImportValue = g.Sum(o => (o.ImportPrice == 0 ? 0 : o.ImportPrice) * o.Quantity)
                })
                .OrderByDescending(x => x.TotalImportValue)
                .ToList();
        }
    }
}