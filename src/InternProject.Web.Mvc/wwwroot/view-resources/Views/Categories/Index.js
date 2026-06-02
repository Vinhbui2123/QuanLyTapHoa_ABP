(function ($) {
  var _categoryService = abp.services.app.category,
    l = abp.localization.getSource("InternProject"),
    _$modal = $("#CategoryCreateModal"),
    _$form = _$modal.find("form"),
    _$table = $("#CategoriesTable");

  function updateKPICounters() {
    _categoryService.getDashboardStats({}).done(function (stats) {
      $("#kpi-total-categories").text(stats.totalCount.toLocaleString('vi-VN'));
      $("#kpi-active-categories").text(stats.activeCount.toLocaleString('vi-VN'));
      $("#kpi-inactive-categories").text(stats.inactiveCount.toLocaleString('vi-VN'));
    });
  }

  var _$categoriesTable = _$table.DataTable({
    paging: true,
    serverSide: true,
    processing: true,
    listAction: {
      ajaxFunction: _categoryService.getList,
      inputFilter: function () {
        return $("#CategoriesSearchForm").serializeFormToObject(true);
      },
    },
    responsive: false,
    columnDefs: [
      {
        targets: 0,
        data: "name",
        render: (data, type, row) => {
          if (abp.auth.hasPermission('Pages.Categories.Edit')) {
            return `<a href="javascript:;" class="category-name-link edit-category" data-category-id="${row.id}" data-bs-toggle="modal" data-bs-target="#CategoryEditModal"><strong>${data}</strong></a>`;
          }
          return `<strong>${data}</strong>`;
        }
      },
      {
        targets: 1,
        data: "description",
        render: (data) => data || "---"
      },
      {
        targets: 2,
        data: "isActive",
        render: (data) => {
          if (data) {
            return `<span class="badge-status badge-status-active"><i class="fas fa-check-circle"></i> ${l("ActiveCustomers")}</span>`;
          } else {
            return `<span class="badge-status badge-status-inactive"><i class="fas fa-ban"></i> ${l("InactiveCustomers")}</span>`;
          }
        }
      },
      {
        targets: 3,
        data: null,
        orderable: false,
        render: (data, type, row) => {
          var actions = [];
          if (abp.auth.hasPermission('Pages.Categories.Edit')) {
            actions.push(`<a href="javascript:;" class="category-action-detail edit-category mr-2" data-category-id="${row.id}" data-bs-toggle="modal" data-bs-target="#CategoryEditModal">${l("Edit")}</a>`);
          }
          if (abp.auth.hasPermission('Pages.Categories.Delete')) {
            if (actions.length > 0) {
              actions.push(`<span class="text-muted">|</span>`);
            }
            actions.push(`<a href="javascript:;" class="text-danger ml-2 delete-category" data-category-id="${row.id}" data-category-name="${row.name}">${l("Delete")}</a>`);
          }
          return actions.join(" ");
        }
      }
    ],
  });

  updateKPICounters();

  $("#CategoriesSearchForm").on("submit", function (e) {
    e.preventDefault();
    _$categoriesTable.ajax.reload();
  });

  $("#StatusFilter").on("change", function () {
    _$categoriesTable.ajax.reload();
  });

  _$form.validate({
    rules: {
      Name: {
        required: true,
      },
    },
  });

  _$form.find(".save-button").on("click", function (e) {
    e.preventDefault();

    if (!_$form.valid()) {
      return;
    }

    var category = _$form.serializeFormToObject();
    category.IsActive = $("#category-is-active").is(":checked");

    abp.ui.setBusy(_$modal);
    _categoryService
      .create(category)
      .done(function () {
        _$modal.modal("hide");
        _$form[0].reset();
        abp.notify.info(l("SavedSuccessfully"));
        updateKPICounters();
        _$categoriesTable.ajax.reload();
      })
      .always(function () {
        abp.ui.clearBusy(_$modal);
      });
  });

  $(document).on("click", ".delete-category", function () {
    var categoryId = $(this).attr("data-category-id");
    var categoryName = $(this).attr("data-category-name");

    deleteCategory(categoryId, categoryName);
  });

  $(document).on("click", ".edit-category", function (e) {
    var categoryId = $(this).attr("data-category-id");

    e.preventDefault();
    abp.ajax({
      url: abp.appPath + "Categories/EditModal?categoryId=" + categoryId,
      type: "POST",
      dataType: "html",
      success: function (content) {
        $("#CategoryEditModal div.modal-content").html(content);
      },
      error: function (e) {},
    });
  });

  abp.event.on("category.edited", (data) => {
    updateKPICounters();
    _$categoriesTable.ajax.reload();
  });

  function deleteCategory(categoryId, categoryName) {
    abp.message.confirm(
      abp.utils.formatString(l("AreYouSureWantToDelete"), categoryName),
      null,
      (isConfirmed) => {
        if (isConfirmed) {
          _categoryService
            .delete({ id: categoryId })
            .done(() => {
              abp.notify.info(l("SuccessfullyDeleted"));
              updateKPICounters();
              _$categoriesTable.ajax.reload();
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
      $("#category-is-active").prop("checked", true);
    });

  $(".btn-search").on("click", () => {
    _$categoriesTable.ajax.reload();
  });

  $(".btn-clear").on("click", () => {
    $(".txt-search").val("");
    $("#StatusFilter").val("").trigger("change");
  });

  $(".txt-search").on("keypress", (e) => {
    if (e.which == 13) {
      _$categoriesTable.ajax.reload();
      return false;
    }
  });
})(jQuery);
