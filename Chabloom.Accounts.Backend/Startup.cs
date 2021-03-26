// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Chabloom.Accounts.Backend.Data;
using Chabloom.Accounts.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

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

            var redis = ConnectionMultiplexer.Connect("redis-master");
            services.AddDataProtection()
                .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");

            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            const string signingKeyPath = "signing/cert.pfx";
            const string frontendPublicAddress = "https://accounts-dev-1.chabloom.com";
            if (File.Exists(signingKeyPath))
            {
                Console.WriteLine("Using signing credential from kubernetes storage");
                var signingKeyCert = new X509Certificate2(File.ReadAllBytes(signingKeyPath));
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
                    .AddSigningCredential(signingKeyCert)
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
                .AddCookie(options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.IsEssential = true;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = "https://accounts-api-dev-1.chabloom.com";
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
                // Allow CORS from accounts DEV, UAT, and local environments
                corsOrigins.Add("http://localhost:3000");
                corsOrigins.Add("https://localhost:3000");
                corsOrigins.Add("https://accounts-dev-1.chabloom.com");
                corsOrigins.Add("https://accounts-uat-1.chabloom.com");
                // Allow CORS from billing DEV, UAT, and local environments
                corsOrigins.Add("http://localhost:3001");
                corsOrigins.Add("https://localhost:3001");
                corsOrigins.Add("https://billing-dev-1.chabloom.com");
                corsOrigins.Add("https://billing-uat-1.chabloom.com");
                // Allow CORS from transactions DEV, UAT, and local environments
                corsOrigins.Add("http://localhost:3002");
                corsOrigins.Add("https://localhost:3002");
                corsOrigins.Add("https://transactions-dev-1.chabloom.com");
                corsOrigins.Add("https://transactions-uat-1.chabloom.com");
                // Allow CORS from ecommerce DEV, UAT, and local environments
                corsOrigins.Add("http://localhost:3003");
                corsOrigins.Add("https://localhost:3003");
                corsOrigins.Add("https://ecommerce-dev-1.chabloom.com");
                corsOrigins.Add("https://ecommerce-uat-1.chabloom.com");
            }
            else
            {
                // Allow CORS from accounts PROD environment
                corsOrigins.Add("https://accounts.chabloom.com");
                // Allow CORS from billing PROD environment
                corsOrigins.Add("https://billing.chabloom.com");
                // Allow CORS from transactions PROD environment
                corsOrigins.Add("https://transactions.chabloom.com");
                // Allow CORS from ecommerce PROD environment
                corsOrigins.Add("https://ecommerce.chabloom.com");
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