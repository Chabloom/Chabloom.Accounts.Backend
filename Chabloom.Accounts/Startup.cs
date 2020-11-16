// Copyright 2020 Chabloom LC. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Chabloom.Accounts.Data;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
            var accountsBackendAddress = System.Environment.GetEnvironmentVariable("ACCOUNTS_BACKEND_ADDRESS");
            var paymentsPublicAddress = System.Environment.GetEnvironmentVariable("PAYMENTS_PUBLIC_ADDRESS");
            if (string.IsNullOrEmpty(accountsBackendAddress) ||
                string.IsNullOrEmpty(paymentsPublicAddress))
            {
                accountsBackendAddress = "http://localhost:5001";
                paymentsPublicAddress = "http://localhost:3001";
            }

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddIdentityServer(options =>
                {
                    options.UserInteraction.ErrorUrl = $"{paymentsPublicAddress}/Accounts/Error";
                    options.UserInteraction.LoginUrl = $"{paymentsPublicAddress}/Accounts/SignIn";
                    options.UserInteraction.LogoutUrl = $"{paymentsPublicAddress}/Accounts/SignOut";
                })
                .AddConfigurationStore(options => options.ConfigureDbContext = x =>
                    x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                        y => y.MigrationsAssembly("Chabloom.Accounts")))
                .AddOperationalStore(options => options.ConfigureDbContext = x =>
                    x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                        y => y.MigrationsAssembly("Chabloom.Accounts")))
                .AddDeveloperSigningCredential()
                .AddAspNetIdentity<ApplicationUser>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = accountsBackendAddress;
                    options.Audience = "Chabloom.Accounts";
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiScope", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", "Chabloom.Accounts");
                });
                options.AddPolicy("IpcScope", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", "Chabloom.Accounts.IPC");
                });
            });

            // Get CORS origins
            var corsOrigins = new List<string>
            {
                paymentsPublicAddress
            };
            // Add development origins if required
            if (Environment.IsDevelopment())
            {
                corsOrigins.Add("http://localhost:3000");
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
                                "Chabloom.Accounts",
                                "Chabloom.Accounts.IPC",
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
                                "Chabloom.Accounts",
                                "Chabloom.Payments",
                                "Chabloom.Processing"
                            },
                            RedirectUris = new List<string>
                            {
                                "http://localhost:3000/signin-oidc",
                                "https://payments-test.chabloom.com/signin-oidc"
                            },
                            PostLogoutRedirectUris = new List<string>
                            {
                                "http://localhost:3000/signout-oidc",
                                "https://payments-test.chabloom.com/signout-oidc"
                            },
                            RequireConsent = false,
                            RequireClientSecret = false,
                            RequirePkce = true
                        }
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
                        new ApiScope("Chabloom.Accounts"),
                        new ApiScope("Chabloom.Accounts.IPC"),
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
                        new ApiResource("Chabloom.Accounts")
                        {
                            Scopes = {"Chabloom.Accounts"}
                        },
                        new ApiResource("Chabloom.Accounts.IPC")
                        {
                            Scopes = {"Chabloom.Accounts.IPC"}
                        },
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers().RequireAuthorization("ApiScope"); });
        }
    }
}