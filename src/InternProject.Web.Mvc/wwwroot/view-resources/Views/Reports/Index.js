$(function () {
  "use strict";

  var _reportsService = abp.services.app.reports;
  var l = abp.localization.getSource("InternProject");

  if (!_reportsService) {
    console.warn("abp.services.app.reports is undefined! Please check if your role has 'Pages.Reports' permission.");
  }

  // Giữ reference chart để mỗi lần reload dữ liệu có thể destroy chart cũ trước khi vẽ chart mới.
  var revenueChart = null;
  var topsellingChart = null;

  // Mặc định lọc báo cáo từ đầu tháng tới hôm nay.
  var today = new Date();
  var startOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
  
  $("#revenueStartDate").val(formatDate(startOfMonth));
  $("#revenueEndDate").val(formatDate(today));
  $("#topsellingStartDate").val(formatDate(startOfMonth));
  $("#topsellingEndDate").val(formatDate(today));

  // --- Hàm tiện ích dùng chung cho các tab báo cáo ---
  function formatDate(date) {
    var d = new Date(date),
        month = '' + (d.getMonth() + 1),
        day = '' + d.getDate(),
        year = d.getFullYear();

    if (month.length < 2) month = '0' + month;
    if (day.length < 2) day = '0' + day;

    return [year, month, day].join('-');
  }

  function formatMoney(amount) {
    if (amount === null || amount === undefined) return "0 đ";
    return amount.toLocaleString("vi-VN") + " đ";
  }

  // --- Tab 1: Báo cáo doanh thu & lợi nhuận ---
  $("#revenueFilterForm").on("submit", function (e) {
    e.preventDefault();
    loadRevenueData();
  });

  function loadRevenueData() {
    // Gọi ReportsAppService.GetRevenueReportAsync để lấy KPI, dữ liệu bảng và điểm biểu đồ.
    if (!_reportsService) {
      abp.notify.warn(l("NoReportPermission"));
      return;
    }
    var startDate = $("#revenueStartDate").val();
    var endDate = $("#revenueEndDate").val();
    var groupBy = $("#revenueGroupBy").val();

    if (!startDate || !endDate) return;

    abp.ui.setBusy("#revenue-report");
    _reportsService
      .getRevenueReport({
        startDate: startDate,
        endDate: endDate,
        groupBy: groupBy,
      })
      .done(function (data) {
        // Render KPI tổng doanh thu, giá vốn, lợi nhuận và biên lợi nhuận.
        $("#revenue-total-val").text(formatMoney(data.totalRevenue));
        $("#cost-total-val").text(formatMoney(data.totalCost));
        $("#profit-total-val").text(formatMoney(data.totalProfit));
        $("#margin-total-val").text(data.profitMarginPercent.toFixed(1) + "%");

        // Render bảng chi tiết từng hóa đơn trong khoảng thời gian lọc.
        var tbody = $("#revenueReportTable tbody");
        tbody.empty();
        if (data.invoices.length === 0) {
          tbody.append(
            `<tr><td colspan="8" class="text-center text-muted p-4"><i class="fas fa-folder-open mr-1"></i> ${l("NoDataInThisPeriod")}</td></tr>`
          );
        } else {
          data.invoices.forEach(function (inv) {
            var date = new Date(inv.creationTime).toLocaleString("vi-VN");
            var pmBadge = `<span class="badge badge-secondary">${inv.paymentMethod}</span>`;
            if (inv.paymentMethod === "Cash") {
              pmBadge = `<span class="badge bg-primary text-white"><i class="fas fa-money-bill-wave"></i> ${l("CashPayment")}</span>`;
            } else if (inv.paymentMethod === "Transfer") {
              pmBadge = `<span class="badge bg-info text-dark"><i class="fas fa-university"></i> ${l("BankTransfer")}</span>`;
            } else if (inv.paymentMethod === "Momo") {
              pmBadge = `<span class="badge bg-pink text-white" style="background-color: #d24d80;"><i class="fas fa-mobile-alt"></i> ${l("MomoWallet")}</span>`;
            } else if (inv.paymentMethod === "ZaloPay") {
              pmBadge = `<span class="badge bg-success text-white" style="background-color: #0077c5;"><i class="fas fa-mobile-alt"></i> ${l("ZaloPayWallet")}</span>`;
            }

            tbody.append(
              `<tr>
                <td><strong>${inv.invoiceNumber}</strong></td>
                <td>${date}</td>
                <td>${inv.customerName || `<span class="text-muted">${l("Guest")}</span>`}</td>
                <td><span class="badge bg-light text-dark border"><i class="fas fa-user-circle"></i> ${inv.cashierName}</span></td>
                <td class="text-right font-weight-bold">${formatMoney(inv.totalAmount)}</td>
                <td class="text-right text-muted">${formatMoney(inv.totalCost)}</td>
                <td class="text-right text-success font-weight-bold">${formatMoney(inv.totalProfit)}</td>
                <td>${pmBadge}</td>
              </tr>`
            );
          });
        }

        // Render biểu đồ đường doanh thu/giá vốn/lợi nhuận.
        renderRevenueChart(data.chartPoints);
      })
      .always(function () {
        abp.ui.clearBusy("#revenue-report");
      });
  }

  function renderRevenueChart(points) {
    // Chuyển chartPoints từ backend thành 3 series cho Chart.js.
    var labels = [];
    var revenues = [];
    var costs = [];
    var profits = [];

    points.forEach(function (pt) {
      labels.push(pt.timeLabel);
      revenues.push(pt.revenue);
      costs.push(pt.cost);
      profits.push(pt.profit);
    });

    var ctx = document.getElementById("revenueReportChart").getContext("2d");

    if (revenueChart) {
      // Destroy chart cũ để tránh Chart.js báo lỗi canvas đã được dùng.
      revenueChart.destroy();
    }

    revenueChart = new Chart(ctx, {
      type: "line",
      data: {
        labels: labels,
        datasets: [
          {
            label: l("Revenue"),
            data: revenues,
            borderColor: "#4f46e5",
            backgroundColor: "rgba(79, 70, 229, 0.1)",
            fill: true,
            tension: 0.3,
          },
          {
            label: l("CostPrice"),
            data: costs,
            borderColor: "#f59e0b",
            backgroundColor: "rgba(245, 158, 11, 0.05)",
            fill: true,
            tension: 0.3,
          },
          {
            label: l("Profit"),
            data: profits,
            borderColor: "#10b981",
            backgroundColor: "rgba(16, 185, 129, 0.1)",
            fill: true,
            tension: 0.3,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: "top",
          },
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: {
              callback: function (value) {
                return value.toLocaleString("vi-VN") + " đ";
              },
            },
          },
        },
      },
    });
  }

  // --- Tab 2: Báo cáo tồn kho & hạn sử dụng ---
  $("#inventoryFilterForm").on("submit", function (e) {
    e.preventDefault();
    loadInventoryData();
  });

  function loadInventoryData() {
    // Gọi ReportsAppService.GetInventoryReportAsync để lấy định giá tồn kho và các lô gần/quá hạn.
    if (!_reportsService) {
      abp.notify.warn(l("NoReportPermission"));
      return;
    }
    var nearExpiryDays = $("#nearExpiryDays").val() || 30;

    abp.ui.setBusy("#inventory-report");
    _reportsService
      .getInventoryReport({
        nearExpiryDays: parseInt(nearExpiryDays),
      })
      .done(function (data) {
        // Render KPI tồn kho: giá trị tồn, tổng số lượng, lô gần hạn và lô quá hạn.
        $("#inventory-valuation-val").text(formatMoney(data.totalStockValuation));
        $("#inventory-items-count").text(data.totalItemsInStock.toLocaleString("vi-VN"));
        $("#expiring-batches-count").text(data.expiringBatchesCount);
        $("#expired-batches-count").text(data.expiredBatchesCount);

        // Bảng 1: tồn kho theo sản phẩm, có trạng thái InStock/LowStock/OutOfStock.
        var tbody1 = $("#productInventoryTable tbody");
        tbody1.empty();
        if (data.productStocks.length === 0) {
          tbody1.append(
            `<tr><td colspan="8" class="text-center text-muted p-4"><i class="fas fa-folder-open mr-1"></i> ${l("NoProductFound")}</td></tr>`
          );
        } else {
          data.productStocks.forEach(function (stock) {
            var statusBadge = `<span class="badge-status badge-status-normal"><i class="fas fa-check-circle"></i> ${l("BatchStatusNormal")}</span>`;
            if (stock.stockStatus === "OutOfStock") {
              statusBadge = `<span class="badge-status badge-status-out"><i class="fas fa-times-circle"></i> ${l("OutOfStock")}</span>`;
            } else if (stock.stockStatus === "LowStock") {
              statusBadge = `<span class="badge-status badge-status-low"><i class="fas fa-exclamation-circle"></i> ${l("LowStock")}</span>`;
            }

            tbody1.append(
              `<tr>
                <td><strong>${stock.sku || "---"}</strong></td>
                <td>${stock.productName}</td>
                <td>${stock.categoryName}</td>
                <td class="text-right font-weight-bold">${stock.stockQuantity}</td>
                <td class="text-right text-muted">${formatMoney(stock.costPrice)}</td>
                <td class="text-right text-muted">${formatMoney(stock.salePrice)}</td>
                <td class="text-right font-weight-bold text-primary">${formatMoney(stock.stockValuation)}</td>
                <td>${statusBadge}</td>
              </tr>`
            );
          });
        }

        // Bảng 2: các lô còn tồn nhưng gần hết hạn hoặc đã quá hạn.
        var tbody2 = $("#expiringBatchesTable tbody");
        tbody2.empty();
        if (data.expiringBatches.length === 0) {
          tbody2.append(
            `<tr><td colspan="8" class="text-center text-muted p-4"><i class="fas fa-check-circle text-success mr-1"></i> ${l("NoExpiringOrExpiredBatches")}</td></tr>`
          );
        } else {
          data.expiringBatches.forEach(function (batch) {
            var expDate = batch.expiryDate ? new Date(batch.expiryDate).toLocaleDateString("vi-VN") : "---";
            var dayClass = batch.daysToExpiry < 0 ? "text-danger font-weight-bold" : "text-dark";
            var dayLabel = batch.daysToExpiry < 0 ? l("ExpiredNDays", Math.abs(batch.daysToExpiry)) : l("NDays", batch.daysToExpiry);
            
            var warningBadge = `<span class="badge bg-warning text-dark"><i class="fas fa-hourglass-half"></i> ${l("BatchStatusNearExpiry")}</span>`;
            if (batch.status === "Expired") {
              warningBadge = `<span class="badge bg-danger text-white"><i class="fas fa-exclamation-triangle"></i> ${l("BatchStatusExpired")}</span>`;
            }

            tbody2.append(
              `<tr>
                <td><strong>${batch.batchCode}</strong></td>
                <td>${batch.productName}</td>
                <td>${batch.supplierName}</td>
                <td class="text-right font-weight-bold">${batch.remainingQuantity}</td>
                <td class="text-right text-muted">${formatMoney(batch.importPrice)}</td>
                <td>${expDate}</td>
                <td class="text-right ${dayClass}">${dayLabel}</td>
                <td>${warningBadge}</td>
              </tr>`
            );
          });
        }
      })
      .always(function () {
        abp.ui.clearBusy("#inventory-report");
      });
  }

  // --- Tab 3: Sản phẩm bán chạy ---
  $("#topsellingFilterForm").on("submit", function (e) {
    e.preventDefault();
    loadTopSellingData();
  });

  function loadTopSellingData() {
    // Gọi ReportsAppService.GetTopSellingProductsReportAsync để xếp hạng sản phẩm theo số lượng/doanh thu/lợi nhuận.
    if (!_reportsService) {
      abp.notify.warn(l("NoReportPermission"));
      return;
    }
    var startDate = $("#topsellingStartDate").val();
    var endDate = $("#topsellingEndDate").val();
    var sortBy = $("#topsellingSortBy").val();
    var topN = $("#topsellingTopN").val() || 10;

    abp.ui.setBusy("#topselling-report");
    _reportsService
      .getTopSellingProductsReport({
        startDate: startDate || null,
        endDate: endDate || null,
        topN: parseInt(topN),
        sortBy: sortBy,
      })
      .done(function (data) {
        var tbody = $("#topsellingReportTable tbody");
        tbody.empty();

        if (data.length === 0) {
          tbody.append(
            `<tr><td colspan="7" class="text-center text-muted p-4"><i class="fas fa-folder-open mr-1"></i> ${l("NoData")}</td></tr>`
          );
          if (topsellingChart) {
            topsellingChart.destroy();
          }
          return;
        }

        data.forEach(function (prod, idx) {
          // Badge top 1/2/3 giúp bảng dễ nhìn khi trình bày báo cáo.
          var indexBadge = `<span class="badge bg-light text-dark">${idx + 1}</span>`;
          if (idx === 0) indexBadge = `<span class="badge bg-warning text-white"><i class="fas fa-crown"></i> 1</span>`;
          else if (idx === 1) indexBadge = `<span class="badge bg-secondary text-white">2</span>`;
          else if (idx === 2) indexBadge = `<span class="badge bg-bronze text-white" style="background-color: #cd7f32;">3</span>`;

          tbody.append(
            `<tr>
              <td>${indexBadge}</td>
              <td><strong>${prod.sku || "---"}</strong></td>
              <td>${prod.productName}</td>
              <td>${prod.categoryName}</td>
              <td class="text-right font-weight-bold">${prod.soldQuantity}</td>
              <td class="text-right text-muted">${formatMoney(prod.totalRevenue)}</td>
              <td class="text-right text-success font-weight-bold">${formatMoney(prod.totalProfit)}</td>
            </tr>`
          );
        });

        renderTopSellingChart(data);
      })
      .always(function () {
        abp.ui.clearBusy("#topselling-report");
      });
  }

  function renderTopSellingChart(data) {
    // Biểu đồ doughnut chỉ lấy top 5, phần còn lại gom vào "Other" để chart không quá rối.
    var labels = [];
    var revenues = [];

    data.slice(0, 5).forEach(function (prod) {
      labels.push(prod.productName);
      revenues.push(prod.totalRevenue);
    });

    // Nếu có hơn 5 sản phẩm thì gom phần còn lại vào nhóm Other.
    if (data.length > 5) {
      var otherRevenue = 0;
      data.slice(5).forEach(function (prod) {
        otherRevenue += prod.totalRevenue;
      });
      labels.push(l("Other"));
      revenues.push(otherRevenue);
    }

    var ctx = document.getElementById("topsellingChart").getContext("2d");

    if (topsellingChart) {
      // Destroy chart cũ trước khi vẽ lại.
      topsellingChart.destroy();
    }

    topsellingChart = new Chart(ctx, {
      type: "doughnut",
      data: {
        labels: labels,
        datasets: [
          {
            data: revenues,
            backgroundColor: [
              "#4f46e5",
              "#10b981",
              "#f59e0b",
              "#06b6d4",
              "#ef4444",
              "#9ca3af",
            ],
            hoverOffset: 10,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: "bottom",
          },
          tooltip: {
            callbacks: {
              label: function (context) {
                var val = context.raw || 0;
                return context.label + ": " + val.toLocaleString("vi-VN") + " đ";
              },
            },
          },
        },
      },
    });
  }

    // --- Xuất Excel bằng SheetJS ---
    // Các đoạn dưới đang comment lại; nếu bật lên thì sẽ xuất bảng hiện tại ra file .xlsx.
    // $("#btnExportRevenue").on("click", function () {
    //   var wb = XLSX.utils.table_to_book(document.getElementById("revenueReportTable"), {
    //     sheet: "Báo cáo Doanh thu",
    //   });
    //   XLSX.writeFile(wb, "BaoCaoDoanhThuLoiNhuan_" + formatDate(new Date()) + ".xlsx");
    // });

    // $("#btnExportInventory").on("click", function () {
    //   // Generate multi-sheet excel
    //   var wb = XLSX.utils.book_new();
      
    //   var ws1 = XLSX.utils.table_to_sheet(document.getElementById("productInventoryTable"));
    //   XLSX.utils.book_append_sheet(wb, ws1, "Báo cáo Tồn Kho");
      
    //   var ws2 = XLSX.utils.table_to_sheet(document.getElementById("expiringBatchesTable"));
    //   XLSX.utils.book_append_sheet(wb, ws2, "Lô Cận Hạn & Quá Hạn");
      
    //   XLSX.writeFile(wb, "BaoCaoTonKhoCanHan_" + formatDate(new Date()) + ".xlsx");
    // });

    // $("#btnExportTopSelling").on("click", function () {
    //   var wb = XLSX.utils.table_to_book(document.getElementById("topsellingReportTable"), {
    //     sheet: "Bán Chạy",
    //   });
    //   XLSX.writeFile(wb, "BaoCaoSanPhamBanChay_" + formatDate(new Date()) + ".xlsx");
    // });

    // --- Tải dữ liệu lần đầu khi mở trang ---
    loadRevenueData();

  // --- Tự xử lý chuyển tab để chạy ổn với cả Bootstrap 4/5 ---
  $('#reportTabs .nav-link').on('click', function (e) {
    e.preventDefault();
    var target = $(this).attr('href');

    // Đổi trạng thái active của tab.
    $('#reportTabs .nav-link').removeClass('active');
    $(this).addClass('active');

    // Hiện đúng nội dung tab được chọn.
    $('.tab-content .tab-pane').removeClass('show active');
    $(target).addClass('show active');

    // Mỗi tab gọi endpoint riêng, chỉ load khi người dùng mở tab đó.
    if (target === '#inventory-report') {
      loadInventoryData();
    } else if (target === '#topselling-report') {
      loadTopSellingData();
    } else if (target === '#revenue-report') {
      loadRevenueData();
    }
  });
});
