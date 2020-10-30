// Copyright 2020 Chabloom LC. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Chabloom.Accounts.Data;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
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

            // Get the public address for the current environment
            var accountsPublicAddress = System.Environment.GetEnvironmentVariable("ACCOUNTS_PUBLIC_ADDRESS");
            var paymentsPublicAddress = System.Environment.GetEnvironmentVariable("PAYMENTS_PUBLIC_ADDRESS");
            var processingPublicAddress = System.Environment.GetEnvironmentVariable("PROCESSING_PUBLIC_ADDRESS");
            if (string.IsNullOrEmpty(accountsPublicAddress) ||
                string.IsNullOrEmpty(paymentsPublicAddress) ||
                string.IsNullOrEmpty(processingPublicAddress))
            {
                accountsPublicAddress = "http://localhost:3000";
                paymentsPublicAddress = "http://localhost:3001";
                processingPublicAddress = "http://localhost:3002";
            }

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddIdentityServer(options =>
                {
                    options.UserInteraction.ErrorUrl = $"{accountsPublicAddress}/error";
                    options.UserInteraction.LoginUrl = $"{accountsPublicAddress}/signIn";
                    options.UserInteraction.LogoutUrl = $"{accountsPublicAddress}/signOut";
                })
                .AddConfigurationStore(options => options.ConfigureDbContext = x =>
                    x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                        y => y.MigrationsAssembly("Chabloom.Accounts")))
                .AddOperationalStore(options => options.ConfigureDbContext = x =>
                    x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                        y => y.MigrationsAssembly("Chabloom.Accounts")))
                .AddDeveloperSigningCredential()
                .AddAspNetIdentity<ApplicationUser>();

            // Get CORS origins
            var corsOrigins = new List<string>
            {
                accountsPublicAddress,
                paymentsPublicAddress,
                processingPublicAddress
            };
            // Add development origins if required
            if (Environment.IsDevelopment())
            {
                corsOrigins.Add("http://localhost:3000");
                corsOrigins.Add("http://localhost:3001");
                corsOrigins.Add("http://localhost:3002");
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
                app.UseDatabaseErrorPage();
            }

            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                if (!context.Clients.Any())
                {
                    // Create initial clients
                    var clients = new List<Client>
                    {
                        new Client
                        {
                            ClientId = "Chabloom.Payments.Backend",
                            ClientName = "Chabloom.Payments.Backend",
                            AllowedGrantTypes = GrantTypes.ClientCredentials,
                            AllowedScopes = new List<string>
                            {
                                "Chabloom.Processing",
                                "Chabloom.Processing.IPC"
                            },
                            ClientSecrets =
                            {
                                new Secret("fs*5bfL53%xUR9KhoQAc*Tg3&vd42bz&".Sha256())
                            }
                        },
                        new Client
                        {
                            ClientId = "Chabloom.Payments.Frontend",
                            ClientName = "Chabloom.Payments.Frontend",
                            AllowedGrantTypes = GrantTypes.Code,
                            AllowedScopes = new List<string>
                            {
                                "openid",
                                "profile",
                                "Chabloom.Payments"
                            },
                            RedirectUris = new List<string>
                            {
                                "http://localhost:3001/signin-oidc",
                                "https://payments-test.chabloom.com/signin-oidc"
                            },
                            PostLogoutRedirectUris = new List<string>
                            {
                                "http://localhost:3001/signout-oidc",
                                "https://payments-test.chabloom.com/signout-oidc"
                            },
                            RequireConsent = false,
                            RequireClientSecret = false,
                            RequirePkce = true
                        },
                        new Client
                        {
                            ClientId = "Chabloom.Payments.Native",
                            ClientName = "Chabloom.Payments.Native",
                            AllowedGrantTypes = GrantTypes.Code,
                            AllowedScopes = new List<string>
                            {
                                "openid",
                                "profile",
                                "Chabloom.Payments"
                            },
                            RedirectUris = new List<string>
                            {
                                "com.chabloom.payments:/callback"
                            },
                            PostLogoutRedirectUris = new List<string>
                            {
                                "com.chabloom.payments:/callback"
                            },
                            RequireConsent = false,
                            RequireClientSecret = false,
                            RequirePkce = true
                        },
                        new Client
                        {
                            ClientId = "Chabloom.Processing.Frontend",
                            ClientName = "Chabloom.Processing.Frontend",
                            AllowedGrantTypes = GrantTypes.Code,
                            AllowedScopes = new List<string>
                            {
                                "openid",
                                "profile",
                                "Chabloom.Processing"
                            },
                            RedirectUris = new List<string>
                            {
                                "http://localhost:3002/signin-oidc",
                                "https://processing-test.chabloom.com/signin-oidc"
                            },
                            PostLogoutRedirectUris = new List<string>
                            {
                                "http://localhost:3002/signout-oidc",
                                "https://processing-test.chabloom.com/signout-oidc"
                            },
                            RequireConsent = false,
                            RequireClientSecret = false,
                            RequirePkce = true
                        },
                    };
                    // Convert client models to entities
                    var clientEntities = clients
                        .Select(client => client.ToEntity())
                        .ToList();
                    // Add client entities to database
                    context.Clients.AddRange(clientEntities);
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    // Create initial identity resources
                    var identityResources = new List<IdentityResource>
                    {
                        new IdentityResources.OpenId(),
                        new IdentityResources.Profile()
                    };
                    // Convert identity resource models to entities
                    var identityResourceEntities = identityResources
                        .Select(resource => resource.ToEntity())
                        .ToList();
                    // Add identity resource entities to database
                    context.IdentityResources.AddRange(identityResourceEntities);
                    context.SaveChanges();
                }

                if (!context.ApiScopes.Any())
                {
                    // Create initial API scopes
                    var apiScopes = new List<ApiScope>
                    {
                        new ApiScope("Chabloom.Payments"),
                        new ApiScope("Chabloom.Payments.IPC"),
                        new ApiScope("Chabloom.Processing"),
                        new ApiScope("Chabloom.Processing.IPC")
                    };
                    // Convert API scope models to entities
                    var apiScopeEntities = apiScopes
                        .Select(resource => resource.ToEntity())
                        .ToList();
                    // Add API scope entities to database
                    context.ApiScopes.AddRange(apiScopeEntities);
                    context.SaveChanges();
                }

                if (!context.ApiResources.Any())
                {
                    // Create initial API resources
                    var apiResources = new List<ApiResource>
                    {
                        new ApiResource("Chabloom.Payments")
                        {
                            Scopes = {"Chabloom.Payments"}
                        },
                        new ApiResource("Chabloom.Payments.IPC")
                        {
                            Scopes = {"Chabloom.Payments.IPC"}
                        },
                        new ApiResource("Chabloom.Processing")
                        {
                            Scopes = {"Chabloom.Processing"}
                        },
                        new ApiResource("Chabloom.Processing.IPC")
                        {
                            Scopes = {"Chabloom.Processing.IPC"}
                        }
                    };
                    // Convert API resource models to entities
                    var apiResourceEntities = apiResources
                        .Select(resource => resource.ToEntity())
                        .ToList();
                    // Add API resource entities to database
                    context.ApiResources.AddRange(apiResourceEntities);
                    context.SaveChanges();
                }
            }

            app.UseCors();

            app.UseIdentityServer();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}