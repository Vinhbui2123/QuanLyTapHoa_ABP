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
