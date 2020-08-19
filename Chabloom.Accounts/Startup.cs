// Copyright 2020 Chabloom LC. All rights reserved.

using System;
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
using Microsoft.OpenApi.Models;

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
            var frontendPublicAddress = System.Environment.GetEnvironmentVariable("FRONTEND_PUBLIC_ADDRESS");
            var jwtPublicAddress = System.Environment.GetEnvironmentVariable("JWT_PUBLIC_ADDRESS");
            if (string.IsNullOrEmpty(frontendPublicAddress) ||
                string.IsNullOrEmpty(jwtPublicAddress))
            {
                frontendPublicAddress = "http://localhost:3000";
                jwtPublicAddress = "https://localhost:44303";
            }

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddIdentityServer(options =>
                {
                    options.UserInteraction.LoginUrl = $"{frontendPublicAddress}/login";
                    options.UserInteraction.LogoutUrl = $"{frontendPublicAddress}/logout";
                })
                .AddConfigurationStore(options => options.ConfigureDbContext = x =>
                    x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                        y => y.MigrationsAssembly("Chabloom.Accounts")))
                .AddOperationalStore(options => options.ConfigureDbContext = x =>
                    x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                        y => y.MigrationsAssembly("Chabloom.Accounts")))
                .AddDeveloperSigningCredential()
                .AddAspNetIdentity<ApplicationUser>();

            services.AddControllers();

            if (Environment.IsDevelopment())
            {
                // Setup development CORS
                services.AddCors(options =>
                {
                    options.AddPolicy("Development",
                        builder =>
                        {
                            builder.WithOrigins(frontendPublicAddress);
                            builder.WithOrigins("http://localhost:3001");
                            builder.AllowAnyMethod();
                            builder.AllowAnyHeader();
                            builder.AllowCredentials();
                        });
                });

                // Setup generated OpenAPI documentation
                services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "Chabloom Accounts",
                        Description = "Chabloom Accounts v1 API",
                        Version = "v1"
                    });
                    options.AddSecurityDefinition("openid", new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OpenIdConnect,
                        OpenIdConnectUrl = new Uri($"{jwtPublicAddress}/.well-known/openid-configuration")
                    });
                });
            }
            else
            {
                // Setup production CORS
                services.AddCors(options =>
                {
                    options.AddPolicy("Production",
                        builder =>
                        {
                            builder.WithOrigins(frontendPublicAddress);
                            builder.AllowAnyMethod();
                            builder.AllowAnyHeader();
                            builder.AllowCredentials();
                        });
                });
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseCors("Development");
                app.UseSwagger(options => options.RouteTemplate = "/swagger/{documentName}/chabloom-accounts-api.json");
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/chabloom-accounts-api.json", "Chabloom Accounts v1 API");
                });
            }
            else
            {
                app.UseCors("Production");
            }

            app.UseIdentityServer();

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
                                "http://localhost:3000/signin-oidc",
                                "http://localhost:3001/signin-oidc"
                            },
                            PostLogoutRedirectUris = new List<string>
                            {
                                "http://localhost:3000/signout-oidc",
                                "http://localhost:3001/signout-oidc"
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
                                "http://localhost:3000/signin-oidc",
                                "http://localhost:3001/signin-oidc"
                            },
                            PostLogoutRedirectUris = new List<string>
                            {
                                "http://localhost:3000/signout-oidc",
                                "http://localhost:3001/signout-oidc"
                            },
                            RequireConsent = false,
                            RequireClientSecret = false,
                            RequirePkce = true
                        },
                        new Client
                        {
                            ClientId = "Chabloom.Payments.Postman",
                            ClientName = "Chabloom.Payments.Postman",
                            AllowedGrantTypes = GrantTypes.Code,
                            AllowedScopes = new List<string>
                            {
                                "openid",
                                "profile",
                                "Chabloom.Payments"
                            },
                            RedirectUris = new List<string>
                            {
                                "https://oauth.pstmn.io/v1/callback"
                            },
                            PostLogoutRedirectUris = new List<string>
                            {
                                "https://oauth.pstmn.io/v1/callback"
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
                        new ApiScope("Chabloom.Payments")
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

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}