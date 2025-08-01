using AutoPartShop.Core.Helpers;
using AutoPartsShop.Core.Helpers;
using AutoPartsShop.Infrastructure;
using AutoPartsShop.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

namespace AutoPartsShop.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // CORS beállítások (Angular fejlesztõi szerver engedélyezése)
            var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(MyAllowSpecificOrigins,
                    policy =>
                    {
                        policy.WithOrigins(
                            "http://localhost:4200",
                            "https://autopartsshopfrontend-azc2b6ajfyggapgg.northeurope-01.azurewebsites.net") // Az Angular fejlesztõi szerver URL-je
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials(); // Engedélyezi a hitelesítési adatok küldését (JWT)
                    });
            });

            // JWT konfiguráció beolvasása az appsettings.json-ból
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]))
                    };
                });

            //  JSON beállítások a válaszokhoz
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            builder.Services.AddControllers();

            // Swagger/OpenAPI konfiguráció
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header használata. Írd be a token-t így: Bearer {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            //  Adatbázis kapcsolat beállítása
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddSingleton<AzureBlobStorageService>();

            var app = builder.Build();

            // Middleware konfiguráció
            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors(MyAllowSpecificOrigins); //  CORS middleware bekapcsolása

            app.UseDefaultFiles();
            app.UseRouting();

            app.UseAuthentication(); //  Autentikáció bekapcsolása (JWT token ellenõrzés)
            app.UseAuthorization(); //  Jogosultságkezelés bekapcsolása

            app.MapControllers();
            app.MapFallbackToFile("/browser/{*path:nonfile}", "/browser/index.html");
            app.Run();
        }
    }
}
