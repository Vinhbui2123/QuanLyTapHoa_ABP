(function ($) {
  var _productService = abp.services.app.product,
    l = abp.localization.getSource("InternProject"),
    _$modal = $("#ProductCreateModal"),
    _$form = _$modal.find("form"),
    _$table = $("#ProductsTable");

  function updateKPICounters() {
    _productService.getDashboardStats({}).done(function (stats) {
      $("#kpi-total-products").text(stats.totalCount.toLocaleString('vi-VN'));
      $("#kpi-low-stock").text(stats.lowStockCount.toLocaleString('vi-VN'));
      $("#kpi-active-products").text(stats.activeCount.toLocaleString('vi-VN'));
    });
  }

  var _$productsTable = _$table.DataTable({
    paging: true,
    serverSide: true,
    processing: true,
    listAction: {
      ajaxFunction: _productService.getList,
      inputFilter: function () {
        return $("#ProductsSearchForm").serializeFormToObject(true);
      },
    },
    responsive: false,
    columnDefs: [
      {
        targets: 0,
        data: "sku",
        render: (data) => data ? `<code>${data}</code>` : `<span class="text-muted">---</span>`
      },
      {
        targets: 1,
        data: "name",
        render: (data, type, row) => {
          if (abp.auth.hasPermission('Pages.Products.Edit')) {
            return `<a href="javascript:;" class="product-name-link edit-product" data-product-id="${row.id}" data-bs-toggle="modal" data-bs-target="#ProductEditModal"><strong>${data}</strong></a>`;
          }
          return `<strong>${data}</strong>`;
        }
      },
      {
        targets: 2,
        data: "categoryName",
        render: (data) => data ? `<span class="badge bg-light text-dark border"><i class="fas fa-tag text-secondary mr-1"></i> ${data}</span>` : `<span class="text-muted">---</span>`
      },
      {
        targets: 3,
        data: "costPrice",
        className: "text-end",
        render: (data) => {
          if (data === null || data === undefined) return "0 đ";
          return `${data.toLocaleString('vi-VN')} đ`;
        }
      },
      {
        targets: 4,
        data: "salePrice",
        className: "text-end",
        render: (data) => {
          if (data === null || data === undefined) return "0 đ";
          return `${data.toLocaleString('vi-VN')} đ`;
        }
      },
      {
        targets: 5,
        data: "stockQuantity",
        className: "text-end",
        render: (data, type, row) => {
          var qty = (data || 0).toLocaleString('vi-VN');
          switch (row.stockStatus) {
            case 2: // OutOfStock
              return `<span class="badge-stock badge-stock-danger"><i class="fas fa-ban mr-1"></i> ${l("OutOfStock")} (≤ ${row.minStock})</span>`;
            case 1: // LowStock
              return `<span class="badge-stock badge-stock-warning"><i class="fas fa-exclamation-triangle mr-1"></i> ${qty} (≤ ${row.minStock})</span>`;
            default:
              return `<span class="badge-stock badge-stock-normal">${qty}</span>`;
          }
        }
      },
      {
        targets: 6,
        data: "unit",
        render: (data) => data || "---"
      },
      {
        targets: 7,
        data: "isActive",
        render: (data) => {
          if (data) {
            return `<span class="badge-status badge-status-active"><i class="fas fa-check-circle"></i> ${l("ActiveProducts")}</span>`;
          } else {
            return `<span class="badge-status badge-status-inactive"><i class="fas fa-ban"></i> ${l("InactiveProducts")}</span>`;
          }
        }
      },
      {
        targets: 8,
        data: null,
        orderable: false,
        render: (data, type, row) => {
          var actions = [];
          if (abp.auth.hasPermission('Pages.Products.Edit')) {
            actions.push(`<a href="javascript:;" class="product-action-detail edit-product mr-2" data-product-id="${row.id}" data-bs-toggle="modal" data-bs-target="#ProductEditModal">${l("Edit")}</a>`);
          }
          if (abp.auth.hasPermission('Pages.Products.Delete')) {
            if (actions.length > 0) {
              actions.push(`<span class="text-muted">|</span>`);
            }
            actions.push(`<a href="javascript:;" class="text-danger ml-2 delete-product" data-product-id="${row.id}" data-product-name="${row.name}">${l("Delete")}</a>`);
          }
          return actions.join(" ");
        }
      }
    ],
  });

  updateKPICounters();

  $("#ProductsSearchForm").on("submit", function (e) {
    e.preventDefault();
    _$productsTable.ajax.reload();
  });

  $("#CategoryFilter, #StatusFilter").on("change", function () {
    _$productsTable.ajax.reload();
  });

  _$form.validate({
    rules: {
      Name: {
        required: true,
      },
    },
  });

  _$form.find(".save-button").on("click", function (e) {
    e.preventDefault();

    if (!_$form.valid()) {
      return;
    }

    var product = _$form.serializeFormToObject();
    product.IsActive = $("#product-is-active").is(":checked");
    
    // Parse numeric fields properly
    product.CostPrice = parseFloat(product.CostPrice) || 0;
    product.SalePrice = parseFloat(product.SalePrice) || 0;
    product.StockQuantity = parseInt(product.StockQuantity) || 0;
    product.MinStock = parseInt(product.MinStock) || 0;

    abp.ui.setBusy(_$modal);
    _productService
      .create(product)
      .done(function () {
        _$modal.modal("hide");
        _$form[0].reset();
        abp.notify.info(l("SavedSuccessfully"));
        updateKPICounters();
        _$productsTable.ajax.reload();
      })
      .always(function () {
        abp.ui.clearBusy(_$modal);
      });
  });

  $(document).on("click", ".delete-product", function () {
    var productId = $(this).attr("data-product-id");
    var productName = $(this).attr("data-product-name");

    deleteProduct(productId, productName);
  });

  $(document).on("click", ".edit-product", function (e) {
    var productId = $(this).attr("data-product-id");

    e.preventDefault();
    abp.ajax({
      url: abp.appPath + "Products/EditModal?productId=" + productId,
      type: "POST",
      dataType: "html",
      success: function (content) {
        $("#ProductEditModal div.modal-content").html(content);
      },
      error: function (e) {},
    });
  });

  abp.event.on("product.edited", (data) => {
    updateKPICounters();
    _$productsTable.ajax.reload();
  });

  function deleteProduct(productId, productName) {
    abp.message.confirm(
      abp.utils.formatString(l("AreYouSureWantToDelete"), productName),
      null,
      (isConfirmed) => {
        if (isConfirmed) {
          _productService
            .delete({ id: productId })
            .done(() => {
              abp.notify.info(l("SuccessfullyDeleted"));
              updateKPICounters();
              _$productsTable.ajax.reload();
            });
        }
      },
    );
  }

  _$modal
    .on("shown.bs.modal", () => {
      _$modal.find("input:not([type=hidden]):first").focus();
    })
    .on("hidden.bs.modal", () => {
      _$form.clearForm();
      $("#product-is-active").prop("checked", true);
      $("#product-cost-price").val(0);
      $("#product-sale-price").val(0);
      $("#product-stock-qty").val(0);
      $("#product-min-stock").val(10);
    });

  $(".btn-search").on("click", () => {
    _$productsTable.ajax.reload();
  });

  $(".btn-clear").on("click", () => {
    $(".txt-search").val("");
    $("#CategoryFilter").val("");
    $("#StatusFilter").val("").trigger("change");
  });

  $(".txt-search").on("keypress", (e) => {
    if (e.which == 13) {
      _$productsTable.ajax.reload();
      return false;
    }
  });
})(jQuery);
