using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swastika.Messenger.Lib.SignalR.Hubs;

namespace Swastika.Messenger.Mvc
{
    public partial class Startup
    {
        public static void ConfigureSignalRServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    //.AllowCredentials()
                    );
            });
            services.AddSignalR();
        }

        public static void ConfigurationSignalR(IApplicationBuilder app)
        {
            app.UseSignalR(routes =>
            {
                routes.MapHub<ChatHub>("/chat");
            });

            app.UseCors("CorsPolicy");
        }

    }
}
