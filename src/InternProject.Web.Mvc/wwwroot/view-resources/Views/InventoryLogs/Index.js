(function ($) {
    var _service = abp.services.app.inventoryLog,
        l = abp.localization.getSource("InternProject"),
        _$table = $("#InventoryLogsTable");

    function typeLabel(type) {
        var keys = { 1: "InventoryLogImport", 2: "InventoryLogExport", 3: "InventoryLogDispose", 4: "InventoryLogAdjust" };
        return l(keys[type] || "InventoryLogType");
    }

    var dataTable = _$table.DataTable({
        paging: true,
        serverSide: true,
        processing: true,
        responsive: true,
        listAction: {
            ajaxFunction: _service.getList,
            inputFilter: function () {
                return $("#InventoryLogsSearchForm").serializeFormToObject(true);
            }
        },
        columnDefs: [
            { targets: 0, data: "creationTime", render: data => data ? new Date(data).toLocaleString() : "" },
            { targets: 1, data: "productName", render: (data, type, row) => `${data || ""}${row.productSku ? ` (${row.productSku})` : ""}` },
            { targets: 2, data: "type", render: data => typeLabel(data) },
            { targets: 3, data: "quantity", render: data => Number(data || 0).toLocaleString() },
            { targets: 4, data: "remainingQuantity", render: data => Number(data || 0).toLocaleString() },
            { targets: 5, data: "batchCode", render: data => data || "—" },
            { targets: 6, data: "userName", render: data => data || "—" },
            { targets: 7, data: null, render: (data, type, row) => row.referenceType ? `${row.referenceType}${row.referenceId ? ` (${row.referenceId})` : ""}` : "—" },
            { targets: 8, data: "note", render: data => data || "—" }
        ]
    });

    $("#InventoryLogsSearchForm input, #InventoryLogsSearchForm select").on("change input", function () {
        dataTable.ajax.reload();
    });
})(jQuery);
