using HT.Config.ConfigApi.Library.Admin;
using HT.Config.ConfigApi.Library.Configuration;
using HT.Config.ConfigApi.Library.Cryptography;
using HT.Config.ConfigApi.Library.Settings;
using HT.Config.Shared.Admin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HT.Config.ConfigApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
           Configuration = configuration;
            //var cs = new CryptoService();
            //var encryptedBytes = cs.Encrypt("my important stuff", "password");
            //var plaintextstring = cs.Decrypt(encryptedBytes, "password");

        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.Configure<ApiOptions>(Configuration.GetSection("Api"));
            services.PostConfigure<ApiOptions>(options =>
            {
                options.DatabaseConnection = Configuration.GetConnectionString("dbConnectionString");
            });

            services.AddScoped<ISettingsService, SettingsService>();
            services.AddScoped<ISettingAdminService, SettingAdminService>();
            services.AddScoped<ICryptoService, CryptoService>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
