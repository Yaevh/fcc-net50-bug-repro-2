using EventFlow;
using EventFlow.Extensions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.DependencyInjection.AspNetCore;

namespace Szlem.SchoolManagement.Impl
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSzlemSchoolManagementImpl(this IServiceCollection services)
        {
            var interfaceAssemblyMarker = typeof(Szlem.SchoolManagement.Marker);
            var implAssemblyMarker = typeof(Szlem.SchoolManagement.Impl.Marker);

            services.AddAsyncInitializer<Initializer>();

            foreach (var entry in FluentValidation.AssemblyScanner.FindValidatorsInAssemblyContaining(interfaceAssemblyMarker))
                services.AddTransient(entry.InterfaceType, entry.ValidatorType);
            foreach (var entry in FluentValidation.AssemblyScanner.FindValidatorsInAssemblyContaining(implAssemblyMarker))
                services.AddTransient(entry.InterfaceType, entry.ValidatorType);

            return services
                .AddMediatR(implAssemblyMarker)
                .AddEventsFrom(implAssemblyMarker.Assembly)
                .AddEventSubscribersFrom(implAssemblyMarker.Assembly)
                .AddSingleton<ISchoolRepository, SchoolRepository>();
        }

        public static IEventFlowOptions ConfigureSzlemSchoolManagement(this IEventFlowOptions options)
        {
            return options
                .UseInMemoryReadStoreFor<SchoolReadModel>();
        }
    }
}
