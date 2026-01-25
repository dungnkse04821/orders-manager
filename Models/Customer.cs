using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OrdersManager.Models
{
    public class Customer
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Họ và tên")]
        [DisplayName("Họ và tên")]
        public string FullName { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập Số điện thoại")]
        [DisplayName("Số điện thoại")]
        public string PhoneNumber { get; set; }

        [DisplayName("Reference")]
        public string? Reference { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập Địa chỉ")]
        [DisplayName("Địa chỉ")]
        public string Address { get; set; }

        [DisplayName("Ghi chú")]
        public string? Note { get; set; }
    }
}
