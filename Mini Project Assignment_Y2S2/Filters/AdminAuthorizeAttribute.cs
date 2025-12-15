using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;

namespace Mini_Project_Assignment_Y2S2.Filters
{
    
    public class AdminAuthorizeAttribute : TypeFilterAttribute
    {
        public AdminAuthorizeAttribute() : base(typeof(AdminAuthorizeFilter))
        {
        }
    }

  
    public class AdminAuthorizeFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if action has [AllowAnonymous] attribute
            var hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
                .Any(em => em.GetType().Name == "AllowAnonymousAttribute");

            if (hasAllowAnonymous)
            {
                return; // Skip authorization
            }

            var userRole = context.HttpContext.Session.GetString("UserRole");
            var role = context.HttpContext.Session.GetString("Role");

            // Check both possible session keys for admin role
            bool isAdmin = (userRole?.ToLower() == "admin") || (role?.ToLower() == "admin");

            if (!isAdmin)
            {
                // Redirect to login if not authorized
                context.Result = new RedirectToActionResult("Login", "Admin", new { area = "" });
            }
        }
    }
}
