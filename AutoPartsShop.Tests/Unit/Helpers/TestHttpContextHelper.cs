using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AutoPartsShop.Tests.Unit.Helpers
{
    public static class TestHttpContextHelper
    {
        public static void AttachUser(ControllerBase p_controller, int p_userId)
        {
            var principal = new ClaimsPrincipal
                (new ClaimsIdentity(
                    new[] {new Claim(ClaimTypes.NameIdentifier, p_userId.ToString()) }, // Ez az egész tesztelt "felhasználó" — a User, amit a controller használ. A ClaimsPrincipal az az objektum, amit a.NET automatikusan a HttpContext.User - be rak, amikor valódi felhasználó van bejelentkezve.
                    authenticationType: "mock")); // a mock egy tetszőleges hitelesítési típus, lehetne bármi más is

            // A controllerbe beleinjektáljuk a mock felhasználót, mintha ténylegesen be lenne jelentkezve.
            p_controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }
    }
}
