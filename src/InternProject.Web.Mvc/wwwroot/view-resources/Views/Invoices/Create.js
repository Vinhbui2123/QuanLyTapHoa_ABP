(function ($) {
    var _productService = abp.services.app.product,
        _invoiceService = abp.services.app.invoice,
        l = abp.localization.getSource("InternProject");

    // Local Caches & State
    var productsCache = [];
    var categoriesCache = [];
    var cart = [];
    var activeCategoryId = "";
    var searchKeyword = "";
    var activePaymentMethod = 1; // 1 = Cash, 2 = Transfer, 3 = MoMo

    // DOM Elements
    var _$searchInput = $("#PosSearchInput"),
        _$categoryTabs = $("#PosCategoryTabs"),
        _$productGrid = $("#PosProductGrid"),
        _$cartList = $("#PosCartList"),
        _$customerSelect = $("#CustomerSelect"),
        _$totalDisplay = $("#PosTotalDisplay"),
        _$amountPaid = $("#PosAmountPaid"),
        _$changeDisplay = $("#PosChangeDisplay"),
        _$note = $("#PosNote"),
        _$checkoutBtn = $("#BtnCheckout"),
        _$cashFields = $("#CashPaymentFields"),
        _$validationHint = $("#CheckoutValidationHint");

    // Initialization
    function init() {
        abp.ui.setBusy(_$productGrid);
        
        // Parallel load of Categories and Products
        $.when(
            loadCategories(),
            loadProducts()
        ).done(function () {
            renderCategories();
            renderProductGrid();
        }).always(function () {
            abp.ui.clearBusy(_$productGrid);
        });

        registerEvents();
        _$searchInput.focus();
    }

    // Load Data
    function loadCategories() {
        return _productService.getCategoryLookup().done(function (result) {
            categoriesCache = result.items || [];
        });
    }

    function loadProducts() {
        return _productService.getList({
            isActive: true,
            maxResultCount: 1000 // Get all active products for fast local catalog search
        }).done(function (result) {
            productsCache = result.items || [];
        });
    }

    // Render Category Tabs
    function renderCategories() {
        var html = '<div class="category-tab active" data-category-id="">Tất cả</div>';
        categoriesCache.forEach(function (cat) {
            html += `<div class="category-tab" data-category-id="${cat.id}">${cat.name}</div>`;
        });
        _$categoryTabs.html(html);
    }

    // Render Catalog Product Grid based on filters
    function renderProductGrid() {
        // Filter locally
        var filtered = productsCache.filter(function (p) {
            // Category filter
            if (activeCategoryId && p.categoryId !== activeCategoryId) {
                return false;
            }
            // Keyword filter (searches Name and Sku)
            if (searchKeyword) {
                var kw = searchKeyword.toLowerCase();
                var nameMatch = p.name && p.name.toLowerCase().indexOf(kw) > -1;
                var skuMatch = p.sku && p.sku.toLowerCase().indexOf(kw) > -1;
                return nameMatch || skuMatch;
            }
            return true;
        });

        if (filtered.length === 0) {
            _$productGrid.html(`
                <div class="text-center w-100 py-5 text-muted">
                    <div class="mb-2"><i class="fas fa-search fs-3 text-secondary"></i></div>
                    ${l("NoProductFound")}
                </div>
            `);
            return;
        }

        var html = "";
        filtered.forEach(function (p) {
            // Stock badge status
            var stockBadgeClass = "stock-badge-normal";
            var stockText = (l("StockFormat") || "Kho: {0} {1}").replace("{0}", p.stockQuantity).replace("{1}", p.unit || "cái");
            if (p.stockQuantity <= 0) {
                stockBadgeClass = "stock-badge-out";
                stockText = l("OutOfStock");
            } else if (p.stockQuantity <= p.minStock) {
                stockBadgeClass = "stock-badge-low";
                stockText = (l("LowStockFormat") || "Sắp hết: {0}").replace("{0}", p.stockQuantity);
            }

            var imgHtml = "";
            if (p.imageUrl) {
                imgHtml = `
                    <img src="${p.imageUrl}" alt="${p.name}" loading="lazy" onerror="this.style.display='none'; this.nextElementSibling.style.display='flex';" />
                    <div class="product-card-img-placeholder" style="display: none;"><i class="fas fa-box"></i></div>
                `;
            } else {
                imgHtml = `<div class="product-card-img-placeholder"><i class="fas fa-box"></i></div>`;
            }

            html += `
                <div class="product-card" id="prod-card-${p.id}">
                    <span class="product-card-stock-badge ${stockBadgeClass}">${stockText}</span>
                    <div class="product-card-img-wrapper">
                        ${imgHtml}
                    </div>
                    <div class="product-card-body">
                        <div class="product-card-title" title="${p.name}">${p.name}</div>
                        <div class="product-card-price">${p.salePrice.toLocaleString('vi-VN')} đ</div>
                    </div>
                </div>
            `;
        });

        _$productGrid.html(html);

        // Bind data objects directly to cards
        filtered.forEach(function (p) {
            $(`#prod-card-${p.id}`).data("product-data", p);
        });
    }

    // Event Registrations
    function registerEvents() {
        // Category tab filter click
        $(document).on("click", ".category-tab", function () {
            $(".category-tab").removeClass("active");
            $(this).addClass("active");
            activeCategoryId = $(this).data("category-id");
            renderProductGrid();
        });

        // Autocomplete search input
        _$searchInput.on("input", function () {
            searchKeyword = $(this).val().trim();
            renderProductGrid();
        });

        // Scan barcode search box Enter
        _$searchInput.on("keypress", function (e) {
            if (e.which === 13) {
                e.preventDefault();
                var keyword = $(this).val().trim();
                if (keyword) {
                    var added = addProductBySku(keyword);
                    if (added) {
                        _$searchInput.val("");
                        searchKeyword = "";
                        renderProductGrid();
                    } else {
                        abp.notify.info((l("BarcodeScanNotFound") || 'Không quét được mã sản phẩm khớp với "{0}"').replace("{0}", keyword));
                    }
                }
            }
        });

        // Click card to add product to cart
        $(document).on("click", ".product-card", function () {
            var product = $(this).data("product-data");
            if (product) {
                addToCart(product);
            }
        });

        // Quantity manipulation +/-
        $(document).on("click", ".cart-qty-minus", function (e) {
            e.stopPropagation();
            var id = $(this).data("id");
            var item = findCartItem(id);
            if (item && item.quantity > 1) {
                updateQuantity(id, item.quantity - 1);
            }
        });

        $(document).on("click", ".cart-qty-plus", function (e) {
            e.stopPropagation();
            var id = $(this).data("id");
            var item = findCartItem(id);
            if (item) {
                if (item.quantity >= item.stockQuantity) {
                    abp.notify.warn((l("QuantityExceededStock") || "Sản phẩm '{0}' trong kho chỉ còn tối đa {1}.").replace("{0}", item.name).replace("{1}", item.stockQuantity));
                    return;
                }
                updateQuantity(id, item.quantity + 1);
            }
        });

        $(document).on("change", ".cart-qty-input", function (e) {
            e.stopPropagation();
            var id = $(this).data("id");
            var val = parseInt($(this).val()) || 1;
            var item = findCartItem(id);
            if (item) {
                if (val > item.stockQuantity) {
                    abp.notify.warn((l("QuantityExceededStock") || "Sản phẩm '{0}' trong kho chỉ còn tối đa {1}.").replace("{0}", item.name).replace("{1}", item.stockQuantity));
                    val = item.stockQuantity;
                }
                if (val < 1) val = 1;
                updateQuantity(id, val);
            }
        });

        // Delete item click
        $(document).on("click", ".cart-item-del-btn", function (e) {
            e.stopPropagation();
            var id = $(this).data("id");
            removeFromCart(id);
        });

        // Toggle Payment Method
        $(".payment-method-btn").on("click", function () {
            $(".payment-method-btn").removeClass("active");
            $(this).addClass("active");
            activePaymentMethod = parseInt($(this).data("method"));

            if (activePaymentMethod === 1) {
                _$cashFields.show();
                _$amountPaid.val("").prop("disabled", false);
                calculateChange();
            } else {
                _$cashFields.hide();
                // Transfer or Momo sets cash received equal to total
                var total = getTotalCartAmount();
                _$amountPaid.val(total.toLocaleString("vi-VN")).prop("disabled", true);
                _$changeDisplay.text("0 đ").removeClass("text-danger").addClass("text-primary");
            }
            updateCheckoutButtonState();
        });

        // Paid Cash Input Auto Format
        _$amountPaid.on("input", function () {
            var val = $(this).val().replace(/\D/g, "");
            if (val) {
                $(this).val(parseInt(val).toLocaleString("vi-VN"));
            } else {
                $(this).val("");
            }
            calculateChange();
            updateCheckoutButtonState();
        });

        // Fast cash selection
        $(".cash-option-btn").on("click", function () {
            var valType = $(this).data("val");
            var total = getTotalCartAmount();

            if (valType === "exact") {
                _$amountPaid.val(total.toLocaleString("vi-VN"));
            } else {
                var value = parseInt(valType);
                _$amountPaid.val(value.toLocaleString("vi-VN"));
            }
            calculateChange();
            updateCheckoutButtonState();
        });

        // Checkout Trigger
        _$checkoutBtn.on("click", function () {
            submitCheckout();
        });

        // Hotkeys Configuration
        $(document).on("keydown", function (e) {
            if (e.which === 120) { // F9: Focus search
                e.preventDefault();
                _$searchInput.focus().select();
            }
            else if (e.which === 121) { // F10: Checkout
                e.preventDefault();
                if (!_$checkoutBtn.prop("disabled")) {
                    submitCheckout();
                }
            }
            else if (e.which === 27) { // Esc: Clear Cart
                e.preventDefault();
                if (cart.length > 0) {
                    abp.message.confirm(
                        l("CancelCartConfirm") || "Bạn có chắc chắn muốn xóa toàn bộ sản phẩm khỏi giỏ hàng hiện tại?",
                        l("ClearCart") || "Hủy giỏ hàng",
                        function (isConfirmed) {
                            if (isConfirmed) {
                                cart = [];
                                renderCart();
                                abp.notify.info(l("CartCleared") || "Đã làm trống giỏ hàng.");
                            }
                        }
                    );
                }
            }
        });
    }

    // Business POS Methods
    function addProductBySku(skuKeyword) {
        var p = productsCache.find(x => x.sku && x.sku.toLowerCase() === skuKeyword.toLowerCase());
        if (p) {
            addToCart(p);
            abp.notify.success((l("BarcodeScanSuccess") || "Đã quét được mã sản phẩm: {0}").replace("{0}", p.name));
            return true;
        }
        return false;
    }

    function addToCart(product) {
        if (product.stockQuantity <= 0) {
            abp.notify.error((l("OutOfStock") || "Hết hàng") + ": " + product.name);
            return;
        }

        var existing = findCartItem(product.id);
        if (existing) {
            if (existing.quantity >= product.stockQuantity) {
                abp.notify.warn((l("QuantityExceededStock") || "Sản phẩm '{0}' trong kho chỉ còn tối đa {1}.").replace("{0}", product.name).replace("{1}", product.stockQuantity));
                return;
            }
            existing.quantity += 1;
        } else {
            cart.push({
                productId: product.id,
                name: product.name,
                sku: product.sku,
                salePrice: product.salePrice,
                stockQuantity: product.stockQuantity,
                quantity: 1
            });
        }
        renderCart();
    }

    function updateQuantity(productId, qty) {
        var item = findCartItem(productId);
        if (item) {
            item.quantity = qty;
            renderCart();
        }
    }

    function removeFromCart(productId) {
        cart = cart.filter(x => x.productId !== productId);
        renderCart();
    }

    function findCartItem(productId) {
        return cart.find(x => x.productId === productId);
    }

    function getTotalCartAmount() {
        return cart.reduce((sum, item) => sum + (item.salePrice * item.quantity), 0);
    }

    // Render Side Cart Panel
    function renderCart() {
        if (cart.length === 0) {
            _$cartList.html(`
                <div id="CartEmptyPlaceholder" class="text-center py-5 text-muted my-auto">
                    <div class="mb-3"><i class="fas fa-shopping-basket fs-1 text-light-emphasis"></i></div>
                    ${l("EmptyCartPlaceholder")}
                </div>
            `);
            _$totalDisplay.text("0 đ");
            if (activePaymentMethod !== 1) {
                _$amountPaid.val(0);
            }
            calculateChange();
            updateCheckoutButtonState();
            return;
        }

        var html = "";
        cart.forEach(function (item) {
            var subtotal = item.salePrice * item.quantity;
            html += `
                <div class="pos-cart-item">
                    <div class="pos-cart-item-info">
                        <div class="pos-cart-item-name" title="${item.name}">${item.name}</div>
                        <div class="pos-cart-item-price">${item.salePrice.toLocaleString('vi-VN')} đ</div>
                    </div>
                    <div class="pos-cart-item-actions">
                        <div class="d-flex align-items-center">
                            <button class="cart-qty-btn cart-qty-minus" data-id="${item.productId}" type="button">-</button>
                            <input class="cart-qty-input" data-id="${item.productId}" type="text" value="${item.quantity}" />
                            <button class="cart-qty-btn cart-qty-plus" data-id="${item.productId}" type="button">+</button>
                        </div>
                        <div class="cart-item-total">${subtotal.toLocaleString('vi-VN')} đ</div>
                        <button class="cart-item-del-btn" data-id="${item.productId}" type="button" title="${l("Delete")}">
                            <i class="fas fa-trash-alt"></i>
                        </button>
                    </div>
                </div>
            `;
        });

        _$cartList.html(html);

        var total = getTotalCartAmount();
        _$totalDisplay.text(total.toLocaleString('vi-VN') + " đ");

        if (activePaymentMethod !== 1) {
            _$amountPaid.val(total.toLocaleString('vi-VN'));
        }
        calculateChange();
        updateCheckoutButtonState();
    }

    function calculateChange() {
        var total = getTotalCartAmount();
        var paidStr = _$amountPaid.val().replace(/\D/g, "");
        var paid = parseInt(paidStr) || 0;

        if (activePaymentMethod !== 1) {
            _$changeDisplay.text("0 đ").removeClass("text-danger").addClass("text-primary");
            return;
        }

        if (paid < total) {
            _$changeDisplay.text(l("InsufficientAmount") || "Chưa đủ tiền").removeClass("text-primary").addClass("text-danger");
        } else {
            var change = paid - total;
            _$changeDisplay.text(change.toLocaleString('vi-VN') + " đ").removeClass("text-danger").addClass("text-primary");
        }
    }

    // Checkout Form Validator
    function updateCheckoutButtonState() {
        var isCartEmpty = cart.length === 0;
        var total = getTotalCartAmount();
        var paidStr = _$amountPaid.val().replace(/\D/g, "");
        var paid = parseInt(paidStr) || 0;

        var isPaymentEnough = true;
        if (activePaymentMethod === 1) { // Cash checks if amount paid is sufficient
            isPaymentEnough = paid >= total;
        }

        if (isCartEmpty) {
            _$checkoutBtn.prop("disabled", true);
            _$validationHint.hide();
        } else if (!isPaymentEnough) {
            _$checkoutBtn.prop("disabled", true);
            _$validationHint.text(l("EnterPaidAmountWarning") || "Vui lòng nhập đủ tiền khách đưa").show();
        } else {
            _$checkoutBtn.prop("disabled", false);
            _$validationHint.hide();
        }
    }

    // Submit POS payment request
    function submitCheckout() {
        var total = getTotalCartAmount();
        var paidStr = _$amountPaid.val().replace(/\D/g, "");
        var paid = parseInt(paidStr) || 0;

        if (cart.length === 0) {
            abp.message.error(l("EmptyCartPlaceholder") || "Vui lòng thêm sản phẩm vào giỏ hàng trước!");
            return;
        }

        if (activePaymentMethod === 1 && paid < total) {
            abp.message.error(l("PaidAmountInvalid") || "Số tiền khách đưa không đủ thanh toán!");
            return;
        }

        if (activePaymentMethod !== 1) {
            paid = total; // Automatically set paid amount for transfer/e-wallets
        }

        // 1. Immediately disable checkout button to prevent double-click
        _$checkoutBtn.prop("disabled", true).html(`<i class="fas fa-spinner fa-spin mr-2"></i> ${l("PleaseWait") || "Đang xử lý..."}`);

        var customerId = _$customerSelect.val() || null;
        var note = _$note.val().trim() || null;

        var payload = {
            customerId: customerId,
            amountPaid: paid,
            paymentMethod: activePaymentMethod,
            note: note,
            invoiceItems: cart.map(x => ({
                productId: x.productId,
                quantity: x.quantity
            }))
        };

        _invoiceService.create(payload)
            .done(function (result) {
                // 2. Success toast feedback
                var successDetail = (l("CheckoutSuccessDetail") || "Hóa đơn {0} đã được tạo thành công. Tiền thừa: {1}").replace("{0}", result.invoiceNumber).replace("{1}", result.changeAmount.toLocaleString('vi-VN') + " đ");
                abp.notify.success(
                    successDetail,
                    l("CheckoutSuccess") || "Thanh toán thành công"
                );

                // 3. Reset form and cart
                cart = [];
                renderCart();
                _$customerSelect.val("");
                _$amountPaid.val("");
                _$note.val("");
                // Reset payment selection back to cash (default)
                $(".payment-method-btn").removeClass("active");
                $('[data-method="1"]').addClass("active");
                activePaymentMethod = 1;
                _$cashFields.show();
                
                // Refresh local product stock quantities from DB so grid stays correct
                loadProducts().done(function () {
                    renderProductGrid();
                });

                // 4. Prompt print modal
                abp.message.confirm(
                    l("PrintConfirm") || "Đã hoàn tất thanh toán. Bạn có muốn in hóa đơn không?",
                    l("CheckoutSuccess") || "Hóa đơn đã tạo",
                    function (confirmed) {
                        if (confirmed) {
                            // Fetch invoice detail modal HTML and trigger printing cleanly in an iframe/window
                            abp.ajax({
                                url: abp.appPath + "Invoices/DetailModal?invoiceId=" + result.id,
                                type: "POST",
                                dataType: "html",
                                success: function (content) {
                                    var printWindow = window.open('', '_blank');
                                    printWindow.document.write('<html><head><title>In hóa đơn</title>');
                                    printWindow.document.write('<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css">');
                                    printWindow.document.write('</head><body><div class="container p-4">');
                                    printWindow.document.write(content);
                                    printWindow.document.write('</div></body></html>');
                                    printWindow.document.close();
                                    setTimeout(function () {
                                        printWindow.print();
                                        printWindow.close();
                                    }, 800);
                                }
                            });
                        }
                    }
                );

                // 5. Refocus search box for next transaction
                _$searchInput.focus();
            })
            .fail(function (err) {
                // Fail automatically shown by sweetalert from ABP, but we still need to enable checkout button
            })
            .always(function () {
                // 6. Enable checkout button back in finally block
                _$checkoutBtn.prop("disabled", false).html('<i class="fas fa-money-bill-wave"></i> ' + l("Checkout"));
                updateCheckoutButtonState();
            });
    }

    $(document).ready(init);

})(jQuery);
