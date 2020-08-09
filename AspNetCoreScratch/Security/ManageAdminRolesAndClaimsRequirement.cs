using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AspNetCoreScratch.Security
{
    public class ManageAdminRolesAndClaimsRequirement : IAuthorizationRequirement
    {
    }

    public class ManageAdminRolesAndClaimsHandler : AuthorizationHandler<ManageAdminRolesAndClaimsRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
            ManageAdminRolesAndClaimsRequirement requirement)
        {
            var authFilterContext = context.Resource as AuthorizationFilterContext;
            if (authFilterContext == null)
            {
                return Task.CompletedTask;
            }
            var userId = context.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
            var userIdBeingEdited = authFilterContext.HttpContext.Request.Query["userId"];
            if(context.User.IsInRole("Admin") &&
               context.User.HasClaim(x => x.Type == "Edit Role" && x.Value == "true") &&
               userId != userIdBeingEdited ||
               context.User.IsInRole("Super Admin"))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
