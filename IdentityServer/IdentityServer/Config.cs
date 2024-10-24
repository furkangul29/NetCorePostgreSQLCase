using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using System.Collections.Generic;

namespace IdentityServer
{
    public static class Config
    {
        // Daha açıklayıcı olmak için ApiResource adlarını güncelleyelim
        public static IEnumerable<ApiResource> ApiResources => new ApiResource[]
        {
            new ApiResource("customersAPI", "Customers API")
            {
                Scopes = { "customers.read", "customers.write", "customers.filter" }
            },
            new ApiResource("usersAPI", "Users API")
            {
                Scopes = { "users.read", "users.write" }
            },
            // Yerel API kaynağı (değiştirilmedi)
            new ApiResource(IdentityServerConstants.LocalApi.ScopeName)
        };

        // IdentityResources (değiştirilmedi)
        public static IEnumerable<IdentityResource> IdentityResources => new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email()
        };

        // ApiScopes (daha açıklayıcı isimlendirme)
        public static IEnumerable<ApiScope> ApiScopes => new ApiScope[]
        {
            new ApiScope("users.read", "Kullanıcıları okuma yetkisi"),
            new ApiScope("users.write", "Kullanıcıları yazma yetkisi"),
            new ApiScope("customers.read", "Müşterileri okuma yetkisi"),
            new ApiScope("customers.write", "Müşterileri yazma yetkisi"),
            new ApiScope("customers.filter", "Müşterileri filtreleme yetkisi"),
            new ApiScope(IdentityServerConstants.LocalApi.ScopeName)
        };

        public static IEnumerable<Client> Clients => new Client[]
        {
            // CRM Kullanıcı (CrmUser)
            new Client
            {
                ClientId = "CrmUserId",
                ClientName = "CRM User",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                ClientSecrets = { new Secret("crmsecret".Sha256()) },
                AllowedScopes = {
                    "customers.read",
                    "customers.write",
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email
                }
            },

            // CRM Yönetici (CrmManager) - customers.filter eklendi
            new Client
            {
                ClientId = "CrmManagerId",
                ClientName = "CRM Manager",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                ClientSecrets = { new Secret("crmsecret".Sha256()) },
                AllowedScopes = {
                    "customers.read",
                    "customers.write",
                    "customers.filter", // Müşteri filtreleme yetkisi
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email
                }
            },

            // Admin (değiştirilmedi)
            new Client
            {
                ClientId = "AdminId",
                ClientName = "Admin User",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                ClientSecrets = { new Secret("crmsecret".Sha256()) },
                AllowedScopes = {
                    "customers.read",
                    "customers.write",
                    "customers.filter",
                    "users.read",
                    "users.write",
                    IdentityServerConstants.LocalApi.ScopeName,
                    IdentityServerConstants.StandardScopes.Email,
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile
                },
                AccessTokenLifetime = 600 // İhtiyaca göre ayarlanabilir
            }
        };
    }
}