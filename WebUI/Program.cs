﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WebUI.Services.Concrete;
using WebUI.Handlers;
using DataAccessLayer.Context;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using DataAccessLayer.Abstract;
using DataAccessLayer.Concrete;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using WebUI.Services.ClientCredentialTokenServices;
using WebUI.Services.IdentityServices;
using WebUI.Services.LoginServices;
using WebUI.Services.UserIdentityServices;
using WebUI.Services.UserServices;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<CRMContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services.AddAutoMapper(typeof(GeneralMap));
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // Önemli!
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
        ValidateAudience = false, // Audience doğrulamasını kapatın
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "http://localhost:3001", // IdentityServer4 adresi
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
builder.Services.AddHttpClient<IClientCredentialTokenService, ClientCredentialTokenService>();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<ICustomerService, CustomerManager>();
builder.Services.AddScoped<IUserIdentityService, UserIdentityService>();
builder.Services.AddScoped<IUserService, UserService>();

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
builder.Services.AddScoped<IUserDal, UserDal>();


// Token Handler'lar
builder.Services.AddScoped<ResourceOwnerPasswordTokenHandler>();
builder.Services.AddScoped<ClientCredentialTokenHandler>();

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
app.UseRouting();

app.UseAuthentication(); // Authentication middleware'i Authorization'dan önce
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();