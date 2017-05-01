using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wtwd.PublishSubscribe.Service.Hubs;

namespace Wtwd.PublishSubscribe.Service
{
    public class Startup
    {
        /// <summary>
        /// Configure aspnet core services
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSignalR();
        }

        /// <summary>
        /// Configure the aspnet core HTTP request pipeline.
        /// </summary>
        /// <param name="app">application builder</param>
        /// <param name="env">hosting environment</param>
        /// <param name="loggerFactory">logger factory</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            app.UseMvc();

            app.UseSignalR(routes =>
            {
                routes.MapHub<PublishSubscribeHub>("/PublishSubscribe");
            });
        }
    }
}
