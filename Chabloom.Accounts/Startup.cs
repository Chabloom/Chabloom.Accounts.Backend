// Copyright 2020 Chabloom LC. All rights reserved.

using Chabloom.Accounts.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Chabloom.Accounts
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
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Get the frontend application address
            var frontendAddress = Configuration.GetValue<string>("FrontendAddress");

            services.AddIdentityServer(options =>
                {
                    options.UserInteraction.LoginUrl = $"{frontendAddress}/login";
                    options.UserInteraction.LogoutUrl = $"{frontendAddress}/logout";
                })
                .AddInMemoryApiResources(Configuration.GetSection("Identity:ApiResources"))
                .AddInMemoryApiScopes(Configuration.GetSection("Identity:ApiScopes"))
                .AddInMemoryClients(Configuration.GetSection("Identity:Clients"))
                .AddInMemoryIdentityResources(Configuration.GetSection("Identity:IdentityResources"))
                .AddDeveloperSigningCredential()
                .AddAspNetIdentity<ApplicationUser>();

            services.AddControllers();

            services.AddCors(options =>
            {
                options.AddPolicy("Development",
                    builder =>
                    {
                        builder.WithOrigins(frontendAddress);
                        builder.WithOrigins("http://localhost:3000");
                        builder.WithOrigins("http://localhost:3001");
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                        builder.AllowCredentials();
                    });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseCors("Development");
            }

            app.UseIdentityServer();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}