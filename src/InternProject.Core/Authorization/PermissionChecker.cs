using Abp.Authorization;
using InternProject.Authorization.Roles;
using InternProject.Authorization.Users;

namespace InternProject.Authorization;

public class PermissionChecker : PermissionChecker<Role, User>
{
    public PermissionChecker(UserManager userManager)
        : base(userManager)
    {
    }
}
