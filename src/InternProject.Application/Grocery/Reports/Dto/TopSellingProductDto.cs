namespace InternProject.Grocery.Reports.Dto
{
    public class TopSellingProductDto
    {
        public string Sku { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public int SoldQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
    }
}
