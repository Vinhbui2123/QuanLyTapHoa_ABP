(function ($) {
  var _productService = abp.services.app.product,
    l = abp.localization.getSource("InternProject"),
    _$modal = $("#ProductCreateModal"),
    _$form = _$modal.find("form"),
    _$table = $("#ProductsTable");

  // Lấy KPI sản phẩm: tổng sản phẩm, sắp hết hàng, đang active.
  function updateKPICounters() {
    _productService.getDashboardStats({}).done(function (stats) {
      $("#kpi-total-products").text(stats.totalCount.toLocaleString('vi-VN'));
      $("#kpi-low-stock").text(stats.lowStockCount.toLocaleString('vi-VN'));
      $("#kpi-active-products").text(stats.activeCount.toLocaleString('vi-VN'));
    });
  }

  // DataTables server-side: lọc theo danh mục/trạng thái/từ khóa sẽ gọi ProductAppService.GetListAsync.
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
          // Hiển thị ảnh thumbnail nếu có; nếu ảnh lỗi thì chuyển sang placeholder.
          var imgHtml = "";
          if (row.imageUrl) {
            var imgUrl = abp.appPath + row.imageUrl.replace(/^\//, "");
            imgHtml = `<img src="${imgUrl}" class="rounded mr-2 border" style="width: 36px; height: 36px; object-fit: cover;" onerror="this.onerror=null; this.src=''; $(this).hide(); $(this).next().show();" />` + 
                      `<div class="rounded mr-2 border bg-light d-none align-items-center justify-content-center" style="width: 36px; height: 36px; font-size: 14px; color: #94a3b8;"><i class="fas fa-image"></i></div>`;
          } else {
            imgHtml = `<div class="rounded mr-2 border bg-light d-inline-flex align-items-center justify-content-center" style="width: 36px; height: 36px; font-size: 14px; color: #94a3b8;"><i class="fas fa-image"></i></div>`;
          }
          var nameHtml = `<strong>${data}</strong>`;
          if (abp.auth.hasPermission('Pages.Products.Edit')) {
            nameHtml = `<a href="javascript:;" class="product-name-link edit-product" data-product-id="${row.id}" data-bs-toggle="modal" data-bs-target="#ProductEditModal">${nameHtml}</a>`;
          }
          return `<div class="d-flex align-items-center">${imgHtml} ${nameHtml}</div>`;
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
          // StockStatus được tính từ entity Product dựa trên StockQuantity và MinStock.
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
          // Chỉ hiện action nếu user có quyền tương ứng.
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
    // Chặn submit mặc định, chỉ reload bảng theo filter hiện tại.
    e.preventDefault();
    _$productsTable.ajax.reload();
  });

  $("#CategoryFilter, #StatusFilter").on("change", function () {
    // Đổi filter thì reload DataTable.
    _$productsTable.ajax.reload();
  });

  // Validate form tạo mới trước khi gọi ProductAppService.CreateAsync.
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
    
    // Form serialize ra chuỗi; chuyển các trường tiền/số lượng về number trước khi gửi backend.
    // Backend vẫn ép StockQuantity = 0 khi tạo, tồn kho thực tế được tăng qua phiếu nhập.
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
    // Lấy partial view edit từ MVC rồi nhúng vào modal.
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
    // Modal edit phát event này sau khi lưu; trang danh sách nghe event để reload KPI/bảng.
    updateKPICounters();
    _$productsTable.ajax.reload();
  });

  function deleteProduct(productId, productName) {
    // Xác nhận trước khi xóa sản phẩm.
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

  // Upload ảnh sản phẩm bằng AJAX FormData; controller trả về imageUrl để lưu vào Product.
  $("#product-image-file").on("change", function () {
    var files = this.files;
    if (files.length === 0) {
      return;
    }

    var file = files[0];
    var formData = new FormData();
    formData.append("file", file);

    abp.ui.setBusy(_$modal);
    
    $.ajax({
      url: abp.appPath + "Products/UploadImage",
      type: "POST",
      data: formData,
      contentType: false,
      processData: false,
      headers: {
        // Gửi anti-forgery token vì đây là POST ngoài form MVC thông thường.
        "X-XSRF-TOKEN": abp.security.antiForgery.getToken()
      },
      success: function (response) {
        if (response.success) {
          $("#product-image-url").val(response.imageUrl);
          var previewUrl = abp.appPath + response.imageUrl.replace(/^\//, "");
          $("#product-image-preview").attr("src", previewUrl).show();
          $("#product-image-placeholder").hide();
          $("#btn-remove-product-image").show();
        }
      },
      error: function (xhr) {
        var errorMsg = l("UploadFailed");
        if (xhr.status === 400 && xhr.responseText) {
          try {
            var errObj = JSON.parse(xhr.responseText);
            errorMsg = errObj.message || (errObj.error && errObj.error.message) || xhr.responseText;
          } catch(e) {
            errorMsg = xhr.responseText;
          }
        }
        abp.message.error(errorMsg);
        $("#product-image-file").val("");
      },
      complete: function () {
        abp.ui.clearBusy(_$modal);
      }
    });
  });

  // Xóa ảnh khỏi form hiện tại; file cũ sẽ được backend dọn khi update nếu URL thay đổi.
  $("#btn-remove-product-image").on("click", function () {
    $("#product-image-url").val("");
    $("#product-image-preview").attr("src", "").hide();
    $("#product-image-placeholder").show();
    $("#product-image-file").val("");
    $(this).hide();
  });

  _$modal
    .on("shown.bs.modal", () => {
      // Khi modal mở, focus ô đầu tiên cho thao tác nhập nhanh.
      _$modal.find("input:not([type=hidden]):first").focus();
    })
    .on("hidden.bs.modal", () => {
      // Khi đóng modal tạo mới, reset toàn bộ form và preview ảnh.
      _$form.clearForm();
      $("#product-is-active").prop("checked", true);
      $("#product-cost-price").val(0);
      $("#product-sale-price").val(0);
      $("#product-stock-qty").val(0);
      $("#product-min-stock").val(10);
      $("#product-image-preview").attr("src", "").hide();
      $("#product-image-placeholder").show();
      $("#product-image-url").val("");
      $("#product-image-file").val("");
      $("#btn-remove-product-image").hide();
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
