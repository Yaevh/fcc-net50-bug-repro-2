using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Szlem.AspNetCore
{
    public static class RouteValueDictionaryExtensions
    {
        public static RouteValueDictionary WithValue(this RouteValueDictionary routeValues, string key, object value)
        {
            var result = new RouteValueDictionary(routeValues);
            result[key] = value;
            return result;
        }

        public static RouteValueDictionary WithPrefix(this RouteValueDictionary routeValues, string prefix)
        {
            Guard.Against.NullOrWhiteSpace(prefix, nameof(prefix));
            var result = new RouteValueDictionary();
            foreach (var entry in routeValues)
                result[$"{prefix}.{entry.Key}"] = entry.Value;
            return result;
        }
    }
}
