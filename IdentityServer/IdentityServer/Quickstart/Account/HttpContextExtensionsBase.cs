using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace IdentityServerHost.Quickstart.UI
{
    public static class HttpContextExtensionsBase
    {
        public static async Task<bool> GetSchemeSupportsSignOutAsync(this HttpContext context, string scheme)
        {
            var schemeProvider = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
            var handlerProvider = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
            var handler = await handlerProvider.GetHandlerAsync(context, scheme);

            // 'null' kontrolü eklenerek olası hatalar önleniyor
            return handler != null && handler is IAuthenticationSignOutHandler;
        }
    }
}