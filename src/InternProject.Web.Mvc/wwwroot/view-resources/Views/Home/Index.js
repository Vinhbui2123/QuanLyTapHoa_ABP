$(function () {
  "use strict";

  var _reportsService = abp.services.app.reports;
  var l = abp.localization.getSource("InternProject");

  if (!_reportsService) {
    console.warn("abp.services.app.reports is undefined! The user may not have 'Pages.Reports' permission. Dashboard charts will not load.");
    return;
  }

  // Get context for monthly chart
  var salesChartCanvas = $("#salesChart").get(0).getContext("2d");
  var salesChart = null;

  function formatMoney(amount) {
    if (amount === null || amount === undefined) return "0 đ";
    return amount.toLocaleString("vi-VN") + " đ";
  }

  function loadDashboardData() {
    abp.ui.setBusy("section.content");
    _reportsService.getDashboardOverview({})
      .done(function (data) {
        // 1. Render KPI counts
        $("#today-revenue").text(formatMoney(data.todayRevenue));
        $("#today-profit").text(formatMoney(data.todayProfit));
        var todayInvoices = data.todayInvoicesCount || 0;
        var outOfStock = data.outOfStockProductsCount || 0;
        var lowStock = data.lowStockProductsCount || 0;

        $("#today-invoices").text(todayInvoices.toLocaleString("vi-VN"));
        $("#outofstock-count").text(outOfStock.toLocaleString("vi-VN"));
        $("#lowstock-count").text(lowStock.toLocaleString("vi-VN"));

        // 2. Render Recent Invoices
        var tbody = $("#recent-invoices-tbody");
        tbody.empty();
        if (data.recentInvoices.length === 0) {
          tbody.append(`<tr><td colspan="5" class="text-center text-muted p-3">${l("NoInvoicesCreatedYet")}</td></tr>`);
        } else {
          data.recentInvoices.forEach(function (inv) {
            var date = new Date(inv.creationTime).toLocaleString("vi-VN");
            var statusBadge = inv.status === "Completed" 
              ? `<span class="badge-status badge-status-completed"><i class="fas fa-check-circle"></i> ${l("CompletedInvoices")}</span>`
              : `<span class="badge-status badge-status-cancelled"><i class="fas fa-times-circle"></i> ${l("CancelledInvoices")}</span>`;

            tbody.append(
              `<tr>
                <td><strong>${inv.invoiceNumber}</strong></td>
                <td>${inv.customerName || `<span class="text-muted">${l("Guest")}</span>`}</td>
                <td class="font-weight-bold text-primary">${formatMoney(inv.totalAmount)}</td>
                <td>${date}</td>
                <td>${statusBadge}</td>
              </tr>`
            );
          });
        }

        // 3. Render Monthly Chart
        renderMonthlyChart(data.monthlyRevenueData);
      })
      .fail(function (err) {
        abp.notify.error(l("LoadDashboardError"));
        console.error("Dashboard error:", err);
      })
      .always(function () {
        abp.ui.clearBusy("section.content");
      });
  }

  function renderMonthlyChart(monthlyData) {
    var labels = [];
    var revenues = [];
    var profits = [];

    monthlyData.forEach(function (m) {
      labels.push(m.month);
      revenues.push(m.revenue);
      profits.push(m.profit);
    });

    var salesChartData = {
      labels: labels,
      datasets: [
        {
          label: l("Revenue"),
          fill: true,
          backgroundColor: "rgba(79, 70, 229, 0.1)",
          borderColor: "rgba(79, 70, 229, 1)",
          pointBackgroundColor: "rgba(79, 70, 229, 1)",
          pointBorderColor: "#fff",
          pointHoverBackgroundColor: "#fff",
          pointHoverBorderColor: "rgba(79, 70, 229, 1)",
          spanGaps: true,
          data: revenues,
          tension: 0.3
        },
        {
          label: l("Profit"),
          fill: true,
          backgroundColor: "rgba(16, 185, 129, 0.1)",
          borderColor: "rgba(16, 185, 129, 1)",
          pointBackgroundColor: "rgba(16, 185, 129, 1)",
          pointBorderColor: "#fff",
          pointHoverBackgroundColor: "#fff",
          pointHoverBorderColor: "rgba(16, 185, 129, 1)",
          spanGaps: true,
          data: profits,
          tension: 0.3
        },
      ],
    };

    var salesChartOptions = {
      maintainAspectRatio: false,
      responsive: true,
      plugins: {
        legend: {
          display: true,
          position: "top"
        }
      },
      scales: {
        y: {
          beginAtZero: true,
          ticks: {
            callback: function (value) {
              return value.toLocaleString("vi-VN") + " đ";
            }
          }
        }
      }
    };

    if (salesChart) {
      salesChart.destroy();
    }

    salesChart = new Chart(salesChartCanvas, {
      type: "line",
      data: salesChartData,
      options: salesChartOptions,
    });
  }

  // Load data initially
  loadDashboardData();
});
