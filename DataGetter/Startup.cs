using DataGetter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Text.Encodings.Web;
using System.Web;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews();
        services.AddSingleton<ConsoleService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        //app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            //ADD SECURITY!!!!!

            endpoints.MapGet("/File/{id}", ([FromRoute] string id,
                                        [FromServices] IImageService imageService) =>
            {
                var filename = HttpUtility.UrlDecode(id);
                var file = imageService.GetImage(filename);

                return file?.Data.Length > 0
                    ? Results.File(file.Data, "image/jpeg")
                    : Results.NotFound();
            });

            //endpoints.MapControllerRoute(
            //    name: "default",
            //    pattern: "{controller=Home}/{action=Index}/{id?}");
        });
        app.ApplicationServices.GetRequiredService<ConsoleService>()
                               .StartAsync(new System.Threading.CancellationToken())
                               .Wait();
    }
}