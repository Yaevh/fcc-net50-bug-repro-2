using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Szlem.AspNetCore.Pages.Shared.Components.RegistrationForm;

namespace Szlem.AspNetCore.Areas.Recruitment.Pages
{
    [AllowAnonymous]
    public class RecruitmentFormSubmittedModel : PageModel
    {
        public const string PageName = "RecruitmentFormSubmitted";


        [BindProperty]
        public RegistrationFormInputModel Input { get; set; }

        public void OnGet(string email)
        {
            if (Input == null)
                Input = new RegistrationFormInputModel();
            Input.Email = email;
        }
    }
}
