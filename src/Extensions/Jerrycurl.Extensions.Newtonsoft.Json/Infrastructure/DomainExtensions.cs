﻿using Newtonsoft.Json;
using Jerrycurl.Extensions.Newtonsoft.Json.Metadata.Contracts;

namespace Jerrycurl.Mvc
{
    public static class DomainExtensions
    {
        public static void UseNewtonsoftJson(this DomainOptions options) => options.UseNewtonsoftJson(null);

        public static void UseNewtonsoftJson(this DomainOptions options, JsonSerializerSettings settings)
        {
            settings ??= JsonConvert.DefaultSettings?.Invoke() ?? new JsonSerializerSettings();

            options.Schemas.AddContract(new NewtonsoftBindingContractResolver(settings));
            options.Schemas.AddContract(new NewtonsoftJsonContractResolver(settings));
        }
    }
}
