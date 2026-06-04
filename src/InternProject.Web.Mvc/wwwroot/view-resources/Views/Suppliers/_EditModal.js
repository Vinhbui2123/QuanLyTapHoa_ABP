(function ($) {
    var _supplierService = abp.services.app.supplier,
        l = abp.localization.getSource('InternProject'),
        _$modal = $('#SupplierEditModal'),
        _$form = _$modal.find('form');

    function save() {
        if (!_$form.valid()) {
            return;
        }

        var supplier = _$form.serializeFormToObject();
        supplier.IsActive = $('#SupplierEditIsActive').is(':checked');

        abp.ui.setBusy(_$form);
        _supplierService.update(supplier).done(function () {
            _$modal.modal('hide');
            abp.notify.info(l('SavedSuccessfully'));
            abp.event.trigger('supplier.edited', supplier);
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
