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
                .AddConfigurationStore(options => options.ConfigureDbContext = x =>
                    x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                        y => y.MigrationsAssembly("Chabloom.Accounts")))
                .AddOperationalStore(options => options.ConfigureDbContext = x =>
                    x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                        y => y.MigrationsAssembly("Chabloom.Accounts")))
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