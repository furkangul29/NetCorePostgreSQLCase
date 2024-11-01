﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WebUI.Services.Concrete;
using DataAccessLayer.Context;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Abstract;
using DataAccessLayer.Concrete;
using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using WebUI.Services.IdentityServices;
using WebUI.Services.LoginServices;
using WebUI.Services.UserIdentityServices;
using WebUI.Services.UserServices;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using WebUI.Services.TokenServices;
using Microsoft.AspNetCore.Authentication;
using Serilog.Events;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<CRMContext>(options =>
    options.UseNpgsql(connectionString));

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseNpgsql(connectionString));

//// Identity yapılandırması
//builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
//    .AddEntityFrameworkStores<ApplicationDbContext>()
//    .AddDefaultTokenProviders();

// Log dizinini oluştur
if (!Directory.Exists("logs"))
{
    Directory.CreateDirectory("logs");
}

// Serilog'u başlat
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information() // En düşük log seviyesi
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Microsoft loglarını filtrele
    .MinimumLevel.Override("System", LogEventLevel.Warning)    // Sistem loglarını filtrele
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Error)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Error)
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Error)
    .MinimumLevel.Override("IdentityServer", LogEventLevel.Warning)
    .Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Request starting") ||
                                     logEvent.MessageTemplate.Text.Contains("Executing endpoint") ||
                                     logEvent.MessageTemplate.Text.Contains("Executed action"))
    .Enrich.FromLogContext()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day) // Dosyaya yaz
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddAutoMapper(typeof(GeneralMap));
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); 
    options.Cookie.HttpOnly = true; 
    options.Cookie.IsEssential = true; 
});



builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opt =>
{
    opt.LoginPath = "/Login/Index/";
    opt.LogoutPath = "/Login/LogOut/";
    opt.AccessDeniedPath = "/Pages/AccessDenied/";
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SameSite = SameSiteMode.Strict;
    opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    opt.Cookie.Name = "CrmCookie";
    opt.SlidingExpiration = true;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Authority = "http://localhost:3001"; // IdentityServer4 adresi
    options.RequireHttpsMetadata = false; // Geliştirme ortamı için

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false, 
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "http://localhost:3001", 
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("crmsecret"))
    };
});
builder.Services.AddAuthorization(options =>
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


// Token yönetimi ve HTTP context
builder.Services.AddAccessTokenManagement();
builder.Services.AddHttpContextAccessor();

// Servis baðýmlýlýklarý
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddHttpClient<IIdentityService, IdentityService>();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<ICustomerService, CustomerManager>();
builder.Services.AddScoped<IUserIdentityService, UserIdentityService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddScoped<ICustomerDal, CustomerDal>();


//builder.Services.AddScoped<IIdentityServerInteractionService, IdentityServerInteractionService>();

// HTTP client
builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Hata yönetimi
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseMiddleware<TokenCheckMiddleware>();
app.UseAuthentication(); // Authentication middleware'i Authorization'dan önce
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();