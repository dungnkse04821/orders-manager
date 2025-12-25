using System.ComponentModel;

namespace OrdersManager.Models
{
    public class Customer
    {
        public string Id { get; set; }

        [DisplayName("Họ và tên")]
        public string FullName { get; set; }

        [DisplayName("Số điện thoại")]
        public string PhoneNumber { get; set; }

        [DisplayName("Địa chỉ")]
        public string Address { get; set; }

        [DisplayName("Ghi chú")]
        public string? Note { get; set; }
    }
}
