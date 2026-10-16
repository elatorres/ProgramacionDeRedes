using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Net.Client;

namespace Server_gRPC
{
    public class Startup
    {
        // Este método es invocado por las bibliotecas de runtime. Utilice este método para añadir servicios al contenedor.
        // Para obtener más información sobre cómo configurar su aplicación, visite https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Se registra gRPC en el dependency injection container
            // Agrega toda la infraestructura de gRPC necesaria (serializadores,
            // interceptores, etc.) al servidor web como plugin/servicio
            services.AddGrpc(); 
        }

        // Este método es invocado por el runtime.
        // Utilice este método para configurar el pipeline de solicitudes HTTP.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                // Si en desarrollo muestre errores con información detallada
                app.UseDeveloperExceptionPage();
            }
            
            // habilitar ruteo al endpoint
            app.UseRouting();
            
            // Use/conéctese a los siguientes EndPoints/servicios
            app.UseEndpoints(endpoints =>
            {
                // registra la implementación del servicio gRPC llamado <GreeterService>
                // Implementado en GreeterService.cs
                endpoints.MapGrpcService<GreeterService>();

                // Si alguien se confunde y accede por http le avisamos que por aquí no es...
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}
