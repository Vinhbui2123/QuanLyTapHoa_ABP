using System;

namespace InternProject.Grocery.Reports.Dto
{
    public class GetTopSellingProductsInput
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int TopN { get; set; } = 10;
        public string SortBy { get; set; } = "Quantity"; // "Quantity", "Revenue", "Profit"
    }
}
