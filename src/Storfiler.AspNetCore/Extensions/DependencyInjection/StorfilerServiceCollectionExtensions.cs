using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Storfiler.AspNetCore.Core;

namespace Storfiler.AspNetCore.Extensions.DependencyInjection
{
    public static class StorfilerServiceCollectionExtensions
    {
        public static IServiceCollection AddStorfiler(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            services.AddRouting();
            
            var storfilerSection = configuration;
            services.Configure<StorfilerOptions>(storfilerSection);
            services.AddSingleton<IStorageProvider, StorageProvider>();
            services.AddScoped<StorfilerService>();
            return services;
        }
    }
}