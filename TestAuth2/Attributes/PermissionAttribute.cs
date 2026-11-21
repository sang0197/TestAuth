using Microsoft.AspNetCore.Authorization;

namespace TestAuth2.Attributes
{
    public class PermissionAttribute : AuthorizeAttribute
    {
        public PermissionAttribute(string permission)
        {
            Policy = permission;
        }
    }
}
