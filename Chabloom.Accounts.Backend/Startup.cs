// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Chabloom.Accounts.Backend.Data;
using Chabloom.Accounts.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Chabloom.Accounts.Backend
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.All;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // Get the public address for the current environment
            var frontendPublicAddress = System.Environment.GetEnvironmentVariable("ACCOUNTS_FRONTEND_ADDRESS");
            var accountsBackendPublicAddress = System.Environment.GetEnvironmentVariable("ACCOUNTS_BACKEND_ADDRESS");
            if (string.IsNullOrEmpty(frontendPublicAddress) ||
                string.IsNullOrEmpty(accountsBackendPublicAddress))
            {
                frontendPublicAddress = "https://accounts-dev-1.chabloom.com";
                accountsBackendPublicAddress = "https://accounts-api-dev-1.chabloom.com";
            }

            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            const string signingKeyPath = "signing/cert.pfx";
            if (File.Exists(signingKeyPath))
            {
                Console.WriteLine("Using signing credential from kubernetes storage");
                services.AddIdentityServer(options =>
                    {
                        options.UserInteraction.ErrorUrl = $"{frontendPublicAddress}/error";
                        options.UserInteraction.LoginUrl = $"{frontendPublicAddress}/signIn";
                        options.UserInteraction.LogoutUrl = $"{frontendPublicAddress}/signOut";
                    })
                    .AddConfigurationStore(options => options.ConfigureDbContext = x =>
                        x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                            y => y.MigrationsAssembly("Chabloom.Accounts.Backend")))
                    .AddOperationalStore(options => options.ConfigureDbContext = x =>
                        x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                            y => y.MigrationsAssembly("Chabloom.Accounts.Backend")))
                    .AddSigningCredential(new X509Certificate2(File.ReadAllBytes(signingKeyPath)))
                    .AddAspNetIdentity<ApplicationUser>();
            }
            else
            {
                Console.WriteLine("Using developer signing credential");
                services.AddIdentityServer(options =>
                    {
                        options.UserInteraction.ErrorUrl = $"{frontendPublicAddress}/error";
                        options.UserInteraction.LoginUrl = $"{frontendPublicAddress}/signIn";
                        options.UserInteraction.LogoutUrl = $"{frontendPublicAddress}/signOut";
                    })
                    .AddConfigurationStore(options => options.ConfigureDbContext = x =>
                        x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                            y => y.MigrationsAssembly("Chabloom.Accounts.Backend")))
                    .AddOperationalStore(options => options.ConfigureDbContext = x =>
                        x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                            y => y.MigrationsAssembly("Chabloom.Accounts.Backend")))
                    .AddDeveloperSigningCredential()
                    .AddAspNetIdentity<ApplicationUser>();
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = accountsBackendPublicAddress;
                    options.Audience = "Chabloom.Accounts.Backend";
                    options.RequireHttpsMetadata = !Environment.IsDevelopment();
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiScope", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", "Chabloom.Accounts.Backend");
                });
            });

            // Setup CORS origins
            var corsOrigins = new List<string>();
            if (Environment.IsDevelopment())
            {
                corsOrigins.Add("http://localhost:3000");
                corsOrigins.Add("http://localhost:3001");
                corsOrigins.Add("http://localhost:3002");
                corsOrigins.Add("http://localhost:3003");
                corsOrigins.Add("https://accounts-dev-1.chabloom.com");
                corsOrigins.Add("https://accounts-uat-1.chabloom.com");
                corsOrigins.Add("https://billing-dev-1.chabloom.com");
                corsOrigins.Add("https://billing-uat-1.chabloom.com");
                corsOrigins.Add("https://ecommerce-dev-1.chabloom.com");
                corsOrigins.Add("https://ecommerce-uat-1.chabloom.com");
                corsOrigins.Add("https://transactions-dev-1.chabloom.com");
                corsOrigins.Add("https://transactions-uat-1.chabloom.com");
            }
            else
            {
                corsOrigins.Add("https://accounts.chabloom.com");
                corsOrigins.Add("https://billing.chabloom.com");
                corsOrigins.Add("https://ecommerce.chabloom.com");
                corsOrigins.Add("https://transactions.chabloom.com");
            }

            // Add the CORS policy
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins(corsOrigins.ToArray());
                    builder.AllowAnyMethod();
                    builder.AllowAnyHeader();
                    builder.AllowCredentials();
                });
            });

            services.AddApplicationInsightsTelemetry();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForwardedHeaders();

            app.SeedIdentityServer();

            app.UseCors();

            app.UseIdentityServer();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers().RequireAuthorization("ApiScope"); });
        }
    }
}