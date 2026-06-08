(function ($) {
  var _purchaseOrderService = abp.services.app.purchaseOrder,
    l = abp.localization.getSource("InternProject"),
    _$modal = $("#PODetailModal"),
    _$table = $("#POTable");

  var _$poTable = _$table.DataTable({
    paging: true,
    serverSide: true,
    processing: true,
    listAction: {
      ajaxFunction: _purchaseOrderService.getList,
      inputFilter: function () {
        return $("#POSearchForm").serializeFormToObject(true);
      },
    },
    responsive: false,
    columnDefs: [
      {
        targets: 0,
        data: "orderNumber",
        render: (data, type, row) => {
          return `<a href="javascript:;" class="po-code-link view-po-detail" data-po-id="${row.id}">${data}</a>`;
        }
      },
      {
        targets: 1,
        data: "supplierName",
        render: (data) => data || "---"
      },
      {
        targets: 2,
        data: "userName",
        render: (data) => data || "---"
      },
      {
        targets: 3,
        data: "totalAmount",
        render: (data) => data != null ? (data).toLocaleString('vi-VN') + " đ" : "0 đ"
      },
      {
        targets: 4,
        data: "status",
        render: (data) => {
          if (data == 2) {
            return `<span class="badge-status badge-status-completed"><i class="fas fa-check-circle"></i> ${l("StatusCompleted")}</span>`;
          } else if (data == 1) {
            return `<span class="badge-status badge-status-pending"><i class="fas fa-clock"></i> ${l("StatusPending")}</span>`;
          } else {
            return `<span class="badge-status badge-status-cancelled"><i class="fas fa-ban"></i> ${l("StatusCancelled")}</span>`;
          }
        }
      },
      {
        targets: 5,
        data: "note",
        render: (data) => data || "---"
      },
      {
        targets: 6,
        data: "creationTime",
        render: (data) => data ? new Date(data).toLocaleString('vi-VN') : "---"
      },
      {
        targets: 7,
        data: null,
        orderable: false,
        render: (data, type, row) => {
          return `<a href="javascript:;" class="po-code-link view-po-detail mr-2" data-po-id="${row.id}"><i class="fas fa-eye"></i> Xem chi tiết</a>`;
        }
      }
    ],
  });

  // Reload table
  $("#SupplierFilter, #StatusFilter").on("change", function () {
    _$poTable.ajax.reload();
  });

  // Realtime search with debounce 350ms
  var _searchTimer = null;
  $(".txt-search").on("input", function () {
    clearTimeout(_searchTimer);
    _searchTimer = setTimeout(function () {
      _$poTable.ajax.reload();
    }, 350);
  });

  // Prevent form submission on enter & reload table
  $("#POSearchForm").on("submit", function (e) {
    e.preventDefault();
    clearTimeout(_searchTimer);
    _$poTable.ajax.reload();
  });

  // Click handler for PO detail
  $(document).on("click", ".view-po-detail", function (e) {
    var orderId = $(this).attr("data-po-id");

    e.preventDefault();
    abp.ajax({
      url: abp.appPath + "PurchaseOrders/DetailModal?orderId=" + orderId,
      type: "POST",
      dataType: "html",
      success: function (content) {
        $("#PODetailModal div.modal-content").html(content);
        $("#PODetailModal").modal("show");
      },
      error: function (e) {},
    });
  });
})(jQuery);
