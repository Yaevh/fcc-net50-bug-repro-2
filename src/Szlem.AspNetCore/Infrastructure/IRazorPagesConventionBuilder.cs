using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Szlem.AspNetCore.Infrastructure
{
    public interface IRazorPagesConventionBuilder
    {
        void Configure(PageConventionCollection conventions);
    }
}
