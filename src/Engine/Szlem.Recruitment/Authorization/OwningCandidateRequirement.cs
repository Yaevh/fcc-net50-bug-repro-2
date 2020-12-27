using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Recruitment.Authorization
{
    /// <summary>
    /// Wymaga, aby użytkownik był kandydatem i posiadaczem danego zgłoszenia
    /// </summary>
    public class OwningCandidateRequirement : IAuthorizationRequirement
    {

    }
}
