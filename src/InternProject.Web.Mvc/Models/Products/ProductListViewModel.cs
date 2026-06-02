using InternProject.Grocery.Products.Dto;
using System.Collections.Generic;

namespace InternProject.Web.Models.Products;

public class ProductListViewModel
{
    public IReadOnlyList<CategoryLookupDto> Categories { get; set; }
}
