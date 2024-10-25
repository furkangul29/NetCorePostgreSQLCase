
using IdentityServer.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Duende.IdentityServer;
using IdentityServer.Data;

namespace IdentityServer
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddAuthorization(options =>
            {
                options.AddPolicy("users.read", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", "users.read");
                });

                options.AddPolicy("users.write", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", "users.write");
                });

                options.AddPolicy("customers.read", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", "customers.read");
                });

                options.AddPolicy("customers.write", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", "customers.write");
                });

                options.AddPolicy("customers.filter", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", "customers.filter");
                });
            });
            services.AddControllersWithViews();

            // Veritabanı bağlantısı
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

            // Identity yapılandırması
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // IdentityServer4 yapılandırması
            var builder = services.AddIdentityServer(options =>
            {
                // ... (Diğer IdentityServer seçenekleri)
            })
            .AddInMemoryIdentityResources(Config.IdentityResources)
            .AddInMemoryApiResources(Config.ApiResources)
            .AddInMemoryApiScopes(Config.ApiScopes)
            .AddInMemoryClients(Config.Clients)
            .AddAspNetIdentity<ApplicationUser>();

            // Geliştirme ortamı için
            builder.AddDeveloperSigningCredential();

            // JWT Bearer kimlik doğrulamasını ekleyin 
            services.AddJwtBearerAuthentication(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // ... (Diğer middleware'ler - loglama, statik dosyalar vb.)

            app.UseRouting();

            // Önce IdentityServer4 middleware'ini ekleyin
            app.UseIdentityServer();

            // Sonra kimlik doğrulama ve yetkilendirme middleware'lerini ekleyin
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }

    // Uzantı metodu (extension method) - IServiceCollection'a eklenir
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityServerConstants.DefaultCookieAuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = "http://localhost:3001"; // IdentityServer adresi
                options.RequireHttpsMetadata = false; // Geliştirme ortamı için

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false, // Aynı projede ise false yapın
                    ValidateAudience = false, // Artık açıkça Audience belirtmiyoruz
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "http://localhost:3001", // IdentityServer adresi
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("crmsecret"))
                };
            });

            return services;
        }
    }
}