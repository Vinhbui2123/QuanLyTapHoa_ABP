(function ($) {
  var _roleService = abp.services.app.role,
    l = abp.localization.getSource("InternProject"),
    _$modal = $("#RoleCreateModal"),
    _$form = _$modal.find("form"),
    _$table = $("#RolesTable");

  var _$rolesTable = _$table.DataTable({
    paging: true,
    serverSide: true,
    processing: true,
    listAction: {
      ajaxFunction: _roleService.getAll,
      inputFilter: function () {
        return $("#RolesSearchForm").serializeFormToObject(true);
      },
    },
    responsive: false,
    columnDefs: [
      {
        targets: 0,
        data: "name",
        render: (data, type, row) => {
          return `<a href="javascript:;" class="role-name-link edit-role" data-role-id="${row.id}" data-bs-toggle="modal" data-bs-target="#RoleEditModal">${data}</a>`;
        }
      },
      {
        targets: 1,
        data: "displayName",
      },
      {
        targets: 2,
        data: null,
        orderable: false,
        render: (data, type, row) => {
          return [
            `<a href="javascript:;" class="role-action-detail edit-role mr-2" data-role-id="${row.id}" data-bs-toggle="modal" data-bs-target="#RoleEditModal">${l("Edit")}</a>`,
            `<span class="text-muted">|</span>`,
            `<a href="javascript:;" class="text-danger ml-2 delete-role" data-role-id="${row.id}" data-role-name="${row.name}">${l("Delete")}</a>`
          ].join(" ");
        }
      }
    ],
  });

  _$form.find(".save-button").on("click", (e) => {
    e.preventDefault();

    if (!_$form.valid()) {
      return;
    }

    var role = _$form.serializeFormToObject();
    role.grantedPermissions = [];
    var _$permissionCheckboxes = _$form[0].querySelectorAll(
      "input[name='permission']:checked",
    );
    if (_$permissionCheckboxes) {
      for (
        var permissionIndex = 0;
        permissionIndex < _$permissionCheckboxes.length;
        permissionIndex++
      ) {
        var _$permissionCheckbox = $(_$permissionCheckboxes[permissionIndex]);
        role.grantedPermissions.push(_$permissionCheckbox.val());
      }
    }

    abp.ui.setBusy(_$modal);
    _roleService
      .create(role)
      .done(function () {
        _$modal.modal("hide");
        _$form[0].reset();
        abp.notify.info(l("SavedSuccessfully"));
        _$rolesTable.ajax.reload();
      })
      .always(function () {
        abp.ui.clearBusy(_$modal);
      });
  });

  $(document).on("click", ".delete-role", function () {
    var roleId = $(this).attr("data-role-id");
    var roleName = $(this).attr("data-role-name");

    deleteRole(roleId, roleName);
  });

  $(document).on("click", ".edit-role", function (e) {
    var roleId = $(this).attr("data-role-id");

    e.preventDefault();
    abp.ajax({
      url: abp.appPath + "Roles/EditModal?roleId=" + roleId,
      type: "POST",
      dataType: "html",
      success: function (content) {
        $("#RoleEditModal div.modal-content").html(content);
      },
      error: function (e) {},
    });
  });

  abp.event.on("role.edited", (data) => {
    _$rolesTable.ajax.reload();
  });

  function deleteRole(roleId, roleName) {
    abp.message.confirm(
      abp.utils.formatString(l("AreYouSureWantToDelete"), roleName),
      null,
      (isConfirmed) => {
        if (isConfirmed) {
          _roleService
            .delete({
              id: roleId,
            })
            .done(() => {
              abp.notify.info(l("SuccessfullyDeleted"));
              _$rolesTable.ajax.reload();
            });
        }
      },
    );
  }

  _$modal
    .on("shown.bs.modal", () => {
      _$modal.find("input:not([type=hidden]):first").focus();
    })
    .on("hidden.bs.modal", () => {
      _$form.clearForm();
    });

  $(".btn-search").on("click", (e) => {
    _$rolesTable.ajax.reload();
  });

  $(".txt-search").on("keypress", (e) => {
    if (e.which == 13) {
      _$rolesTable.ajax.reload();
      return false;
    }
  });
})(jQuery);
