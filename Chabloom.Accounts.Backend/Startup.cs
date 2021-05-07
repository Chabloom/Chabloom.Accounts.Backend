// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System;
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
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

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

            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            const string signingKeyPath = "signing/cert.pfx";
            var frontendPublicAddress = Environment.GetEnvironmentVariable("ACCOUNTS_FRONTEND");
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

            var authority = Environment.GetEnvironmentVariable("ACCOUNTS_AUTHORITY");
            const string audience = "Chabloom.Accounts.Backend";

            var redisConfiguration = Environment.GetEnvironmentVariable("REDIS_CONFIGURATION");
            if (!string.IsNullOrEmpty(redisConfiguration))
            {
                services.AddDataProtection()
                    .PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(redisConfiguration),
                        $"{audience}-DataProtection");
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
                    options.Authority = authority;
                    options.Audience = audience;
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiScope", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", audience);
                });
            });

            services.AddTransient<EmailSender>();
            services.AddTransient<SmsSender>();

            // Load CORS origins
            var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS");
            if (!string.IsNullOrEmpty(corsOrigins))
            {
                services.AddCors(options =>
                {
                    options.AddDefaultPolicy(builder =>
                    {
                        builder.WithOrigins(corsOrigins.Split(';'));
                        builder.AllowAnyMethod();
                        builder.AllowAnyHeader();
                        builder.AllowCredentials();
                    });
                });
            }

            services.AddControllers();
        }

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