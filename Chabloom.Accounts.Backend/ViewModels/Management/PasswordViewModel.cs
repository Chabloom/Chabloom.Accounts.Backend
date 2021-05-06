// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System;
using System.ComponentModel.DataAnnotations;

namespace Chabloom.Accounts.Backend.ViewModels.Management
{
    public class PasswordViewModel
    {
        [Required]
        public Guid Id { get; set; }

        public string Token { get; set; }

        [StringLength(255, MinimumLength = 8)]
        public string CurrentPassword { get; set; }

        [StringLength(255, MinimumLength = 8)]
        public string NewPassword { get; set; }
    }
}