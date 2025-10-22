using AutoPartsShop.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AutoPartsShop.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program> //gy alkalmazás memóriában történő elindításához a funkcionális végponttól végpontig tartó tesztekhez
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
        }
    }
}