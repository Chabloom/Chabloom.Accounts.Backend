// Copyright 2020 Chabloom LC. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Chabloom.Accounts.ViewModels
{
    public class SignOutViewModel
    {
        [Required]
        public string Id { get; set; }

        public string PostLogoutRedirectUri { get; set; }
    }
}