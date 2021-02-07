using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MainApp.RBAC.Attributes
{
    public static class NoPermissionResult
    {
        public static IActionResult Generate()
        {
            return new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
    }
}