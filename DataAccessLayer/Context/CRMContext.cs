using EntityLayer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Context
{
    public class CRMContext : DbContext
    {
        public CRMContext(DbContextOptions<CRMContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<User> Users { get; set; }

        // ASP.NET Identity tabloları için DbSet tanımlamaları
        public DbSet<IdentityRole> AspNetRoles { get; set; }
        public DbSet<IdentityUser> AspNetUsers { get; set; }
        public DbSet<IdentityUserClaim<string>> AspNetUserClaims { get; set; }
        public DbSet<IdentityUserLogin<string>> AspNetUserLogins { get; set; }
        public DbSet<IdentityUserRole<string>> AspNetUserRoles { get; set; }
        public DbSet<IdentityUserToken<string>> AspNetUserTokens { get; set; }
        public DbSet<IdentityRoleClaim<string>> AspNetRoleClaims { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ASP.NET Identity tabloları için yapılandırma
            modelBuilder.Entity<IdentityRole>().ToTable("AspNetRoles");
            modelBuilder.Entity<IdentityUser>().ToTable("AspNetUsers");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("AspNetUserClaims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("AspNetUserLogins")
                .HasKey(e => new { e.LoginProvider, e.ProviderKey }); // Birincil anahtar tanımı
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("AspNetUserRoles")
                .HasKey(e => new { e.UserId, e.RoleId }); // Birincil anahtar tanımı
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("AspNetUserTokens")
                .HasKey(e => new { e.UserId, e.LoginProvider, e.Name }); // Birincil anahtar tanımı
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims")
                .HasKey(e => new { e.RoleId, e.Id }); // Birincil anahtar tanımı
        }
    }
}
