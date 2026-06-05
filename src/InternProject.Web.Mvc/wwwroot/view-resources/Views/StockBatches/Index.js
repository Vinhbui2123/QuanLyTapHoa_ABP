(function ($) {
  var _stockBatchService = abp.services.app.stockBatch,
    l = abp.localization.getSource("InternProject"),
    _$modal = $("#DisposeBatchModal"),
    _$form = _$modal.find("form"),
    _$table = $("#BatchesTable");

  var _$batchesTable = _$table.DataTable({
    paging: true,
    serverSide: true,
    processing: true,
    listAction: {
      ajaxFunction: _stockBatchService.getList,
      inputFilter: function () {
        return $("#BatchesSearchForm").serializeFormToObject(true);
      },
    },
    responsive: false,
    columnDefs: [
      {
        targets: 0,
        data: "batchCode",
        render: (data) => `<strong>${data}</strong>`
      },
      {
        targets: 1,
        data: "productName",
        render: (data) => data || "---"
      },
      {
        targets: 2,
        data: "supplierName",
        render: (data) => data || "---"
      },
      {
        targets: 3,
        data: "importPrice",
        render: (data) => data != null ? (data).toLocaleString('vi-VN') + " đ" : "0 đ"
      },
      {
        targets: 4,
        data: "initialQuantity",
        render: (data) => data.toLocaleString('vi-VN')
      },
      {
        targets: 5,
        data: "remainingQuantity",
        render: (data) => `<strong>${data.toLocaleString('vi-VN')}</strong>`
      },
      {
        targets: 6,
        data: "expiryDate",
        render: (data) => {
          if (!data) return l("NoExpiryDate");
          var date = new Date(data);
          return date.toLocaleDateString('vi-VN');
        }
      },
      {
        targets: 7,
        data: null,
        orderable: false,
        render: (data, type, row) => {
          if (row.remainingQuantity <= 0) {
            return `<span class="badge-status badge-status-inactive"><i class="fas fa-check-circle"></i> ${l("BatchStatusOutOfStock")}</span>`;
          }
          if (row.expiryDate) {
            var expiry = new Date(row.expiryDate);
            var now = new Date();
            expiry.setHours(0,0,0,0);
            now.setHours(0,0,0,0);
            if (expiry < now) {
              return `<span class="badge-status badge-status-expired"><i class="fas fa-ban"></i> ${l("BatchStatusExpired")}</span>`;
            }
            var diffTime = expiry - now;
            var diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
            if (diffDays <= 7) {
              return `<span class="badge-status badge-status-warning"><i class="fas fa-exclamation-triangle"></i> ${l("BatchStatusNearExpiry")}</span>`;
            }
          }
          return `<span class="badge-status badge-status-active"><i class="fas fa-check-circle"></i> ${l("BatchStatusNormal")}</span>`;
        }
      },
      {
        targets: 8,
        data: null,
        orderable: false,
        render: (data, type, row) => {
          if (row.remainingQuantity > 0) {
            return `<a href="javascript:;" class="btn btn-sm btn-danger dispose-batch-btn" data-batch-id="${row.id}" data-batch-code="${row.batchCode}" data-batch-qty="${row.remainingQuantity}"><i class="fas fa-trash-alt"></i> ${l("Dispose")}</a>`;
          }
          return "---";
        }
      }
    ],
  });

  // Reload on change/keyup
  $("#ProductFilter, #SupplierFilter, #ExpiredFilter").on("change", function () {
    _$batchesTable.ajax.reload();
  });

  $(".txt-search").on("keyup", function (e) {
    if (e.which == 13 || $(this).val() == "") {
      _$batchesTable.ajax.reload();
    }
  });

  // Dispose button click
  $(document).on("click", ".dispose-batch-btn", function () {
    var id = $(this).attr("data-batch-id");
    var code = $(this).attr("data-batch-code");
    var qty = parseInt($(this).attr("data-batch-qty"));

    $("#dispose-batch-id").val(id);
    $("#dispose-batch-code").val(code);
    $("#dispose-batch-current-qty").val(qty);
    $("#dispose-qty").val(qty).attr("max", qty);
    $("#dispose-reason").val("");

    _$modal.modal("show");
  });

  // Submit dispose form
  _$form.on("submit", function (e) {
    e.preventDefault();

    if (!_$form.valid()) {
      return;
    }

    var input = _$form.serializeFormToObject();
    input.Quantity = parseInt(input.Quantity);

    abp.ui.setBusy(_$modal);
    _stockBatchService.disposeBatch(input).done(function () {
      _$modal.modal("hide");
      abp.notify.info(l("SavedSuccessfully"));
      _$batchesTable.ajax.reload();
    }).always(function () {
      abp.ui.clearBusy(_$modal);
    });
  });

  // Form Validation
  _$form.validate();
})(jQuery);
