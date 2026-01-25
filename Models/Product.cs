namespace OrdersManager.Models
{
    public class Product
    {
        public string Sku { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal ImportPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public string Source { get; set; }
        public string Warehouse { get; set; }
        public string Reference { get; set; }

        // Property phụ để hiển thị đẹp trên Dropdown (VD: "AO01 - Áo Thun")
        public string DisplayText => $"{Sku} - {Name}";
    }
}
