using Abp.Authorization;
using Abp.Domain.Repositories;
using InternProject.Authorization;
using InternProject.Grocery.Reports.Dto;
using InternProject.Authorization.Users;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InternProject.Grocery.Reports
{
    [AbpAuthorize(PermissionNames.Pages_Reports)]
    public class ReportsAppService : InternProjectAppServiceBase, IReportsAppService
    {
        // ReportsAppService chỉ đọc dữ liệu để tổng hợp báo cáo, không thay đổi tồn kho/hóa đơn.
        // Các query dùng AsNoTracking để EF Core không cần theo dõi entity, đọc báo cáo nhanh hơn.
        private readonly IRepository<Invoice, Guid> _invoiceRepository;
        private readonly IRepository<Product, Guid> _productRepository;
        private readonly IRepository<StockBatch, Guid> _stockBatchRepository;
        private readonly IRepository<Category, Guid> _categoryRepository;

        public ReportsAppService(
            IRepository<Invoice, Guid> invoiceRepository,
            IRepository<Product, Guid> productRepository,
            IRepository<StockBatch, Guid> stockBatchRepository,
            IRepository<Category, Guid> categoryRepository)
        {
            _invoiceRepository = invoiceRepository;
            _productRepository = productRepository;
            _stockBatchRepository = stockBatchRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<DashboardOverviewDto> GetDashboardOverviewAsync()
        {
            // Dashboard tổng quan lấy số liệu nhanh cho ngày hiện tại và biểu đồ 6 tháng gần nhất.
            var todayStart = DateTime.Today;
            var todayEnd = DateTime.Today.AddDays(1);

            var todayInvoicesQuery = _invoiceRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.Status == InvoiceStatus.Completed && x.CreationTime >= todayStart && x.CreationTime < todayEnd);

            var todayInvoicesCount = await todayInvoicesQuery.CountAsync();
            var todayRevenue = await todayInvoicesQuery.SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

            // Giá vốn ưu tiên lấy từ InvoiceItemBatches vì đó là giá nhập thực tế của lô đã bán.
            // Nếu dữ liệu cũ chưa có batch thì fallback sang Product.CostPrice.
            var todayCogs = await todayInvoicesQuery
                .SelectMany(x => x.InvoiceItems)
                .SumAsync(item => 
                    item.InvoiceItemBatches.Any() 
                        ? item.InvoiceItemBatches.Sum(b => b.Quantity * b.CostPrice) 
                        : (item.Quantity * item.Product.CostPrice)
                );

            var todayProfit = todayRevenue - todayCogs;

            // Đếm sản phẩm hết hàng/sắp hết hàng để hiển thị cảnh báo vận hành.
            var outOfStockCount = await _productRepository.GetAll()
                .AsNoTracking()
                .CountAsync(p => p.IsActive && p.StockQuantity <= 0);

            var lowStockCount = await _productRepository.GetAll()
                .AsNoTracking()
                .CountAsync(p => p.IsActive && p.StockQuantity > 0 && p.StockQuantity <= p.MinStock);

            // Lấy 5 hóa đơn gần nhất cho khu vực hoạt động gần đây trên dashboard.
            var recentInvoices = await _invoiceRepository.GetAll()
                .AsNoTracking()
                .OrderByDescending(x => x.CreationTime)
                .Take(5)
                .Select(x => new RecentInvoiceDto
                {
                    InvoiceNumber = x.InvoiceNumber,
                    CustomerName = x.Customer != null ? x.Customer.Name : null,
                    TotalAmount = x.TotalAmount,
                    Status = x.Status == InvoiceStatus.Completed ? "Completed" : "Cancelled",
                    CreationTime = x.CreationTime
                })
                .ToListAsync();

            // Gom doanh thu/lợi nhuận 6 tháng gần nhất để vẽ biểu đồ xu hướng.
            var sixMonthsAgo = DateTime.Today.AddMonths(-5);
            sixMonthsAgo = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

            var monthlyStatsRaw = await _invoiceRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.Status == InvoiceStatus.Completed && x.CreationTime >= sixMonthsAgo)
                .Select(x => new {
                    x.CreationTime.Year,
                    x.CreationTime.Month,
                    x.TotalAmount,
                    TotalCost = x.InvoiceItems.Sum(item => 
                        item.InvoiceItemBatches.Any() 
                            ? item.InvoiceItemBatches.Sum(b => b.Quantity * b.CostPrice) 
                            : (item.Quantity * item.Product.CostPrice)
                    )
                })
                .ToListAsync();

            var monthlyData = monthlyStatsRaw
                .GroupBy(x => new { x.Year, x.Month })
                .Select(g => new MonthlyRevenueDto {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Revenue = g.Sum(x => x.TotalAmount),
                    Profit = g.Sum(x => x.TotalAmount - x.TotalCost)
                })
                .OrderBy(x => x.Month)
                .ToList();

            // Bổ sung tháng không có doanh thu bằng 0 để biểu đồ luôn đủ 6 mốc thời gian.
            var finalMonthlyData = new List<MonthlyRevenueDto>();
            for (int i = -5; i <= 0; i++)
            {
                var targetMonth = DateTime.Today.AddMonths(i).ToString("yyyy-MM");
                var existing = monthlyData.FirstOrDefault(m => m.Month == targetMonth);
                if (existing != null)
                {
                    finalMonthlyData.Add(existing);
                }
                else
                {
                    finalMonthlyData.Add(new MonthlyRevenueDto
                    {
                        Month = targetMonth,
                        Revenue = 0,
                        Profit = 0
                    });
                }
            }

            return new DashboardOverviewDto
            {
                TodayRevenue = todayRevenue,
                TodayProfit = todayProfit,
                TodayInvoicesCount = todayInvoicesCount,
                LowStockProductsCount = lowStockCount,
                OutOfStockProductsCount = outOfStockCount,
                RecentInvoices = recentInvoices,
                MonthlyRevenueData = finalMonthlyData
            };
        }

        public async Task<RevenueReportDto> GetRevenueReportAsync(GetRevenueReportInput input)
        {
            // EndDate cộng thêm 1 ngày và dùng dấu < để lấy trọn ngày kết thúc.
            var endOfDayLimit = input.EndDate.Date.AddDays(1);

            var invoicesQuery = _invoiceRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.CreationTime >= input.StartDate.Date && x.CreationTime < endOfDayLimit);

            var totalCount = await invoicesQuery.CountAsync();
            var totalCancelled = await invoicesQuery.CountAsync(x => x.Status == InvoiceStatus.Cancelled);

            var completedInvoicesQuery = invoicesQuery.Where(x => x.Status == InvoiceStatus.Completed);

            var totalRevenue = await completedInvoicesQuery.SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

            // Tính giá vốn hàng bán: dùng giá vốn theo lô đã xuất để lợi nhuận sát thực tế.
            var totalCost = await completedInvoicesQuery
                .SelectMany(x => x.InvoiceItems)
                .SumAsync(item => 
                    item.InvoiceItemBatches.Any() 
                        ? item.InvoiceItemBatches.Sum(b => b.Quantity * b.CostPrice) 
                        : (item.Quantity * item.Product.CostPrice)
                );

            var totalProfit = totalRevenue - totalCost;
            var profitMargin = totalRevenue > 0 ? (totalProfit / totalRevenue) * 100 : 0;

            // Chuẩn bị dữ liệu biểu đồ theo ngày hoặc theo tháng tùy lựa chọn ở UI.
            var rawChartPoints = await completedInvoicesQuery
                .Select(x => new {
                    x.CreationTime,
                    x.TotalAmount,
                    TotalCost = x.InvoiceItems.Sum(item => 
                        item.InvoiceItemBatches.Any() 
                            ? item.InvoiceItemBatches.Sum(b => b.Quantity * b.CostPrice) 
                            : (item.Quantity * item.Product.CostPrice)
                    )
                })
                .ToListAsync();

            List<RevenueChartPointDto> chartPoints;
            if (input.GroupBy == "Month")
            {
                chartPoints = rawChartPoints
                    .GroupBy(x => new { x.CreationTime.Year, x.CreationTime.Month })
                    .Select(g => new RevenueChartPointDto
                    {
                        TimeLabel = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Revenue = g.Sum(x => x.TotalAmount),
                        Cost = g.Sum(x => x.TotalCost),
                        Profit = g.Sum(x => x.TotalAmount - x.TotalCost)
                    })
                    .OrderBy(x => x.TimeLabel)
                    .ToList();
            }
            else
            {
                chartPoints = rawChartPoints
                    .GroupBy(x => x.CreationTime.Date)
                    .Select(g => new RevenueChartPointDto
                    {
                        TimeLabel = g.Key.ToString("yyyy-MM-dd"),
                        Revenue = g.Sum(x => x.TotalAmount),
                        Cost = g.Sum(x => x.TotalCost),
                        Profit = g.Sum(x => x.TotalAmount - x.TotalCost)
                    })
                    .OrderBy(x => x.TimeLabel)
                    .ToList();
            }

            var invoicesList = await completedInvoicesQuery
                .Select(x => new RevenueInvoiceDetailDto
                {
                    InvoiceNumber = x.InvoiceNumber,
                    CustomerName = x.Customer != null ? x.Customer.Name : null,
                    CashierName = x.CashierUser != null ? x.CashierUser.Name : null,
                    TotalAmount = x.TotalAmount,
                    TotalCost = x.InvoiceItems.Sum(item => 
                        item.InvoiceItemBatches.Any() 
                            ? item.InvoiceItemBatches.Sum(b => b.Quantity * b.CostPrice) 
                            : (item.Quantity * item.Product.CostPrice)
                    ),
                    PaymentMethod = x.PaymentMethod.ToString(),
                    CreationTime = x.CreationTime
                })
                .OrderByDescending(x => x.CreationTime)
                .ToListAsync();

            // TotalProfit không lấy trực tiếp từ SQL ở bước trước để DTO dễ đọc và thống nhất công thức.
            foreach (var inv in invoicesList)
            {
                inv.TotalProfit = inv.TotalAmount - inv.TotalCost;
            }

            return new RevenueReportDto
            {
                TotalRevenue = totalRevenue,
                TotalCost = totalCost,
                TotalProfit = totalProfit,
                ProfitMarginPercent = profitMargin,
                TotalInvoices = totalCount - totalCancelled,
                TotalCancelledInvoices = totalCancelled,
                ChartPoints = chartPoints,
                Invoices = invoicesList
            };
        }

        public async Task<InventoryReportDto> GetInventoryReportAsync(GetInventoryReportInput input)
        {
            // Định giá tồn kho dựa trên số lượng còn lại từng lô * giá nhập của lô.
            // Cách này chính xác hơn dùng Product.CostPrice trung bình.
            var stockBatches = await _stockBatchRepository.GetAll()
                .AsNoTracking()
                .Where(b => b.RemainingQuantity > 0)
                .Select(b => new {
                    b.ProductId,
                    b.RemainingQuantity,
                    b.ImportPrice
                })
                .ToListAsync();

            var batchValuations = stockBatches
                .GroupBy(b => b.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(b => b.RemainingQuantity * b.ImportPrice));

            var totalStockValuation = stockBatches.Sum(b => b.RemainingQuantity * b.ImportPrice);

            // Lấy danh sách sản phẩm kèm danh mục để tạo bảng chi tiết tồn kho.
            var productsQuery = _productRepository.GetAll()
                .AsNoTracking()
                .Include(p => p.Category);

            var productsRaw = await productsQuery
                .Select(p => new {
                    p.Id,
                    p.Sku,
                    ProductName = p.Name,
                    CategoryName = p.Category != null ? p.Category.Name : "Chưa phân loại",
                    p.StockQuantity,
                    p.CostPrice,
                    p.SalePrice,
                    p.MinStock,
                    p.IsActive
                })
                .ToListAsync();

            var productStocksList = productsRaw
                .Select(p => {
                    var valuation = batchValuations.ContainsKey(p.Id) 
                        ? batchValuations[p.Id] 
                        : (p.StockQuantity * p.CostPrice);

                    // Trạng thái tồn kho dùng MinStock của từng sản phẩm làm ngưỡng cảnh báo.
                    var status = p.StockQuantity <= 0 ? "OutOfStock" : (p.StockQuantity <= p.MinStock ? "LowStock" : "InStock");

                    return new InventoryItemDetailDto
                    {
                        Id = p.Id,
                        Sku = p.Sku,
                        ProductName = p.ProductName,
                        CategoryName = p.CategoryName,
                        StockQuantity = p.StockQuantity,
                        CostPrice = p.CostPrice,
                        SalePrice = p.SalePrice,
                        StockValuation = valuation,
                        StockStatus = status
                    };
                })
                .OrderBy(x => x.StockQuantity)
                .ToList();

            // Tìm các lô còn tồn và có hạn dùng để cảnh báo gần hết hạn/quá hạn.
            var today = DateTime.Today;

            var expiringBatchesQuery = _stockBatchRepository.GetAll()
                .AsNoTracking()
                .Include(b => b.Product)
                .Include(b => b.Supplier)
                .Where(b => b.RemainingQuantity > 0 && b.ExpiryDate != null);

            var expiringBatchesRaw = await expiringBatchesQuery
                .Select(b => new {
                    b.BatchCode,
                    ProductName = b.Product.Name,
                    SupplierName = b.Supplier != null ? b.Supplier.Name : "Không rõ",
                    b.RemainingQuantity,
                    b.ImportPrice,
                    b.ExpiryDate
                })
                .ToListAsync();

            var expiringBatchesList = expiringBatchesRaw
                .Select(b => {
                    var daysToExpiry = (b.ExpiryDate.Value.Date - today).Days;
                    return new ExpiringBatchDetailDto
                    {
                        BatchCode = b.BatchCode,
                        ProductName = b.ProductName,
                        SupplierName = b.SupplierName,
                        RemainingQuantity = b.RemainingQuantity,
                        ImportPrice = b.ImportPrice,
                        ExpiryDate = b.ExpiryDate,
                        DaysToExpiry = daysToExpiry,
                        Status = daysToExpiry < 0 ? "Expired" : "NearExpiry"
                    };
                })
                .Where(b => b.DaysToExpiry <= input.NearExpiryDays)
                .OrderBy(b => b.DaysToExpiry)
                .ToList();

            var totalItemsInStock = productStocksList.Sum(x => x.StockQuantity);
            var expiringCount = expiringBatchesList.Count(x => x.DaysToExpiry >= 0);
            var expiredCount = expiringBatchesList.Count(x => x.DaysToExpiry < 0);

            return new InventoryReportDto
            {
                TotalStockValuation = totalStockValuation,
                TotalItemsInStock = totalItemsInStock,
                ExpiringBatchesCount = expiringCount,
                ExpiredBatchesCount = expiredCount,
                ProductStocks = productStocksList,
                ExpiringBatches = expiringBatchesList
            };
        }

        public async Task<List<TopSellingProductDto>> GetTopSellingProductsReportAsync(GetTopSellingProductsInput input)
        {
            // Top bán chạy chỉ tính hóa đơn Completed, không tính hóa đơn đã hủy.
            var query = _invoiceRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.Status == InvoiceStatus.Completed);

            if (input.StartDate.HasValue)
            {
                query = query.Where(x => x.CreationTime >= input.StartDate.Value);
            }
            if (input.EndDate.HasValue)
            {
                var endOfDay = input.EndDate.Value.Date.AddDays(1);
                query = query.Where(x => x.CreationTime < endOfDay);
            }

            var itemsQuery = query.SelectMany(x => x.InvoiceItems);

            // Gom theo sản phẩm để tính tổng số lượng bán, doanh thu và lợi nhuận từng sản phẩm.
            var groupedProducts = await itemsQuery
                .GroupBy(x => new { 
                    x.ProductId, 
                    x.ProductName, 
                    x.Sku, 
                    CategoryName = x.Product.Category != null ? x.Product.Category.Name : "Chưa phân loại" 
                })
                .Select(g => new {
                    g.Key.Sku,
                    g.Key.ProductName,
                    g.Key.CategoryName,
                    SoldQuantity = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Subtotal),
                    TotalCost = g.Sum(x => 
                        x.InvoiceItemBatches.Any()
                            ? x.InvoiceItemBatches.Sum(b => b.Quantity * b.CostPrice)
                            : (x.Quantity * x.Product.CostPrice)
                    )
                })
                .ToListAsync();

            var result = groupedProducts
                .Select(x => new TopSellingProductDto
                {
                    Sku = x.Sku,
                    ProductName = x.ProductName,
                    CategoryName = x.CategoryName,
                    SoldQuantity = x.SoldQuantity,
                    TotalRevenue = x.TotalRevenue,
                    TotalProfit = x.TotalRevenue - x.TotalCost
                });

            if (input.SortBy == "Revenue")
            {
                result = result.OrderByDescending(x => x.TotalRevenue);
            }
            else if (input.SortBy == "Profit")
            {
                result = result.OrderByDescending(x => x.TotalProfit);
            }
            else
            {
                result = result.OrderByDescending(x => x.SoldQuantity);
            }

            return result.Take(input.TopN).ToList();
        }
    }
}
