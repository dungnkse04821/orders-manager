using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OrdersManager.Models
{
    public class Order
    {
        public string Id { get; set; }

        // Cột B: Ngày đặt
        [DisplayName("Ngày đặt")]
        [DataType(DataType.Date)]
        public DateTime? OrderDate { get; set; }

        // Cột C: Hàng (Trung, Hàn, Nhật...)
        [DisplayName("Nguồn hàng")]
        public string Source { get; set; }

        // Cột D: Kho
        [DisplayName("Kho")]
        public string Warehouse { get; set; }

        // Cột E: Code
        [DisplayName("Mã sản phẩm/Code")]
        public string Code { get; set; } = string.Empty;

        // Cột F: Loại (Quần, Áo...)
        [DisplayName("Loại sản phẩm")]
        public string Category { get; set; }

        // Cột G: Sản phẩm
        [DisplayName("Tên sản phẩm")]
        public string ProductName { get; set; }

        // Cột H: Màu sắc
        [DisplayName("Màu sắc")]
        public string Color { get; set; }

        // Cột I: Size
        [DisplayName("Size")]
        public string Size { get; set; }

        // Cột J: Giá bán 
        [DisplayName("Giá bán")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal SellingPrice { get; set; }

        // Cột K: Số lượng đặt
        [DisplayName("Số lượng")]
        public int Quantity { get; set; }

        // Cột L: Tổng tiền (Công thức) - Chỉ đọc
        [DisplayName("Tổng tiền")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal TotalAmount { get; set; }

        // Cột M: Khách hàng
        [DisplayName("Khách hàng")]
        public string CustomerName { get; set; }

        // Cột N: Đặt cọc
        [DisplayName("Đặt cọc")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal Deposit { get; set; }

        // Cột O: Chiết khấu
        [DisplayName("Chiết khấu")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal Discount { get; set; }

        // Cột P: Số tiền TT khi nhận hàng (Công thức) - Chỉ đọc
        [DisplayName("Còn lại")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal RemainingAmount { get; set; }

        // Cột Q: Ngày về
        [DisplayName("Ngày về")]
        [DataType(DataType.Date)]
        public DateTime? ArrivalDate { get; set; }

        // Cột R: Ngày nhận thanh toán
        [DisplayName("Ngày TT")]
        [DataType(DataType.Date)]
        public DateTime? PaymentDate { get; set; }

        // Cột S: Đơn giá nhập
        [DisplayName("Giá nhập")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal ImportPrice { get; set; }

        // Cột T: Thành tiền nhập (Công thức) - Chỉ đọc
        [DisplayName("Tổng vốn")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal TotalImportCost { get; set; }

        // Cột U: Lãi (Công thức) - Chỉ đọc
        [DisplayName("Lãi")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal Profit { get; set; }

        [DisplayName("Trạng thái")]
        public string Status { get; set; } = "Chờ đặt";

        [DisplayName("Số điện thoại")]
        public string PhoneNumber { get; set; }

        [DisplayName("Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }
    }

    public static class OrderStatusList
    {
        public static List<string> All = new List<string>
        {
            "Chờ đặt",
            "Đã đặt",
            "Đang về",
            "Đã về",
            "Đã giao",
            "Hủy"
        };
    }
}
