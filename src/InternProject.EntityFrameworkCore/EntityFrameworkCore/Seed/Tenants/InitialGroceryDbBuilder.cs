using InternProject.Grocery;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InternProject.EntityFrameworkCore.Seed.Tenants;

public class InitialGroceryDbBuilder
{
    private readonly InternProjectDbContext _context;

    public InitialGroceryDbBuilder(InternProjectDbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        CreateCategoriesAndProducts();
        CreateCustomers();
    }

    private void CreateCategoriesAndProducts()
    {
        // 1. Seed Categories
        var categories = new List<Category>
        {
            new Category { Name = "Nước giải khát", Description = "Bao gồm các loại nước ngọt, nước khoáng, bia, nước tăng lực", IsActive = true },
            new Category { Name = "Bánh kẹo", Description = "Bao gồm các loại bánh quy, kẹo dẻo, khoai tây chiên, sô cô la", IsActive = true },
            new Category { Name = "Gia vị & Đồ khô", Description = "Hạt nêm, nước mắm, bột ngọt, mì ăn liền, gạo, miến", IsActive = true },
            new Category { Name = "Hóa mỹ phẩm & Tẩy rửa", Description = "Dầu gội, bột giặt, nước rửa chén, sữa tắm, kem đánh răng", IsActive = true },
            new Category { Name = "Đồ đông lạnh & Đồ hộp", Description = "Cá hộp, xúc xích, thịt đông lạnh, chả giò, lạp xưởng", IsActive = true },
            new Category { Name = "Sữa & Sản phẩm từ sữa", Description = "Sữa tươi, sữa đặc, sữa chua ăn, sữa chua uống, sữa bột", IsActive = true },
            new Category { Name = "Chăm sóc cá nhân", Description = "Kem đánh răng, bàn chải, dầu gội, sữa tắm, sữa rửa mặt, dao cạo", IsActive = true },
            new Category { Name = "Đồ dùng gia đình", Description = "Khăn giấy, màng bọc thực phẩm, túi rác, hộp nhựa đựng thực phẩm", IsActive = true }
        };

        foreach (var category in categories)
        {
            var existingCategory = _context.Categories.IgnoreQueryFilters().FirstOrDefault(x => x.Name == category.Name);
            if (existingCategory == null)
            {
                _context.Categories.Add(category);
            }
        }
        _context.SaveChanges();

        // Load categories from database with generated IDs
        var dbCategories = _context.Categories.IgnoreQueryFilters().ToList();
        var catBeverages = dbCategories.First(x => x.Name == "Nước giải khát").Id;
        var catSnacks = dbCategories.First(x => x.Name == "Bánh kẹo").Id;
        var catSpices = dbCategories.First(x => x.Name == "Gia vị & Đồ khô").Id;
        var catCosmetics = dbCategories.First(x => x.Name == "Hóa mỹ phẩm & Tẩy rửa").Id;
        var catFrozen = dbCategories.First(x => x.Name == "Đồ đông lạnh & Đồ hộp").Id;
        var catMilk = dbCategories.First(x => x.Name == "Sữa & Sản phẩm từ sữa").Id;
        var catPersonal = dbCategories.First(x => x.Name == "Chăm sóc cá nhân").Id;
        var catHousehold = dbCategories.First(x => x.Name == "Đồ dùng gia đình").Id;

        // 2. Seed Products
        var products = new List<Product>
        {
            // Nước giải khát
            new Product { Sku = "COCA320", Name = "Coca-Cola Lon 320ml", CategoryId = catBeverages, CostPrice = 8500M, SalePrice = 11000M, StockQuantity = 120, MinStock = 24, Unit = "Lon", IsActive = true },
            new Product { Sku = "AQUA500", Name = "Nước khoáng Aquafina 500ml", CategoryId = catBeverages, CostPrice = 4000M, SalePrice = 6000M, StockQuantity = 200, MinStock = 50, Unit = "Chai", IsActive = true },
            new Product { Sku = "KEN330", Name = "Bia Heineken Lon 330ml", CategoryId = catBeverages, CostPrice = 17500M, SalePrice = 21000M, StockQuantity = 8, MinStock = 24, Unit = "Lon", IsActive = true },
            new Product { Sku = "REDBULL250", Name = "Nước tăng lực Red Bull Lon 250ml", CategoryId = catBeverages, CostPrice = 11500M, SalePrice = 14000M, StockQuantity = 150, MinStock = 30, Unit = "Lon", IsActive = true },
            new Product { Sku = "PEPSI320", Name = "Nước ngọt Pepsi Lon 320ml", CategoryId = catBeverages, CostPrice = 8000M, SalePrice = 10000M, StockQuantity = 100, MinStock = 20, Unit = "Lon", IsActive = true },
            new Product { Sku = "TIGER330", Name = "Bia Tiger Lon 330ml", CategoryId = catBeverages, CostPrice = 15000M, SalePrice = 18000M, StockQuantity = 12, MinStock = 24, Unit = "Lon", IsActive = true },
            new Product { Sku = "OLONG455", Name = "Trà ô long Tea+ Plus 455ml", CategoryId = catBeverages, CostPrice = 8000M, SalePrice = 10000M, StockQuantity = 90, MinStock = 15, Unit = "Chai", IsActive = true },
            new Product { Sku = "C2TRATHAI", Name = "Trà xanh C2 hương chanh 360ml", CategoryId = catBeverages, CostPrice = 5000M, SalePrice = 7000M, StockQuantity = 110, MinStock = 20, Unit = "Chai", IsActive = true },

            // Bánh kẹo
            new Product { Sku = "CHOCO12", Name = "Bánh Choco-Pie Hộp 12 cái", CategoryId = catSnacks, CostPrice = 42000M, SalePrice = 55000M, StockQuantity = 45, MinStock = 10, Unit = "Hộp", IsActive = true },
            new Product { Sku = "HARIBO80", Name = "Kẹo dẻo Haribo Goldbears 80g", CategoryId = catSnacks, CostPrice = 18000M, SalePrice = 26000M, StockQuantity = 3, MinStock = 15, Unit = "Gói", IsActive = true },
            new Product { Sku = "PRINGLES107", Name = "Khoai tây chiên Pringles Sour Cream 107g", CategoryId = catSnacks, CostPrice = 32000M, SalePrice = 42000M, StockQuantity = 30, MinStock = 8, Unit = "Hộp", IsActive = true },
            new Product { Sku = "OISHITOMBAY", Name = "Bim bim Oishi tôm cay 80g", CategoryId = catSnacks, CostPrice = 5000M, SalePrice = 7000M, StockQuantity = 120, MinStock = 20, Unit = "Gói", IsActive = true },
            new Product { Sku = "OREO133", Name = "Bánh quy Oreo nhân kem vani 133g", CategoryId = catSnacks, CostPrice = 12000M, SalePrice = 16000M, StockQuantity = 60, MinStock = 12, Unit = "Gói", IsActive = true },
            new Product { Sku = "COSYMET", Name = "Bánh quy bơ Cosy Marie 136g", CategoryId = catSnacks, CostPrice = 11000M, SalePrice = 15000M, StockQuantity = 50, MinStock = 10, Unit = "Gói", IsActive = true },
            new Product { Sku = "ALPENLIEBE", Name = "Kẹo Alpenliebe hương dâu sữa", CategoryId = catSnacks, CostPrice = 9000M, SalePrice = 12000M, StockQuantity = 80, MinStock = 15, Unit = "Gói", IsActive = true },

            // Gia vị & Đồ khô
            new Product { Sku = "KNORR900", Name = "Hạt nêm Knorr Thịt thăn 900g", CategoryId = catSpices, CostPrice = 68000M, SalePrice = 82000M, StockQuantity = 25, MinStock = 5, Unit = "Gói", IsActive = true },
            new Product { Sku = "NAMNGU750", Name = "Nước mắm Chinsu Nam Ngư 750ml", CategoryId = catSpices, CostPrice = 35000M, SalePrice = 43000M, StockQuantity = 60, MinStock = 15, Unit = "Chai", IsActive = true },
            new Product { Sku = "HAOHAO", Name = "Mì Hảo Hảo Tôm chua cay", CategoryId = catSpices, CostPrice = 3200M, SalePrice = 4500M, StockQuantity = 350, MinStock = 100, Unit = "Gói", IsActive = true },
            new Product { Sku = "CHINSUTUONG", Name = "Tương ớt Chinsu Chai 250g", CategoryId = catSpices, CostPrice = 11000M, SalePrice = 14000M, StockQuantity = 95, MinStock = 20, Unit = "Chai", IsActive = true },
            new Product { Sku = "NESTLECAFE", Name = "Cà phê hòa tan Nescafé 3in1 hộp 20 gói", CategoryId = catSpices, CostPrice = 48000M, SalePrice = 58000M, StockQuantity = 40, MinStock = 10, Unit = "Hộp", IsActive = true },
            new Product { Sku = "MI3MIEN", Name = "Mì 3 Miền tôm chua cay", CategoryId = catSpices, CostPrice = 2500M, SalePrice = 3500M, StockQuantity = 280, MinStock = 80, Unit = "Gói", IsActive = true },
            new Product { Sku = "AJINOMOTO454", Name = "Bột ngọt Ajinomoto 454g", CategoryId = catSpices, CostPrice = 26000M, SalePrice = 32000M, StockQuantity = 35, MinStock = 8, Unit = "Gói", IsActive = true },
            new Product { Sku = "TUONGAN1L", Name = "Dầu ăn Tường An Cooking Oil 1L", CategoryId = catSpices, CostPrice = 39000M, SalePrice = 48000M, StockQuantity = 75, MinStock = 15, Unit = "Chai", IsActive = true },

            // Hóa mỹ phẩm & Tẩy rửa
            new Product { Sku = "SUNLIGHT750", Name = "Nước rửa chén Sunlight Trà xanh 750g", CategoryId = catCosmetics, CostPrice = 24000M, SalePrice = 31000M, StockQuantity = 40, MinStock = 10, Unit = "Chai", IsActive = true },
            new Product { Sku = "OMOMATIC3.9", Name = "Bột giặt OMO Matic Cửa trước 3.9kg", CategoryId = catCosmetics, CostPrice = 185000M, SalePrice = 220000M, StockQuantity = 15, MinStock = 4, Unit = "Túi", IsActive = true },
            new Product { Sku = "CLEAR630", Name = "Dầu gội Clear Bạc hà 630ml", CategoryId = catCosmetics, CostPrice = 125000M, SalePrice = 155000M, StockQuantity = 2, MinStock = 6, Unit = "Chai", IsActive = true },
            new Product { Sku = "COMFORT1.8", Name = "Nước xả vải Comfort một lần xả 1.8L", CategoryId = catCosmetics, CostPrice = 95000M, SalePrice = 115000M, StockQuantity = 22, MinStock = 5, Unit = "Túi", IsActive = true },
            new Product { Sku = "VIM900", Name = "Nước tẩy bồn cầu Vim diệt khuẩn 900ml", CategoryId = catCosmetics, CostPrice = 29000M, SalePrice = 36000M, StockQuantity = 30, MinStock = 8, Unit = "Chai", IsActive = true },
            new Product { Sku = "SUNLIGHTLAU", Name = "Nước lau sàn Sunlight hương hoa hạ 1L", CategoryId = catCosmetics, CostPrice = 22000M, SalePrice = 28000M, StockQuantity = 45, MinStock = 10, Unit = "Chai", IsActive = true },
            new Product { Sku = "GIFTLOC", Name = "Nước xịt kính Gift sắc biển 580ml", CategoryId = catCosmetics, CostPrice = 17000M, SalePrice = 22000M, StockQuantity = 25, MinStock = 5, Unit = "Chai", IsActive = true },

            // Đồ đông lạnh & Đồ hộp
            new Product { Sku = "CANGU170", Name = "Cá ngừ ngâm dầu Sea Crown 170g", CategoryId = catFrozen, CostPrice = 22000M, SalePrice = 28000M, StockQuantity = 18, MinStock = 5, Unit = "Hộp", IsActive = true },
            new Product { Sku = "XUCHXICHVISSAN", Name = "Xúc xích tiệt trùng Vissan 175g", CategoryId = catFrozen, CostPrice = 16500M, SalePrice = 22000M, StockQuantity = 50, MinStock = 15, Unit = "Gói", IsActive = true },
            new Product { Sku = "ONGTHORED", Name = "Sữa đặc có đường Ông Thọ đỏ 380g", CategoryId = catFrozen, CostPrice = 21000M, SalePrice = 27000M, StockQuantity = 80, MinStock = 20, Unit = "Lon", IsActive = true },
            new Product { Sku = "SPAM340", Name = "Thịt hộp Spam Less Sodium 340g", CategoryId = catFrozen, CostPrice = 68000M, SalePrice = 85000M, StockQuantity = 14, MinStock = 4, Unit = "Hộp", IsActive = true },
            new Product { Sku = "HACAO500", Name = "Há cảo Cầu Tre gói 500g", CategoryId = catFrozen, CostPrice = 52000M, SalePrice = 65000M, StockQuantity = 16, MinStock = 5, Unit = "Gói", IsActive = true },
            new Product { Sku = "PHOMAI8", Name = "Phô mai Con Bò Cười hộp 8 miếng", CategoryId = catFrozen, CostPrice = 33000M, SalePrice = 42000M, StockQuantity = 35, MinStock = 8, Unit = "Hộp", IsActive = true },

            // Sữa & Sản phẩm từ sữa
            new Product { Sku = "THTRUE1L", Name = "Sữa tươi tiệt trùng TH True Milk ít đường 1L", CategoryId = catMilk, CostPrice = 30000M, SalePrice = 36000M, StockQuantity = 50, MinStock = 10, Unit = "Hộp", IsActive = true },
            new Product { Sku = "VINAMILK180", Name = "Sữa dinh dưỡng Vinamilk có đường lốc 4x180ml", CategoryId = catMilk, CostPrice = 24000M, SalePrice = 28000M, StockQuantity = 80, MinStock = 15, Unit = "Lốc", IsActive = true },
            new Product { Sku = "MILO180", Name = "Sữa lúa mạch Nestlé Milo lốc 4x180ml", CategoryId = catMilk, CostPrice = 25000M, SalePrice = 30000M, StockQuantity = 9, MinStock = 12, Unit = "Lốc", IsActive = true },
            new Product { Sku = "YAKULT5", Name = "Sữa chua uống lên men Yakult lốc 5 chai", CategoryId = catMilk, CostPrice = 20000M, SalePrice = 25000M, StockQuantity = 40, MinStock = 8, Unit = "Lốc", IsActive = true },
            new Product { Sku = "SUACHUAVINA", Name = "Sữa chua ăn Vinamilk có đường lốc 4 hộp", CategoryId = catMilk, CostPrice = 21000M, SalePrice = 26000M, StockQuantity = 30, MinStock = 5, Unit = "Lốc", IsActive = true },

            // Chăm sóc cá nhân
            new Product { Sku = "PS200", Name = "Kem đánh răng P/S ngừa sâu răng 200g", CategoryId = catPersonal, CostPrice = 18000M, SalePrice = 23000M, StockQuantity = 60, MinStock = 10, Unit = "Hộp", IsActive = true },
            new Product { Sku = "ROMANO180", Name = "Dầu gội nước hoa Romano Classic 180g", CategoryId = catPersonal, CostPrice = 52000M, SalePrice = 65000M, StockQuantity = 18, MinStock = 5, Unit = "Chai", IsActive = true },
            new Product { Sku = "LIFEBUOY850", Name = "Sữa tắm Lifebuoy Bảo vệ vượt trội 850g", CategoryId = catPersonal, CostPrice = 120000M, SalePrice = 145000M, StockQuantity = 12, MinStock = 3, Unit = "Chai", IsActive = true },
            new Product { Sku = "SENSODYNE100", Name = "Kem đánh răng Sensodyne phục hồi 100g", CategoryId = catPersonal, CostPrice = 60000M, SalePrice = 72000M, StockQuantity = 4, MinStock = 8, Unit = "Hộp", IsActive = true },
            new Product { Sku = "COLGATE150", Name = "Bàn chải đánh răng Colgate Cushion Clean", CategoryId = catPersonal, CostPrice = 28000M, SalePrice = 35000M, StockQuantity = 45, MinStock = 10, Unit = "Cái", IsActive = true },

            // Đồ dùng gia đình
            new Product { Sku = "KHANGIAYMIY", Name = "Khăn giấy rút cao cấp Silkwell 280 tờ", CategoryId = catHousehold, CostPrice = 13000M, SalePrice = 18000M, StockQuantity = 110, MinStock = 20, Unit = "Gói", IsActive = true },
            new Product { Sku = "MANGTHUCPHAM", Name = "Màng bọc thực phẩm Laspalm 30cm x 150m", CategoryId = catHousehold, CostPrice = 54000M, SalePrice = 68000M, StockQuantity = 22, MinStock = 5, Unit = "Cuộn", IsActive = true },
            new Product { Sku = "TUIRAC3", Name = "Túi rác đen tự hủy sinh học Opec 3 cuộn", CategoryId = catHousehold, CostPrice = 24000M, SalePrice = 32000M, StockQuantity = 70, MinStock = 15, Unit = "Gói", IsActive = true },
            new Product { Sku = "HOPNHUA1200", Name = "Hộp nhựa Duy Tân chữ nhật 1200ml", CategoryId = catHousehold, CostPrice = 19000M, SalePrice = 25000M, StockQuantity = 30, MinStock = 5, Unit = "Cái", IsActive = true },
            new Product { Sku = "NENSINHNHAT", Name = "Nến Tealight hộp 10 viên", CategoryId = catHousehold, CostPrice = 10000M, SalePrice = 15000M, StockQuantity = 2, MinStock = 5, Unit = "Hộp", IsActive = true }
        };

        foreach (var product in products)
        {
            var existingProduct = _context.Products.IgnoreQueryFilters().FirstOrDefault(x => x.Sku == product.Sku);
            if (existingProduct == null)
            {
                _context.Products.Add(product);
            }
        }
        _context.SaveChanges();
    }

    private void CreateCustomers()
    {
        var customers = new List<Customer>
        {
            new Customer { Code = "KH-001", Name = "Nguyễn Văn Hùng", Phone = "0903123456", Address = "123 Đường 3/2, Quận 10, TP. HCM", IsActive = true },
            new Customer { Code = "KH-002", Name = "Trần Thị Mai", Phone = "0918765432", Address = "456 Lê Lợi, Quận 1, TP. HCM", IsActive = true },
            new Customer { Code = "KH-003", Name = "Lê Hoàng Nam", Phone = "0987112233", Address = "789 Nguyễn Trãi, Quận 5, TP. HCM", IsActive = true },
            new Customer { Code = "KH-004", Name = "Phạm Minh Tuấn", Phone = "0966445566", Address = "101 Cách Mạng Tháng Tám, Quận Tân Bình, TP. HCM", IsActive = true },
            new Customer { Code = "KH-005", Name = "Vũ Thị Hồng", Phone = "0933221100", Address = "202 Điện Biên Phủ, Quận Bình Thạnh, TP. HCM", IsActive = true },
            new Customer { Code = "KH-006", Name = "Đỗ Gia Bảo", Phone = "0977889900", Address = "303 Lý Thường Kiệt, Quận 11, TP. HCM", IsActive = false },
            new Customer { Code = "KH-007", Name = "Nguyễn Hoàng Long", Phone = "0902888777", Address = "456 Hoàng Diệu, Quận 4, TP. HCM", IsActive = true },
            new Customer { Code = "KH-008", Name = "Phan Thanh Thảo", Phone = "0938444555", Address = "78 Đường Số 9, Quận 7, TP. HCM", IsActive = true },
            new Customer { Code = "KH-009", Name = "Bùi Anh Tuấn", Phone = "0919222333", Address = "159 Xa Lộ Hà Nội, Quận 2, TP. HCM", IsActive = true },
            new Customer { Code = "KH-010", Name = "Ngô Mỹ Linh", Phone = "0988666777", Address = "88 Trần Hưng Đạo, Quận 1, TP. HCM", IsActive = true },
            new Customer { Code = "KH-011", Name = "Đặng Quốc Khánh", Phone = "0972333444", Address = "215 Nguyễn Văn Cừ, Quận 5, TP. HCM", IsActive = true },
            new Customer { Code = "KH-012", Name = "Trịnh Hoài Nam", Phone = "0909555666", Address = "12 Lê Văn Sỹ, Quận Phú Nhuận, TP. HCM", IsActive = true },
            new Customer { Code = "KH-013", Name = "Lý Thị Thu Hà", Phone = "0912111222", Address = "54 Nguyễn Chí Thanh, Quận Đống Đa, Hà Nội", IsActive = true },
            new Customer { Code = "KH-014", Name = "Hoàng Văn Thái", Phone = "0963777888", Address = "120 Bà Triệu, Quận Hai Bà Trưng, Hà Nội", IsActive = true },
            new Customer { Code = "KH-015", Name = "Mai Xuân Trường", Phone = "0984999000", Address = "310 Trần Phú, Quận Hồng Bàng, Hải Phòng", IsActive = true },
            new Customer { Code = "KH-016", Name = "Đỗ Thị Kim Oanh", Phone = "0935123987", Address = "89 Hùng Vương, Quận Hải Châu, Đà Nẵng", IsActive = true },
            new Customer { Code = "KH-017", Name = "Nguyễn Minh Triết", Phone = "0905654321", Address = "234 Nguyễn Văn Linh, Quận Thanh Khê, Đà Nẵng", IsActive = true },
            new Customer { Code = "KH-018", Name = "Vũ Anh Dũng", Phone = "0914852963", Address = "12A Lê Hồng Phong, TP. Nha Trang, Khánh Hòa", IsActive = true },
            new Customer { Code = "KH-019", Name = "Lâm Minh Thư", Phone = "0979369258", Address = "45 Mậu Thân, Quận Ninh Kiều, Cần Thơ", IsActive = true },
            new Customer { Code = "KH-020", Name = "Lê Quốc Anh", Phone = "0908147258", Address = "90 Trần Phú, TP. Vũng Tàu, Bà Rịa - Vũng Tàu", IsActive = false }
        };

        foreach (var customer in customers)
        {
            var existingCustomer = _context.Customers.IgnoreQueryFilters().FirstOrDefault(x => x.Code == customer.Code);
            if (existingCustomer == null)
            {
                _context.Customers.Add(customer);
            }
        }
        _context.SaveChanges();
    }
}

