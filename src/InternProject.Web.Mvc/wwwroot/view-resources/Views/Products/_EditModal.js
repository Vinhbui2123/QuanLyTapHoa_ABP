(function ($) {
    var _productService = abp.services.app.product,
        l = abp.localization.getSource('InternProject'),
        _$modal = $('#ProductEditModal'),
        _$form = _$modal.find('form');

    function save() {
        if (!_$form.valid()) {
            return;
        }

        var product = _$form.serializeFormToObject();
        product.IsActive = $('#ProductEditIsActive').is(':checked');

        // Parse numeric fields properly
        product.CostPrice = parseFloat(product.CostPrice) || 0;
        product.SalePrice = parseFloat(product.SalePrice) || 0;
        product.StockQuantity = parseInt(product.StockQuantity) || 0;
        product.MinStock = parseInt(product.MinStock) || 0;

        abp.ui.setBusy(_$form);
        _productService.update(product).done(function () {
            _$modal.modal('hide');
            abp.notify.info(l('SavedSuccessfully'));
            abp.event.trigger('product.edited', product);
        }).always(function () {
            abp.ui.clearBusy(_$form);
        });
    }

    // Handle Product Edit Image Upload
    $("#product-edit-image-file").on("change", function () {
        var files = this.files;
        if (files.length === 0) {
            return;
        }

        var file = files[0];
        var formData = new FormData();
        formData.append("file", file);

        abp.ui.setBusy(_$form);

        $.ajax({
            url: abp.appPath + "Products/UploadImage",
            type: "POST",
            data: formData,
            contentType: false,
            processData: false,
            headers: {
                "X-XSRF-TOKEN": abp.security.antiForgery.getToken()
            },
            success: function (response) {
                if (response.success) {
                    $("#product-edit-image-url").val(response.imageUrl);
                    var previewUrl = abp.appPath + response.imageUrl.replace(/^\//, "");
                    $("#product-edit-image-preview").attr("src", previewUrl).show();
                    $("#product-edit-image-placeholder").hide();
                    $("#btn-remove-product-edit-image").show();
                }
            },
            error: function (xhr) {
                var errorMsg = l("UploadFailed");
                if (xhr.status === 400 && xhr.responseText) {
                    try {
                        var errObj = JSON.parse(xhr.responseText);
                        errorMsg = errObj.message || errObj.error?.message || xhr.responseText;
                    } catch(e) {
                        errorMsg = xhr.responseText;
                    }
                }
                abp.message.error(errorMsg);
                $("#product-edit-image-file").val("");
            },
            complete: function () {
                abp.ui.clearBusy(_$form);
            }
        });
    });

    // Handle Remove Image button click in Edit modal
    $("#btn-remove-product-edit-image").on("click", function () {
        $("#product-edit-image-url").val("");
        $("#product-edit-image-preview").attr("src", "").hide();
        $("#product-edit-image-placeholder").show();
        $("#product-edit-image-file").val("");
        $(this).hide();
    });

    _$form.closest('div.modal-content').find(".save-button").click(function (e) {
        e.preventDefault();
        save();
    });

    _$form.find('input').on('keypress', function (e) {
        if (e.which === 13) {
            e.preventDefault();
            save();
        }
    });

    _$modal.on('shown.bs.modal', function () {
        _$form.find('input[type=text]:first').focus();
    });
})(jQuery);
