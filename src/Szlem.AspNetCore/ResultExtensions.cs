using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Szlem.AspNetCore
{
    public static class ResultExtensions
    {
        public static IActionResult MatchToActionResult<T>(this Result<T, Szlem.Domain.Error> result, Func<T, IActionResult> onSuccess)
        {
            return result.Match(
                v => onSuccess(result.Value),
                e => new ActionResultHelper().GetActionResultFor(e)
            );
        }
    }
}
