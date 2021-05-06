// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System;
using System.ComponentModel.DataAnnotations;

namespace Chabloom.Accounts.Backend.ViewModels.Management
{
    public class AccountViewModel
    {
        [Required]
        public Guid Id { get; set; }

        public string Name { get; set; }

        [MaxLength(255)]
        public string Email { get; set; }

        public string PhoneNumber { get; set; }
    }
}