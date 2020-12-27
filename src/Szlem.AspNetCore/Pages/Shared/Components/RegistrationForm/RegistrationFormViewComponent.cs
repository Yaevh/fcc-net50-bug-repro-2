using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Szlem.Models.Users;

namespace Szlem.AspNetCore.Pages.Shared.Components.RegistrationForm
{
    public class RegistrationFormViewComponent : ViewComponent
    {
        [BindProperty]
        public RegistrationFormInputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        private readonly SignInManager<ApplicationUser> _signInManager;

        public RegistrationFormViewComponent(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        }


        public async Task<IViewComponentResult> InvokeAsync(RegistrationFormInputModel input, string returnUrl)
        {
            Input = input;
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            return View(this);
        }
    }
}
