// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System;
using System.Collections.Generic;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Chabloom.Accounts.Backend.Data
{
    public class AccountsDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public AccountsDbContext(DbContextOptions<AccountsDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var users = new List<ApplicationUser>
            {
                new()
                {
                    Id = Guid.Parse("421bde72-5f81-451b-83b6-08d8d3b98c06"),
                    UserName = "mdcasey@chabloom.com",
                    NormalizedUserName = "MDCASEY@CHABLOOM.COM",
                    Email = "mdcasey@chabloom.com",
                    NormalizedEmail = "MDCASEY@CHABLOOM.COM",
                    EmailConfirmed = true,
                    PhoneNumber = "+18036179564",
                    PhoneNumberConfirmed = true,
                    PasswordHash =
                        "AQAAAAEAACcQAAAAELYyWQtU3cVbIfdmk4LHrtYsKTiYVW7OAge27lolZ3I8D97OE4QQ6Yn4XwGhO8YPuQ==",
                    SecurityStamp = "C3KZM3I2WQCCAD7EVHRZQSGRFRX5MY3I",
                    ConcurrencyStamp = "3934a9f3-2a09-41c5-8d62-900007cb3a3f"
                }
            };

            modelBuilder.Entity<ApplicationUser>()
                .HasData(users);

            var userClaims = new List<IdentityUserClaim<Guid>>
            {
                new()
                {
                    Id = 1,
                    UserId = users[0].Id,
                    ClaimType = JwtClaimTypes.Name,
                    ClaimValue = "Matthew Casey"
                }
            };

            modelBuilder.Entity<IdentityUserClaim<Guid>>()
                .HasData(userClaims);
        }
    }
}