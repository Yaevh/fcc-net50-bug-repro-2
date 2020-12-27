using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Szlem.AspNetCore.Options
{
    public class JwtOptions
    {
        public string Secret { get; set; }
        public NodaTime.Duration TokenLifetime { get; set; }
    }
}
