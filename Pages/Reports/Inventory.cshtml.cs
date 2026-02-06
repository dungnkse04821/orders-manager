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

        public void OnGet()
        {
            var orders = _service.GetAll();

            // 1. THỐNG KÊ THEO NGUỒN HÀNG (SOURCE)
            SourceStats = orders
                .Where(o => !string.IsNullOrEmpty(o.Source))
                .GroupBy(o => o.Source)
                .Select(g => new SupplierStat
                {
                    Name = g.Key,
                    OrderCount = g.Count(),
                    ProductCount = g.Sum(o => o.Quantity),
                    // Tính tổng vốn: Giá nhập * Số lượng
                    TotalImportValue = g.Sum(o => (o.ImportPrice == 0 ? 0 : o.ImportPrice) * o.Quantity)
                })
                .OrderByDescending(x => x.TotalImportValue)
                .ToList();

            // 2. THỐNG KÊ THEO KHO HÀNG (WAREHOUSE)
            WarehouseStats = orders
                .Where(o => !string.IsNullOrEmpty(o.Warehouse))
                .GroupBy(o => o.Warehouse)
                .Select(g => new SupplierStat
                {
                    Name = g.Key,
                    OrderCount = g.Count(),
                    ProductCount = g.Sum(o => o.Quantity),
                    TotalImportValue = g.Sum(o => (o.ImportPrice ?? 0) * o.Quantity)
                })
                .OrderByDescending(x => x.TotalImportValue)
                .ToList();
        }
    }
}