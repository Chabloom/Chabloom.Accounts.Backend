// Copyright 2020-2021 Chabloom LC. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Chabloom.Accounts.Backend.ViewModels
{
    public class SignOutViewModel
    {
        [Required]
        public string Id { get; set; }

        public string PostLogoutRedirectUri { get; set; }
    }
}