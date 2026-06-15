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
            var todayStart = DateTime.Today;
            var todayEnd = DateTime.Today.AddDays(1);

            var todayInvoicesQuery = _invoiceRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.Status == InvoiceStatus.Completed && x.CreationTime >= todayStart && x.CreationTime < todayEnd);

            var todayInvoicesCount = await todayInvoicesQuery.CountAsync();
            var todayRevenue = await todayInvoicesQuery.SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

            // Calculate profit for today
            var todayCogs = await todayInvoicesQuery
                .SelectMany(x => x.InvoiceItems)
                .SumAsync(item => 
                    item.InvoiceItemBatches.Any() 
                        ? item.InvoiceItemBatches.Sum(b => b.Quantity * b.CostPrice) 
                        : (item.Quantity * item.Product.CostPrice)
                );

            var todayProfit = todayRevenue - todayCogs;

            // Out of stock and low stock counts
            var outOfStockCount = await _productRepository.GetAll()
                .AsNoTracking()
                .CountAsync(p => p.IsActive && p.StockQuantity <= 0);

            var lowStockCount = await _productRepository.GetAll()
                .AsNoTracking()
                .CountAsync(p => p.IsActive && p.StockQuantity > 0 && p.StockQuantity <= p.MinStock);

            // Recent invoices
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

            // Monthly revenue for the last 6 months
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

            // Fill missing months with zero
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
            var endOfDayLimit = input.EndDate.Date.AddDays(1);

            var invoicesQuery = _invoiceRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.CreationTime >= input.StartDate.Date && x.CreationTime < endOfDayLimit);

            var totalCount = await invoicesQuery.CountAsync();
            var totalCancelled = await invoicesQuery.CountAsync(x => x.Status == InvoiceStatus.Cancelled);

            var completedInvoicesQuery = invoicesQuery.Where(x => x.Status == InvoiceStatus.Completed);

            var totalRevenue = await completedInvoicesQuery.SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

            // Calculate cost of goods sold
            var totalCost = await completedInvoicesQuery
                .SelectMany(x => x.InvoiceItems)
                .SumAsync(item => 
                    item.InvoiceItemBatches.Any() 
                        ? item.InvoiceItemBatches.Sum(b => b.Quantity * b.CostPrice) 
                        : (item.Quantity * item.Product.CostPrice)
                );

            var totalProfit = totalRevenue - totalCost;
            var profitMargin = totalRevenue > 0 ? (totalProfit / totalRevenue) * 100 : 0;

            // Grouping for chart points
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

            // Populate TotalProfit
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
            // Calculate total valuation based on active StockBatch.RemainingQuantity * ImportPrice
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

            // Detailed product stocks
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

            // Active batches with expiring check
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
