// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace  IdentityServer
{
    public static class Config
    {
        public static IEnumerable<ApiResource> ApiResources => new ApiResource[]
        {
            // Müşteriler API kaynağı
            new ApiResource("ResourceCustomers")
            {
                Scopes = { "CustomersReadPermission", "CustomersWritePermission" } // Müşterileri okuma ve yazma izni
            },
            // Müşterileri filtreleme API kaynağı
            new ApiResource("ResourceCustomersFilter")
            {
                Scopes = { "CustomersFilterPermission" } // Müşterileri filtreleme izni
            },

            // Kullanıcılar API kaynağı
            new ApiResource("ResourceUsers")
            {
                Scopes = { "UsersReadPermission", "UsersWritePermission" } // Kullanıcıları okuma ve yazma izni
            },

            // Yerel API kaynakları
            new ApiResource(IdentityServerConstants.LocalApi.ScopeName)
        };

        public static IEnumerable<IdentityResource> IdentityResources => new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email()
        };

        public static IEnumerable<ApiScope> ApiScopes => new ApiScope[]
        {
            new ApiScope("UsersReadPermission", "Read authority for user operations"),
            new ApiScope("UsersWritePermission", "Write authority for user operations"),
            new ApiScope("CustomersReadPermission", "Read authority for customers operations"),
            new ApiScope("CustomersWritePermission", "Write authority for customers operations"),
            // Müşteriler için filtreleme API izni (sadece admin erişimi)
            new ApiScope("CustomersFilterPermission", "Filter authority for customers operations (Admin only)"),
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
                    "CustomersReadPermission",  // Müşteri okuma izni
                    "CustomersWritePermission", // Müşteri yazma izni
                    IdentityServerConstants.LocalApi.ScopeName,
                    IdentityServerConstants.StandardScopes.Email,
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile
                }
            },

            // CRM Yönetici (CrmManager)
            new Client
            {
                ClientId = "CrmManagerId",
                ClientName = "CRM Manager",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                ClientSecrets = { new Secret("crmsecret".Sha256()) },
                AllowedScopes = {
                    "CustomersReadPermission",  // Müşteri okuma izni
                    "CustomersWritePermission", // Müşteri yazma izni
                    "CustomersFilterPermission", // Müşteri filtreleme izni
                    IdentityServerConstants.LocalApi.ScopeName,
                    IdentityServerConstants.StandardScopes.Email,
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile
                }
            },

            // Admin
            new Client
            {
                ClientId = "AdminId",
                ClientName = "Admin User",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                ClientSecrets = { new Secret("crmsecret".Sha256()) },
                AllowedScopes = {
                    "CustomersReadPermission",  // Müşteri okuma izni
                    "CustomersWritePermission", // Müşteri yazma izni
                    "CustomersFilterPermission", // Müşteri filtreleme izni
                    "UsersReadPermission", // Kullanıcı okuma izni
                    "UsersWritePermission", // Kullanıcı yazma izni
                    IdentityServerConstants.LocalApi.ScopeName,
                    IdentityServerConstants.StandardScopes.Email,
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile
                },
                AccessTokenLifetime = 600
            }
        };
    }
}
