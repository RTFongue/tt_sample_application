using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.EntityFrameworkCore;
using sample_application.Data;
using sample_application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;

namespace sample_application
{
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
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(
                    Configuration.GetConnectionString("DefaultConnection")));

            services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddIdentityServer()
                .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

            services.AddAuthentication()
                .AddIdentityServerJwt();

            services.AddControllersWithViews();
            services.AddRazorPages();
            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            if (!env.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseIdentityServer();
            app.UseAuthorization();

            app.Use(async (context, next) =>
            {
                bool b = await IsPasswordCompromised(context);
                if (b)
                {
                    await context.Response.WriteAsync("The password is registered in the passwords breach database. Please enter a new one");
                    return;
                }

                await next();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });
        }

        private bool IsPasswordResetRequest(HttpContext context)
        {
            if (!context.Request.Path.ToString().ToLower().EndsWith("changepassword") || context.Request.ContentLength == null)
                return false;

            var keys = context.Request.Form.Keys.ToArray().ToList();
            return (keys.Any(x => x.ToLower().Contains("oldpassword")) && keys.Any(x => x.ToLower().Contains("newpassword")));
        }

        private bool IsNewUserRequest(HttpContext context)
        {
            if (!context.Request.Path.ToString().ToLower().EndsWith("register") || context.Request.ContentLength == null)
                return false;

            var keys = context.Request.Form.Keys.ToArray().ToList();
            return (keys.Any(x => x.ToLower().Contains("password")) && keys.Any(x => x.ToLower().Contains("confirmpassword")));
        }

        private async Task<bool> IsPasswordCompromised(HttpContext context)
        {
            string password = string.Empty;
            if (IsPasswordResetRequest(context))
            {
                password = context.Request.Form.FirstOrDefault(x => x.Key.ToLower().Contains("newpassword")).Value;
            }
            else if (IsNewUserRequest(context))
            {
                password = context.Request.Form.FirstOrDefault(x => x.Key.ToLower().Contains("password")).Value;
            }

            if (!string.IsNullOrEmpty(password))
            {
                return await CheckPassword(password);
            }

            return false;
        }


        public async Task<bool> CheckPassword(string s)
        {
            string hashWord = HashString(s);
            var url = $"https://api.pwnedpasswords.com/range/";

            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);

            HttpResponseMessage response = await httpClient.GetAsync(hashWord.Substring(0, 5)).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var items = data.Split("\r\n").ToList().ConvertAll(x => x.Substring(0, x.IndexOf(":")));

                var checks = items.Where(x => x == hashWord.Substring(5)).ToList();

                return checks.Any();
            }

            return true;
        }

        public string HashString(string pwd)
        {
            byte[] bytes;
            using (HashAlgorithm algorithm = SHA1.Create())
                bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(pwd));

            StringBuilder body = new StringBuilder(bytes.Length);
            foreach (byte byt in bytes)
            {
                body.Append(byt.ToString());
            }

            return body.ToString();
        }

    }
}
