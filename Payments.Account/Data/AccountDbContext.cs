// Copyright 2020 Chabloom LC. All rights reserved.

using System;
using Microsoft.EntityFrameworkCore;

namespace Payments.Account.Data
{
    public class AccountDbContext : DbContext
    {
        public AccountDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}