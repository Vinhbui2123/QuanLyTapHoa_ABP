(function ($) {
  var _userService = abp.services.app.user,
    l = abp.localization.getSource("InternProject"),
    _$modal = $("#UserCreateModal"),
    _$form = _$modal.find("form"),
    _$table = $("#UsersTable");

  var _$usersTable = _$table.DataTable({
    paging: true,
    serverSide: true,
    processing: true,
    listAction: {
      ajaxFunction: _userService.getAll,
      inputFilter: function () {
        return $("#UsersSearchForm").serializeFormToObject(true);
      },
    },
    responsive: false,
    columnDefs: [
      {
        targets: 0,
        data: "userName",
        render: (data, type, row) => {
          return `<a href="javascript:;" class="user-name-link edit-user" data-user-id="${row.id}" data-bs-toggle="modal" data-bs-target="#UserEditModal">${data}</a>`;
        }
      },
      {
        targets: 1,
        data: "fullName",
        orderable: false,
      },
      {
        targets: 2,
        data: "emailAddress",
      },
      {
        targets: 3,
        data: "isActive",
        orderable: false,
        render: (data) => {
          if (data) {
            return `<span class="badge-status badge-status-active"><i class="fas fa-check-circle"></i> ${l("Active")}</span>`;
          } else {
            return `<span class="badge-status badge-status-inactive"><i class="fas fa-ban"></i> ${l("Inactive")}</span>`;
          }
        }
      },
      {
        targets: 4,
        data: null,
        orderable: false,
        render: (data, type, row) => {
          return [
            `<a href="javascript:;" class="user-action-detail edit-user mr-2" data-user-id="${row.id}" data-bs-toggle="modal" data-bs-target="#UserEditModal">${l("Edit")}</a>`,
            `<span class="text-muted">|</span>`,
            `<a href="javascript:;" class="text-danger ml-2 delete-user" data-user-id="${row.id}" data-user-name="${row.name}">${l("Delete")}</a>`
          ].join(" ");
        }
      }
    ],
  });

  _$form.validate({
    rules: {
      Password: "required",
      ConfirmPassword: {
        equalTo: "#Password",
      },
    },
  });

  _$form.find(".save-button").on("click", (e) => {
    e.preventDefault();

    if (!_$form.valid()) {
      return;
    }

    var user = _$form.serializeFormToObject();
    user.roleNames = [];
    var _$roleCheckboxes = _$form[0].querySelectorAll(
      "input[name='role']:checked",
    );
    if (_$roleCheckboxes) {
      for (
        var roleIndex = 0;
        roleIndex < _$roleCheckboxes.length;
        roleIndex++
      ) {
        var _$roleCheckbox = $(_$roleCheckboxes[roleIndex]);
        user.roleNames.push(_$roleCheckbox.val());
      }
    }

    abp.ui.setBusy(_$modal);
    _userService
      .create(user)
      .done(function () {
        _$modal.modal("hide");
        _$form[0].reset();
        abp.notify.info(l("SavedSuccessfully"));
        _$usersTable.ajax.reload();
      })
      .always(function () {
        abp.ui.clearBusy(_$modal);
      });
  });

  $(document).on("click", ".delete-user", function () {
    var userId = $(this).attr("data-user-id");
    var userName = $(this).attr("data-user-name");

    deleteUser(userId, userName);
  });

  function deleteUser(userId, userName) {
    abp.message.confirm(
      abp.utils.formatString(l("AreYouSureWantToDelete"), userName),
      null,
      (isConfirmed) => {
        if (isConfirmed) {
          _userService
            .delete({
              id: userId,
            })
            .done(() => {
              abp.notify.info(l("SuccessfullyDeleted"));
              _$usersTable.ajax.reload();
            });
        }
      },
    );
  }

  $(document).on("click", ".edit-user", function (e) {
    var userId = $(this).attr("data-user-id");

    e.preventDefault();
    abp.ajax({
      url: abp.appPath + "Users/EditModal?userId=" + userId,
      type: "POST",
      dataType: "html",
      success: function (content) {
        $("#UserEditModal div.modal-content").html(content);
      },
      error: function (e) {},
    });
  });

  $(document).on("click", 'a[data-bs-target="#UserCreateModal"]', (e) => {
    $('.nav-tabs a[href="#user-details"]').tab("show");
  });

  abp.event.on("user.edited", (data) => {
    _$usersTable.ajax.reload();
  });

  _$modal
    .on("shown.bs.modal", () => {
      _$modal.find("input:not([type=hidden]):first").focus();
    })
    .on("hidden.bs.modal", () => {
      _$form.clearForm();
    });

  // Handle status filter selection change
  $("#StatusFilter").on("change", function () {
    _$usersTable.ajax.reload();
  });

  $(".btn-search").on("click", (e) => {
    _$usersTable.ajax.reload();
  });

  $(".txt-search").on("keypress", (e) => {
    if (e.which == 13) {
      _$usersTable.ajax.reload();
      return false;
    }
  });
})(jQuery);
