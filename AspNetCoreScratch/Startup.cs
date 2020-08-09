using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreScratch.Models;
using AspNetCoreScratch.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspNetCoreScratch
{
    public class Startup
    {
        private IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextPool<AppDbContext>(options => 
                                        options.UseSqlServer(_configuration.GetConnectionString("EmployeeDBConnection")));
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.SignIn.RequireConfirmedEmail = true;
                options.Tokens.EmailConfirmationTokenProvider = "CustomEmailConfirmation";
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<CustomEmailConfirmationTokenProvider<ApplicationUser>>("CustomEmailConfirmation");

            services.Configure<DataProtectionTokenProviderOptions>(o =>
                    o.TokenLifespan = TimeSpan.FromHours(5));

            // Changes token lifespan of just the Email Confirmation Token type
            services.Configure<CustomEmailConfirmationTokenProviderOptions>(o =>
                    o.TokenLifespan = TimeSpan.FromDays(3));
            services.AddMvc((options) =>
            {
                options.EnableEndpointRouting = false;
                var authorizationBuilder = new AuthorizationPolicyBuilder()
                                                .RequireAuthenticatedUser()
                                                .Build();
                options.Filters.Add(new AuthorizeFilter(authorizationBuilder));
            });

            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = "381295515714-g28jdsnh3o5351pp47cu7gvq9lgmgurs.apps.googleusercontent.com";
                    options.ClientSecret = "VP-fbr1xwGnpCyhXmY1Le0Xv";
                })
                .AddFacebook(options => {
                    options.AppId = "769387073799821";
                    options.AppSecret = "f616f28434391d8c9b449214a48ab9dc";
                });


            services.AddAuthorization(options =>
            {
                options.AddPolicy("DeleteRolePolicy", policy => policy.RequireClaim("Delete Role", "true"));
                options.AddPolicy("EditUserRolePolicy", policy => 
                                                    policy.AddRequirements(new ManageAdminRolesAndClaimsRequirement()));
                                                    
                options.AddPolicy("CreateRolePolicy", policy => policy.RequireClaim("Create Role", "true"));
                options.AddPolicy("EditRolePolicy", policy =>
                                                    policy.RequireAssertion(handler =>
                                                    handler.User.IsInRole("Admin") &&
                                                    handler.User.HasClaim(c => c.Type == "Edit Role" && c.Value == "true") ||
                                                    handler.User.IsInRole("Super Admin")));
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = new PathString("/Administration/AccessDenied");
            });
            services.AddScoped<IEmployeeRepository, SQLEmployeeRepository>();
            services.AddSingleton<IAuthorizationHandler, ManageAdminRolesAndClaimsHandler>();
            services.AddSingleton<DataProtectionPurposeStrings>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error"); // to show global expception
                //app.UseStatusCodePagesWithRedirects("/Error/{0}"); // to show route exception
                app.UseStatusCodePagesWithReExecute("/Error/{0}");
                // app.UseStatusCodePages();
            }

            app.UseRouting();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
            /*app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World");
                });
            });*/

            // for next request in the pipeline

            /*app.Use(async (context, next) =>
            {
                logger.LogInformation("Mv1: Request");
                await next();
                logger.LogInformation("Mv1: Response");
            });*/
            /*var defaultFileOptions = new DefaultFilesOptions();
            defaultFileOptions.DefaultFileNames.Clear();
            defaultFileOptions.DefaultFileNames.Add("foo.html");
            app.UseDefaultFiles(defaultFileOptions);*/
            /*var fileServerOptions = new FileServerOptions();
            fileServerOptions.DefaultFilesOptions.DefaultFileNames.Clear();
            fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add("default.html");
            app.UseFileServer(fileServerOptions);*/

            //app.UseMvcWithDefaultRoute();

            /*app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World");
                
            });*/
        }
    }
}
