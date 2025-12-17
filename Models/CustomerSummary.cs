namespace OrdersManager.Models
{
    public class CustomerSummary
    {
        public string CustomerName { get; set; }
        public int OrderCount { get; set; } // Số lượng đơn
        public decimal TotalSpent { get; set; } // Tổng tiền đã mua
        public DateTime? LastOrderDate { get; set; } // Ngày mua gần nhất
    }
}
