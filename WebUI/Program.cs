using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WebUI.Services.Concrete;
using WebUI.Concrete;
using WebUI.Handlers;
using WebUI.Interfaces;
using IdentityServer4.Services;
using DataAccessLayer.Context;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using DataAccessLayer.Abstract;
using DataAccessLayer.Concrete;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using WebUI.UserIdentityServices;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<CRMContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddCookie(JwtBearerDefaults.AuthenticationScheme, opt =>
{
    opt.LoginPath = "/Login/Index/";
    opt.LogoutPath = "/Login/LogOut/";
    opt.AccessDeniedPath = "/Pages/AccessDenied/";
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SameSite = SameSiteMode.Strict;
    opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    opt.Cookie.Name = "CrmJwt";
});
builder.Services.AddAutoMapper(typeof(GeneralMap));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opt =>
    {
        opt.LoginPath = "/Login/Index/";
        opt.ExpireTimeSpan = TimeSpan.FromDays(5);
        opt.Cookie.Name = "CrmCookie";
        opt.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    // Users policies
    options.AddPolicy("UsersReadPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "UsersReadPermission");
    });

    options.AddPolicy("UsersWritePolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "UsersWritePermission");
    });

    // Customers policies
    options.AddPolicy("CustomersReadPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "CustomersReadPermission");
    });

    options.AddPolicy("CustomersWritePolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "CustomersWritePermission");
    });

    // Customers Filter policy (Admin only)
    options.AddPolicy("CustomersFilterPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "CustomersFilterPermission");
    });

    // Local API policy
    // Local API policy
    options.AddPolicy("IdentityServerApi", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "IdentityServerApi");
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