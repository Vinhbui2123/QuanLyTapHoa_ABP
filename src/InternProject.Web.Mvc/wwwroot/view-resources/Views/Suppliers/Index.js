(function ($) {
  var _supplierService = abp.services.app.supplier,
    l = abp.localization.getSource("InternProject"),
    _$modal = $("#SupplierCreateModal"),
    _$form = _$modal.find("form"),
    _$table = $("#SuppliersTable");

  // Function to load and update overview KPI counters dynamically from backend
  function updateKPICounters() {
    _supplierService.getDashboardStats({}).done(function (stats) {
      $("#kpi-total-suppliers").text(stats.totalCount.toLocaleString('vi-VN'));
      $("#kpi-active-suppliers").text(stats.activeCount.toLocaleString('vi-VN'));
      $("#kpi-inactive-suppliers").text(stats.inactiveCount.toLocaleString('vi-VN'));
    });
  }

  var _$suppliersTable = _$table.DataTable({
    paging: true,
    serverSide: true, // Use server-side processing to leverage existing backend filters
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

  // Load KPI cards on start
  updateKPICounters();

  // Prevent form submission which reloads the page
  $("#SuppliersSearchForm").on("submit", function (e) {
    e.preventDefault();
    _$suppliersTable.ajax.reload();
  });

  // Handle status filter selection change
  $("#StatusFilter").on("change", function () {
    _$suppliersTable.ajax.reload();
  });

  // Form Validation
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

  // Save Supplier Event
  _$form.find(".save-button").on("click", function (e) {
    e.preventDefault();

    if (!_$form.valid()) {
      return;
    }

    var supplier = _$form.serializeFormToObject();
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

  // Delete Supplier click handler
  $(document).on("click", ".delete-supplier", function () {
    var supplierId = $(this).attr("data-supplier-id");
    var supplierName = $(this).attr("data-supplier-name");

    deleteSupplier(supplierId, supplierName);
  });

  // Edit Supplier click handler (triggers partial view load in Edit Modal)
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
    updateKPICounters();
    _$suppliersTable.ajax.reload();
  });

  function deleteSupplier(supplierId, supplierName) {
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
      _$modal.find("input:not([type=hidden]):first").focus();
    })
    .on("hidden.bs.modal", () => {
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
