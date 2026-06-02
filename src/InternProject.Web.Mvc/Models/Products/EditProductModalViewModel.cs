using InternProject.Grocery.Products.Dto;
using System.Collections.Generic;

namespace InternProject.Web.Models.Products;

public class EditProductModalViewModel
{
    public ProductDto Product { get; set; }
    public IReadOnlyList<CategoryLookupDto> Categories { get; set; }
}
