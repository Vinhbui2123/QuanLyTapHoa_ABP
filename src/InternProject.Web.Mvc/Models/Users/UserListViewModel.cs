using InternProject.Roles.Dto;
using System.Collections.Generic;

namespace InternProject.Web.Models.Users;

public class UserListViewModel
{
    public IReadOnlyList<RoleDto> Roles { get; set; }
}
