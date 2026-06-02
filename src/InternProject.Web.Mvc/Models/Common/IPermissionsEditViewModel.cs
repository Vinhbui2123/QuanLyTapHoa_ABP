using InternProject.Roles.Dto;
using System.Collections.Generic;

namespace InternProject.Web.Models.Common;

public interface IPermissionsEditViewModel
{
    List<FlatPermissionDto> Permissions { get; set; }
}