(function ($) {
    var _productService = abp.services.app.product,
        _invoiceService = abp.services.app.invoice,
        _customerService = abp.services.app.customer,
        l = abp.localization.getSource("InternProject");

    // Cache và state của màn hình POS:
    // productsCache/categoriesCache giúp lọc nhanh trên trình duyệt, cart là giỏ hàng hiện tại.
    var productsCache = [];
    var categoriesCache = [];
    var cart = [];
    var activeCategoryId = "";
    var searchKeyword = "";
    var activePaymentMethod = 1; // 1 = Cash, 2 = Transfer, 3 = MoMo

    // Gom các DOM element hay dùng để tránh query jQuery lặp lại nhiều lần.
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
        _$validationHint = $("#CheckoutValidationHint"),
        _$quickCustomerModal = $("#QuickAddCustomerModal"),
        _$quickCustomerForm = $("#QuickAddCustomerForm");

    // Khởi tạo màn hình POS: tải dữ liệu, render danh mục/sản phẩm và đăng ký sự kiện.
    function init() {
        abp.ui.setBusy(_$productGrid);
        
        // Tải danh mục và sản phẩm song song để màn hình mở nhanh hơn.
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

    // Các hàm load dữ liệu gọi tới Application Service proxy do ABP sinh ra.
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

    // Render tab danh mục; khi click tab thì chỉ lọc lại cache phía client.
    function renderCategories() {
        var html = '<div class="category-tab active" data-category-id="">Tất cả</div>';
        categoriesCache.forEach(function (cat) {
            html += `<div class="category-tab" data-category-id="${cat.id}">${cat.name}</div>`;
        });
        _$categoryTabs.html(html);
    }

    // Render lưới sản phẩm theo danh mục/từ khóa đang chọn.
    function renderProductGrid() {
        // Lọc tại client từ productsCache, không gọi server lại mỗi lần gõ.
        var filtered = productsCache.filter(function (p) {
            // Lọc theo danh mục.
            if (activeCategoryId && p.categoryId !== activeCategoryId) {
                return false;
            }
            // Lọc theo tên hoặc SKU.
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
            // Badge tồn kho giúp thu ngân biết sản phẩm còn hàng/sắp hết/hết hàng.
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

        // Gắn object sản phẩm vào card để click thêm giỏ mà không cần query lại.
        filtered.forEach(function (p) {
            $(`#prod-card-${p.id}`).data("product-data", p);
        });
    }

    // Đăng ký toàn bộ sự kiện thao tác trên màn hình POS.
    function registerEvents() {
        $("#BtnQuickAddCustomer").on("click", function () {
            _$quickCustomerForm[0].reset();
            _$quickCustomerModal.modal("show");
        });

        _$quickCustomerForm.on("submit", function (e) {
            e.preventDefault();
            if (!_$quickCustomerForm.valid()) {
                return;
            }

            abp.ui.setBusy(_$quickCustomerModal);
            _customerService.create({
                name: $("#QuickCustomerName").val().trim(),
                phone: $("#QuickCustomerPhone").val().trim() || null,
                isActive: true
            }).done(function (result) {
                var customer = result || {};
                // CreateAsync currently returns no DTO, so reload the lookup and select by name/phone.
                var name = $("#QuickCustomerName").val().trim();
                var phone = $("#QuickCustomerPhone").val().trim();
                _customerService.getList({ maxResultCount: 1000, isActive: true }).done(function (list) {
                    _$customerSelect.find("option:not(:first)").remove();
                    (list.items || []).forEach(function (item) {
                        _$customerSelect.append($("<option>", { value: item.id, text: item.name }));
                    });
                    var selected = (list.items || []).find(function (item) {
                        return item.name === name && (!phone || item.phone === phone);
                    });
                    if (selected) {
                        _$customerSelect.val(selected.id);
                    }
                    _$quickCustomerModal.modal("hide");
                    abp.notify.success(l("SavedSuccessfully"));
                });
            }).always(function () {
                abp.ui.clearBusy(_$quickCustomerModal);
            });
        });

        // Chọn danh mục để lọc sản phẩm.
        $(document).on("click", ".category-tab", function () {
            $(".category-tab").removeClass("active");
            $(this).addClass("active");
            activeCategoryId = $(this).data("category-id");
            renderProductGrid();
        });

        // Gõ từ khóa thì render lại catalog theo cache hiện có.
        _$searchInput.on("input", function () {
            searchKeyword = $(this).val().trim();
            renderProductGrid();
        });

        // Máy quét barcode thường nhập SKU rồi Enter; đoạn này thử thêm sản phẩm trực tiếp vào giỏ.
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

        // Click card sản phẩm để thêm vào giỏ.
        $(document).on("click", ".product-card", function () {
            var product = $(this).data("product-data");
            if (product) {
                addToCart(product);
            }
        });

        // Tăng/giảm số lượng trong giỏ, luôn chặn vượt quá tồn kho đang cache.
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

        // Xóa một dòng sản phẩm khỏi giỏ.
        $(document).on("click", ".cart-item-del-btn", function (e) {
            e.stopPropagation();
            var id = $(this).data("id");
            removeFromCart(id);
        });

        // Đổi phương thức thanh toán: tiền mặt cần nhập tiền khách đưa, chuyển khoản/ví thì mặc định đủ tiền.
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
                // Chuyển khoản/ví điện tử xem như đã nhận đủ đúng tổng tiền.
                var total = getTotalCartAmount();
                _$amountPaid.val(total.toLocaleString("vi-VN")).prop("disabled", true);
                _$changeDisplay.text("0 đ").removeClass("text-danger").addClass("text-primary");
            }
            updateCheckoutButtonState();
        });

        // Format tiền khách đưa theo vi-VN khi nhập, rồi tính lại tiền thối.
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

        // Các nút chọn nhanh mệnh giá tiền mặt.
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

        // Bấm thanh toán.
        _$checkoutBtn.on("click", function () {
            submitCheckout();
        });

        // Phím tắt cho thao tác bán hàng nhanh: F9 tìm kiếm, F10 thanh toán, Esc xóa giỏ.
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

    // Các hàm nghiệp vụ phía client của POS.
    // Backend vẫn kiểm tra lại tồn kho/thanh toán khi tạo hóa đơn, nên JS chỉ là lớp hỗ trợ thao tác nhanh.
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
        // Client chặn nhanh sản phẩm hết hàng; backend vẫn là lớp kiểm tra cuối cùng.
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
        // Tổng tiền giỏ hàng = giá bán hiện tại * số lượng từng dòng.
        return cart.reduce((sum, item) => sum + (item.salePrice * item.quantity), 0);
    }

    // Render panel giỏ hàng bên phải và cập nhật tổng tiền/trạng thái nút thanh toán.
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
        // Chỉ tiền mặt mới cần tính tiền thối; các phương thức khác luôn hiển thị 0 đồng.
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

    // Bật/tắt nút thanh toán dựa vào giỏ hàng và số tiền khách đưa.
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

    // Gửi yêu cầu tạo hóa đơn xuống InvoiceAppService.CreateAsync.
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

        // Khóa nút ngay khi gửi để tránh double-click tạo trùng hóa đơn.
        _$checkoutBtn.prop("disabled", true).html(`<i class="fas fa-spinner fa-spin mr-2"></i> ${l("PleaseWait") || "Đang xử lý..."}`);

        var customerId = _$customerSelect.val() || null;
        var note = _$note.val().trim() || null;

        // Payload chỉ gửi thông tin cần thiết; backend tự lấy giá bán, tồn kho và thu ngân hiện tại.
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
                // Thành công nghĩa là backend đã tạo hóa đơn, trừ kho và trả về mã hóa đơn/tiền thừa.
                var successDetail = (l("CheckoutSuccessDetail") || "Hóa đơn {0} đã được tạo thành công. Tiền thừa: {1}").replace("{0}", result.invoiceNumber).replace("{1}", result.changeAmount.toLocaleString('vi-VN') + " đ");
                abp.notify.success(
                    successDetail,
                    l("CheckoutSuccess") || "Thanh toán thành công"
                );

                // Reset giỏ hàng/form để chuẩn bị giao dịch tiếp theo.
                cart = [];
                renderCart();
                _$customerSelect.val("");
                _$amountPaid.val("");
                _$note.val("");
                // Đưa phương thức thanh toán về tiền mặt mặc định.
                $(".payment-method-btn").removeClass("active");
                $('[data-method="1"]').addClass("active");
                activePaymentMethod = 1;
                _$cashFields.show();
                
                // Tải lại sản phẩm để số tồn trên catalog khớp với database sau khi trừ kho.
                loadProducts().done(function () {
                    renderProductGrid();
                });

                // Hỏi in hóa đơn; lấy lại HTML chi tiết hóa đơn từ MVC để in.
                abp.message.confirm(
                    l("PrintConfirm") || "Đã hoàn tất thanh toán. Bạn có muốn in hóa đơn không?",
                    l("CheckoutSuccess") || "Hóa đơn đã tạo",
                    function (confirmed) {
                        if (confirmed) {
                            // Mở cửa sổ in riêng để không ảnh hưởng màn hình POS.
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

                // Focus lại ô tìm kiếm cho giao dịch tiếp theo.
                _$searchInput.focus();
            })
            .fail(function (err) {
                // ABP tự hiển thị lỗi từ backend; always bên dưới sẽ mở lại nút thanh toán.
            })
            .always(function () {
                // Luôn mở lại nút, kể cả khi backend báo lỗi.
                _$checkoutBtn.prop("disabled", false).html('<i class="fas fa-money-bill-wave"></i> ' + l("Checkout"));
                updateCheckoutButtonState();
            });
    }

    $(document).ready(init);

})(jQuery);
