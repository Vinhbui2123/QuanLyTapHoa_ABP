(function ($) {
  var _supplierService = abp.services.app.supplier,
    l = abp.localization.getSource("InternProject"),
    _$modal = $("#SupplierCreateModal"),
    _$form = _$modal.find("form"),
    _$table = $("#SuppliersTable");

  // Lấy KPI nhà cung cấp từ backend để các thẻ tổng quan luôn khớp dữ liệu mới nhất.
  function updateKPICounters() {
    _supplierService.getDashboardStats({}).done(function (stats) {
      $("#kpi-total-suppliers").text(stats.totalCount.toLocaleString('vi-VN'));
      $("#kpi-active-suppliers").text(stats.activeCount.toLocaleString('vi-VN'));
      $("#kpi-inactive-suppliers").text(stats.inactiveCount.toLocaleString('vi-VN'));
    });
  }

  // DataTables server-side: lọc, phân trang, sắp xếp đều gọi SupplierAppService.GetListAsync.
  var _$suppliersTable = _$table.DataTable({
    paging: true,
    serverSide: true,
    processing: true,
    listAction: {
      ajaxFunction: _supplierService.getList,
      inputFilter: function () {
        return $("#SuppliersSearchForm").serializeFormToObject(true);
      },
    },
    responsive: false,
    columnDefs: [
      {
        targets: 0,
        data: "code",
        defaultContent: "",
        render: (data, type, row) => {
          return `<a href="javascript:;" class="supplier-code-link edit-supplier" data-supplier-id="${row.id}" data-bs-toggle="modal" data-bs-target="#SupplierEditModal">${data || "NCC---"}</a>`;
        }
      },
      {
        targets: 1,
        data: "name",
        render: (data) => `<strong>${data}</strong>`
      },
      {
        targets: 2,
        data: "phone",
        render: (data) => data || "---"
      },
      {
        targets: 3,
        data: "contactPerson",
        render: (data) => data || "---"
      },
      {
        targets: 4,
        data: "email",
        render: (data) => data || "---"
      },
      {
        targets: 5,
        data: "address",
        render: (data) => data || "---"
      },
      {
        targets: 6,
        data: "isActive",
        render: (data) => {
          if (data) {
            return `<span class="badge-status badge-status-active"><i class="fas fa-check-circle"></i> ${l("ActiveSuppliers")}</span>`;
          } else {
            return `<span class="badge-status badge-status-inactive"><i class="fas fa-ban"></i> ${l("InactiveSuppliers")}</span>`;
          }
        }
      },
      {
        targets: 7,
        data: null,
        orderable: false,
        render: (data, type, row) => {
          return [
            `<a href="javascript:;" class="supplier-action-detail edit-supplier mr-2" data-supplier-id="${row.id}" data-bs-toggle="modal" data-bs-target="#SupplierEditModal">${l("Edit")}</a>`,
            `<span class="text-muted">|</span>`,
            `<a href="javascript:;" class="text-danger ml-2 delete-supplier" data-supplier-id="${row.id}" data-supplier-name="${row.name}">${l("Delete")}</a>`
          ].join(" ");
        }
      }
    ],
  });

  // Tải KPI khi mở trang.
  updateKPICounters();

  // Chặn submit form mặc định để trang không reload; chỉ reload lại DataTable.
  $("#SuppliersSearchForm").on("submit", function (e) {
    e.preventDefault();
    _$suppliersTable.ajax.reload();
  });

  // Đổi trạng thái active/inactive thì tải lại danh sách.
  $("#StatusFilter").on("change", function () {
    _$suppliersTable.ajax.reload();
  });

  // Validate form tạo mới trước khi gọi SupplierAppService.CreateAsync.
  _$form.validate({
    rules: {
      Code: {
        required: true,
      },
      Name: {
        required: true,
      },
    },
  });

  // Lưu nhà cung cấp mới từ modal tạo.
  _$form.find(".save-button").on("click", function (e) {
    e.preventDefault();

    if (!_$form.valid()) {
      return;
    }

    var supplier = _$form.serializeFormToObject();
    // Checkbox không tự serialize đúng kiểu bool nên đọc thủ công.
    supplier.IsActive = $("#supplier-is-active").is(":checked");

    abp.ui.setBusy(_$modal);
    _supplierService
      .create(supplier)
      .done(function () {
        _$modal.modal("hide");
        _$form[0].reset();
        abp.notify.info(l("SavedSuccessfully"));
        updateKPICounters();
        _$suppliersTable.ajax.reload();
      })
      .always(function () {
        abp.ui.clearBusy(_$modal);
      });
  });

  // Click xóa: lấy id/tên từ data attribute rồi gọi hàm xác nhận.
  $(document).on("click", ".delete-supplier", function () {
    var supplierId = $(this).attr("data-supplier-id");
    var supplierName = $(this).attr("data-supplier-name");

    deleteSupplier(supplierId, supplierName);
  });

  // Click sửa: gọi MVC action trả về partial view và nhúng vào modal edit.
  $(document).on("click", ".edit-supplier", function (e) {
    var supplierId = $(this).attr("data-supplier-id");

    e.preventDefault();
    abp.ajax({
      url: abp.appPath + "Suppliers/EditModal?supplierId=" + supplierId,
      type: "POST",
      dataType: "html",
      success: function (content) {
        $("#SupplierEditModal div.modal-content").html(content);
      },
      error: function (e) {},
    });
  });

  abp.event.on("supplier.edited", (data) => {
    // Modal edit phát event này sau khi lưu; trang danh sách nghe event để reload KPI/bảng.
    updateKPICounters();
    _$suppliersTable.ajax.reload();
  });

  function deleteSupplier(supplierId, supplierName) {
    // Xác nhận trước khi xóa để tránh thao tác nhầm.
    abp.message.confirm(
      abp.utils.formatString(l("AreYouSureWantToDelete"), supplierName),
      null,
      (isConfirmed) => {
        if (isConfirmed) {
          _supplierService
            .delete({ id: supplierId })
            .done(() => {
              abp.notify.info(l("SuccessfullyDeleted"));
              updateKPICounters();
              _$suppliersTable.ajax.reload();
            });
        }
      },
    );
  }

  _$modal
    .on("shown.bs.modal", () => {
      // Khi modal mở, focus ô đầu tiên cho thao tác nhập liệu nhanh.
      _$modal.find("input:not([type=hidden]):first").focus();
    })
    .on("hidden.bs.modal", () => {
      // Khi đóng modal tạo mới, reset form về trạng thái mặc định.
      _$form.clearForm();
      $("#supplier-is-active").prop("checked", true);
    });

  $(".btn-search").on("click", () => {
    _$suppliersTable.ajax.reload();
  });

  $(".btn-clear").on("click", () => {
    $(".txt-search").val("");
    $("#StatusFilter").val("").trigger("change");
  });

  $(".txt-search").on("keypress", (e) => {
    if (e.which == 13) {
      _$suppliersTable.ajax.reload();
      return false;
    }
  });
})(jQuery);
