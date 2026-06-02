using InternProject.Roles.Dto;
using System.Collections.Generic;

namespace InternProject.Web.Models.Roles;

public class RoleListViewModel
{
    public IReadOnlyList<PermissionDto> Permissions { get; set; }
}
