(function ($) {
  var _purchaseOrderService = abp.services.app.purchaseOrder,
    l = abp.localization.getSource("InternProject"),
    _$form = $("#CreatePurchaseOrderForm"),
    _$tableBody = $("#POItemsTable tbody"),
    _$productTemplate = $("#ProductTemplate");

  var rowIdCounter = 0;

  // Add new row function
  function addRow() {
    rowIdCounter++;
    var rowId = "item-row-" + rowIdCounter;
    
    // Copy options from product template
    var selectHtml = `<select name="items[${rowId}][ProductId]" class="form-select product-select" required>` + _$productTemplate.html() + "</select>";

    var html = `
      <tr id="${rowId}">
        <td>${selectHtml}</td>
        <td>
          <input type="number" name="items[${rowId}][Quantity]" class="form-control qty-input" min="1" value="1" required />
        </td>
        <td>
          <div class="input-group">
            <input type="number" name="items[${rowId}][UnitPrice]" class="form-control price-input" min="0" value="0" required />
            <span class="input-group-text">₫</span>
          </div>
        </td>
        <td>
          <input type="text" name="items[${rowId}][BatchId]" class="form-control" placeholder="Mã lô" />
        </td>
        <td>
          <input type="date" name="items[${rowId}][ExpiryDate]" class="form-control" />
        </td>
        <td class="text-center">
          <button type="button" class="btn btn-sm btn-danger btn-delete-row"><i class="fas fa-trash"></i></button>
        </td>
      </tr>
    `;

    _$tableBody.append(html);
    calculateTotalAmount();
  }

  // Calculate total amount
  function calculateTotalAmount() {
    var total = 0;
    _$tableBody.find("tr").each(function () {
      var qty = parseFloat($(this).find(".qty-input").val()) || 0;
      var price = parseFloat($(this).find(".price-input").val()) || 0;
      total += qty * price;
    });

    $("#po-total-amount").text(total.toLocaleString('vi-VN') + " đ");
  }

  // Initialize with one row
  addRow();

  // Add Item Row Event
  $("#BtnAddItem").on("click", function () {
    addRow();
  });

  // Product Selection Change -> Auto-fill default cost price
  $(document).on("change", ".product-select", function () {
    var selectedOption = $(this).find("option:selected");
    var defaultPrice = parseFloat(selectedOption.attr("data-price")) || 0;
    $(this).closest("tr").find(".price-input").val(defaultPrice);
    calculateTotalAmount();
  });

  // Inputs Change -> Recalculate total amount
  $(document).on("change keyup", ".qty-input, .price-input", function () {
    calculateTotalAmount();
  });

  // Delete Row Event
  $(document).on("click", ".btn-delete-row", function () {
    if (_$tableBody.find("tr").length <= 1) {
      abp.message.warn("Phiếu nhập phải có ít nhất 1 sản phẩm.");
      return;
    }
    $(this).closest("tr").remove();
    calculateTotalAmount();
  });

  // Submit form
  _$form.on("submit", function (e) {
    e.preventDefault();

    if (!_$form.valid()) {
      return;
    }

    var supplierId = $("#SupplierId").val();
    if (!supplierId) {
      abp.message.warn("Vui lòng chọn nhà cung cấp.");
      return;
    }

    var items = [];
    var hasError = false;

    _$tableBody.find("tr").each(function () {
      var productId = $(this).find(".product-select").val();
      var quantity = parseInt($(this).find(".qty-input").val()) || 0;
      var unitPrice = parseFloat($(this).find(".price-input").val()) || 0;
      var batchId = $(this).find("input[name*='[BatchId]']").val();
      var expiryDateStr = $(this).find("input[name*='[ExpiryDate]']").val();

      if (!productId) {
        hasError = true;
        abp.message.warn("Vui lòng chọn sản phẩm đầy đủ ở các dòng.");
        return false;
      }

      if (quantity <= 0) {
        hasError = true;
        abp.message.warn("Số lượng phải lớn hơn 0.");
        return false;
      }

      items.push({
        productId: productId,
        quantity: quantity,
        unitPrice: unitPrice,
        batchId: batchId || null,
        expiryDate: expiryDateStr ? new Date(expiryDateStr).toISOString() : null
      });
    });

    if (hasError) {
      return;
    }

    var order = {
      supplierId: supplierId,
      note: $("#Note").val(),
      purchaseOrderItems: items
    };

    abp.ui.setBusy(_$form);
    _purchaseOrderService
      .create(order)
      .done(function () {
        abp.notify.info(l("SavedSuccessfully"));
        setTimeout(function () {
          window.location.href = abp.appPath + "PurchaseOrders";
        }, 1000);
      })
      .always(function () {
        abp.ui.clearBusy(_$form);
      });
  });

  _$form.validate();
})(jQuery);
