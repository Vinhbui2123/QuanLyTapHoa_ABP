(function ($) {
    var _invoiceService = abp.services.app.invoice,
        l = abp.localization.getSource("InternProject"),
        _$table = $("#InvoicesTable"),
        _$modal = $("#InvoiceDetailModal");

    function updateKPICounters() {
        _invoiceService.getDashboardStats({}).done(function (stats) {
            $("#kpi-total-revenue").text((stats.totalRevenue || 0).toLocaleString('vi-VN') + " đ");
            $("#kpi-completed-invoices").text(stats.completedInvoiceCount.toLocaleString('vi-VN'));
            $("#kpi-cancelled-invoices").text(stats.cancelledInvoiceCount.toLocaleString('vi-VN'));
        });
    }

    var _$invoicesTable = _$table.DataTable({
        paging: true,
        serverSide: true,
        processing: true,
        listAction: {
            ajaxFunction: _invoiceService.getList,
            inputFilter: function () {
                var filter = $("#InvoicesSearchForm").serializeFormToObject(true);
                // Convert string values to integers if present
                if (filter.PaymentMethod) filter.PaymentMethod = parseInt(filter.PaymentMethod);
                if (filter.Status) filter.Status = parseInt(filter.Status);
                return filter;
            },
        },
        responsive: false,
        columnDefs: [
            {
                targets: 0,
                data: "invoiceNumber",
                render: (data, type, row) => {
                    return `<a href="javascript:;" class="invoice-code-link view-invoice-details" data-invoice-id="${row.id}"><strong>${data}</strong></a>`;
                }
            },
            {
                targets: 1,
                data: "customerName",
                render: (data) => data ? `<strong>${data}</strong>` : `<span class="text-muted">${l("Guest")}</span>`
            },
            {
                targets: 2,
                data: "cashierUserName",
                render: (data) => `<span class="badge bg-light text-dark border"><i class="fas fa-user-circle mr-1"></i> ${data}</span>`
            },
            {
                targets: 3,
                data: "totalAmount",
                className: "text-end",
                render: (data) => {
                    if (data === null || data === undefined) return "0 đ";
                    return `<strong>${data.toLocaleString('vi-VN')} đ</strong>`;
                }
            },
            {
                targets: 4,
                data: "paymentMethod",
                render: (data) => {
                    switch (data) {
                        case 1:
                            return `<span class="badge bg-primary text-white"><i class="fas fa-money-bill-wave mr-1"></i> ${l("CashPayment")}</span>`;
                        case 2:
                            return `<span class="badge bg-info text-dark"><i class="fas fa-university mr-1"></i> ${l("BankTransfer")}</span>`;
                        case 3:
                            return `<span class="badge bg-pink text-white" style="background-color: #d24d80;"><i class="fas fa-mobile-alt mr-1"></i> ${l("MomoWallet")}</span>`;
                        case 4:
                            return `<span class="badge bg-success text-white" style="background-color: #0077c5;"><i class="fas fa-mobile-alt mr-1"></i> ${l("ZaloPayWallet")}</span>`;
                        default:
                            return `<span class="text-muted">---</span>`;
                    }
                }
            },
            {
                targets: 5,
                data: "status",
                render: (data) => {
                    if (data === 1) { // Completed
                        return `<span class="badge-status badge-status-completed"><i class="fas fa-check-circle"></i> ${l("CompletedInvoices")}</span>`;
                    } else { // Cancelled
                        return `<span class="badge-status badge-status-cancelled"><i class="fas fa-times-circle"></i> ${l("CancelledInvoices")}</span>`;
                    }
                }
            },
            {
                targets: 6,
                data: "creationTime",
                render: (data) => {
                    if (!data) return "---";
                    var date = new Date(data);
                    return date.toLocaleString('vi-VN');
                }
            },
            {
                targets: 7,
                data: null,
                orderable: false,
                render: (data, type, row) => {
                    var actions = [];
                    actions.push(`<a href="javascript:;" class="invoice-action-detail view-invoice-details mr-2" data-invoice-id="${row.id}">${l("InvoiceDetails")}</a>`);
                    
                    // Show cancel action if not cancelled and within 24h
                    if (row.status !== 2 && abp.auth.hasPermission('Pages.Invoices.Cancel')) {
                        var creationTime = new Date(row.creationTime);
                        var now = new Date();
                        var diffHours = Math.abs(now - creationTime) / 36e5;
                        if (diffHours <= 24) {
                            actions.push(`<span class="text-muted">|</span>`);
                            actions.push(`<a href="javascript:;" class="text-danger ml-2 cancel-invoice" data-invoice-id="${row.id}" data-invoice-number="${row.invoiceNumber}">${l("CancelInvoice")}</a>`);
                        }
                    }
                    return actions.join(" ");
                }
            }
        ],
    });

    updateKPICounters();

    $("#InvoicesSearchForm").on("submit", function (e) {
        e.preventDefault();
        _$invoicesTable.ajax.reload();
    });

    $("#PaymentMethodFilter, #StatusFilter").on("change", function () {
        _$invoicesTable.ajax.reload();
    });

    // Detail click
    $(document).on("click", ".view-invoice-details", function (e) {
        var invoiceId = $(this).attr("data-invoice-id");
        e.preventDefault();
        abp.ajax({
            url: abp.appPath + "Invoices/DetailModal?invoiceId=" + invoiceId,
            type: "POST",
            dataType: "html",
            success: function (content) {
                _$modal.find(".modal-content").html(content);
                _$modal.modal("show");
            }
        });
    });

    // Cancel click - Single prompt with input (SweetAlert v2 Promise style)
    $(document).on("click", ".cancel-invoice", function (e) {
        e.preventDefault();
        var invoiceId = $(this).attr("data-invoice-id");
        var invoiceNumber = $(this).attr("data-invoice-number");
        
        var title = l("CancelInvoice") + " " + invoiceNumber;
        var text = (l("CancelInvoiceConfirmText") || "Bạn có chắc chắn muốn hủy Hóa đơn {0}?").replace("{0}", invoiceNumber) + " " + (l("CancelInvoiceReasonPrompt") || "Vui lòng nhập lý do hủy:");
        
        swal({
            title: title,
            text: text,
            content: {
                element: "input",
                attributes: {
                    placeholder: "VD: Khách trả hàng, sai sản phẩm, nhập sai thông tin...",
                    type: "text",
                },
            },
            buttons: {
                cancel: { text: l("Cancel") || "Đóng", visible: true, value: null },
                confirm: { text: l("CancelInvoice") || "Xác nhận hủy", closeModal: false }
            },
            dangerMode: true
        }).then(function (inputValue) {
            if (inputValue === null || inputValue === undefined) return;
            
            if (inputValue.trim() === "") {
                swal(l("Error") || "Lỗi", l("CancelInvoiceReasonEmpty") || "Lý do hủy không được để trống!", "error");
                return;
            }
            
            abp.ui.setBusy(_$table);
            _invoiceService.cancel({
                id: invoiceId,
                cancelReason: inputValue
            }).done(function () {
                swal(l("CancelledInvoices") || "Đã hủy!", l("InvoiceCancelledSuccessfully") || "Hóa đơn đã được hủy thành công.", "success");
                updateKPICounters();
                _$invoicesTable.ajax.reload();
            }).fail(function (err) {
                console.error("Cancel failed:", err);
                swal(l("Error") || "Lỗi", l("CancelInvoiceFailed") || "Không thể hủy hóa đơn. Vui lòng kiểm tra lại log.", "error");
            }).always(function () {
                abp.ui.clearBusy(_$table);
            });
        });
    });

    $(".txt-search").on("keyup", function (e) {
        if (e.which == 13) {
            _$invoicesTable.ajax.reload();
        }
    });
})(jQuery);
