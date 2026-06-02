(function ($) {
  var _customerService = abp.services.app.customer,
    l = abp.localization.getSource("InternProject"),
    _$modal = $("#CustomerCreateModal"),
    _$form = _$modal.find("form"),
    _$table = $("#CustomersTable");

  // Function to load and update overview KPI counters dynamically from backend
  function updateKPICounters() {
    _customerService.getDashboardStats({}).done(function (stats) {
      $("#kpi-total-customers").text(stats.totalCount.toLocaleString('vi-VN'));
      $("#kpi-active-customers").text(stats.activeCount.toLocaleString('vi-VN'));
      $("#kpi-inactive-customers").text(stats.inactiveCount.toLocaleString('vi-VN'));
    });
  }

  var _$customersTable = _$table.DataTable({
    paging: true,
    serverSide: true, // Use server-side processing to leverage existing backend filters
    processing: true,
    listAction: {
      ajaxFunction: _customerService.getList,
      inputFilter: function () {
        return $("#CustomersSearchForm").serializeFormToObject(true);
      },
    },
    responsive: false,
    columnDefs: [
      {
        targets: 0,
        data: "code",
        defaultContent: "",
        render: (data, type, row) => {
          return `<a href="javascript:;" class="customer-code-link edit-customer" data-customer-id="${row.id}" data-bs-toggle="modal" data-bs-target="#CustomerEditModal">${data || "KH---"}</a>`;
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
        data: "address",
        render: (data) => data || "---"
      },
      {
        targets: 4,
        data: "isActive",
        render: (data) => {
          if (data) {
            return `<span class="badge-status badge-status-active"><i class="fas fa-check-circle"></i> ${l("ActiveCustomers")}</span>`;
          } else {
            return `<span class="badge-status badge-status-inactive"><i class="fas fa-ban"></i> ${l("InactiveCustomers")}</span>`;
          }
        }
      },
      {
        targets: 5,
        data: null,
        orderable: false,
        render: (data, type, row) => {
          return [
            `<a href="javascript:;" class="customer-action-detail edit-customer mr-2" data-customer-id="${row.id}" data-bs-toggle="modal" data-bs-target="#CustomerEditModal">${l("Edit")}</a>`,
            `<span class="text-muted">|</span>`,
            `<a href="javascript:;" class="text-danger ml-2 delete-customer" data-customer-id="${row.id}" data-customer-name="${row.name}">${l("Delete")}</a>`
          ].join(" ");
        }
      }
    ],
  });

  // Load KPI cards on start
  updateKPICounters();

  // Prevent form submission which reloads the page
  $("#CustomersSearchForm").on("submit", function (e) {
    e.preventDefault();
    _$customersTable.ajax.reload();
  });

  // Handle status filter selection change
  $("#StatusFilter").on("change", function () {
    _$customersTable.ajax.reload();
  });

  // Form Validation
  _$form.validate({
    rules: {
      Name: {
        required: true,
      },
    },
  });

  // Save Customer Event
  _$form.find(".save-button").on("click", function (e) {
    e.preventDefault();

    if (!_$form.valid()) {
      return;
    }

    var customer = _$form.serializeFormToObject();
    customer.IsActive = $("#customer-is-active").is(":checked");

    abp.ui.setBusy(_$modal);
    _customerService
      .create(customer)
      .done(function () {
        _$modal.modal("hide");
        _$form[0].reset();
        abp.notify.info(l("SavedSuccessfully"));
        updateKPICounters();
        _$customersTable.ajax.reload();
      })
      .always(function () {
        abp.ui.clearBusy(_$modal);
      });
  });

  // Delete Customer click handler
  $(document).on("click", ".delete-customer", function () {
    var customerId = $(this).attr("data-customer-id");
    var customerName = $(this).attr("data-customer-name");

    deleteCustomer(customerId, customerName);
  });

  // Edit Customer click handler (triggers partial view load in Edit Modal)
  $(document).on("click", ".edit-customer", function (e) {
    var customerId = $(this).attr("data-customer-id");

    e.preventDefault();
    abp.ajax({
      url: abp.appPath + "Customers/EditModal?customerId=" + customerId,
      type: "POST",
      dataType: "html",
      success: function (content) {
        $("#CustomerEditModal div.modal-content").html(content);
      },
      error: function (e) {},
    });
  });

  abp.event.on("customer.edited", (data) => {
    updateKPICounters();
    _$customersTable.ajax.reload();
  });

  function deleteCustomer(customerId, customerName) {
    abp.message.confirm(
      abp.utils.formatString(l("AreYouSureWantToDelete"), customerName),
      null,
      (isConfirmed) => {
        if (isConfirmed) {
          _customerService
            .delete({ id: customerId })
            .done(() => {
              abp.notify.info(l("SuccessfullyDeleted"));
              updateKPICounters();
              _$customersTable.ajax.reload();
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
      $("#customer-is-active").prop("checked", true);
    });

  $(".btn-search").on("click", () => {
    _$customersTable.ajax.reload();
  });

  $(".btn-clear").on("click", () => {
    $(".txt-search").val("");
    $("#StatusFilter").val("").trigger("change");
  });

  $(".txt-search").on("keypress", (e) => {
    if (e.which == 13) {
      _$customersTable.ajax.reload();
      return false;
    }
  });
})(jQuery);
