using Extensions.Hosting.AsyncInitialization;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Szlem.Engine.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Szlem.AspNetCore.WebApi.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Szlem.Recruitment.Tests")]

namespace Szlem.DependencyInjection.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterValidatorsFromAssembliesContaining(this IServiceCollection services, params Type[] types)
        {
            foreach (var type in types)
                foreach (var entry in FluentValidation.AssemblyScanner.FindValidatorsInAssemblyContaining(type))
                    services.AddTransient(entry.InterfaceType, entry.ValidatorType);
        }


        public static IServiceCollection Replace<TInterface, TImplementation>(this IServiceCollection services)
            where TImplementation : class, TInterface
            where TInterface : class
        {
            var existing = services.RemoveDescriptor<TInterface>();
            switch (existing.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    return services.AddSingleton<TInterface, TImplementation>();
                case ServiceLifetime.Scoped:
                    return services.AddScoped<TInterface, TImplementation>();
                case ServiceLifetime.Transient:
                    return services.AddTransient<TInterface, TImplementation>();
                default:
                    throw new NotSupportedException();
            }
        }

        public static IServiceCollection Remove<TInterface>(this IServiceCollection services)
        {
            services.RemoveDescriptor<TInterface>();
            return services;
        }

        public static IServiceCollection RemoveImplementation<TImplementation>(this IServiceCollection services)
        {
            return services.RemoveImplementation(typeof(TImplementation));
        }

        public static IServiceCollection RemoveImplementation(this IServiceCollection services, Type implementation)
        {
            var impl = services.Single(x => x.ImplementationType == implementation);
            services.Remove(impl);
            return services;
        }

        private static ServiceDescriptor RemoveDescriptor<TInterface>(this IServiceCollection services)
        {
            var descriptor = services.Single(x => x.ServiceType == typeof(TInterface));
            services.Remove(descriptor);
            return descriptor;
        }

        public static IServiceCollection AddEventInitializer(this IServiceCollection services)
        {
            return services.AddAsyncInitializer<EventInitializer>();
        }

        public static IServiceCollection AddEventsFrom(this IServiceCollection services, Assembly assembly)
        {
            return services.AddSingleton(new EventContainingAssembly(assembly));
        }

        public static IServiceCollection AddEventSubscribersFrom(this IServiceCollection services, Assembly assembly)
        {
            var interfaces = new Type[] {
                typeof(EventFlow.Subscribers.ISubscribeAsynchronousTo<,,>),
                typeof(EventFlow.Subscribers.ISubscribeSynchronousTo<,,>),
                typeof(EventFlow.Subscribers.ISubscribeSynchronousToAll)
            };

            foreach (var @interface in interfaces)
                AddTypesImplementingOpenGenericInterfaceAsScoped(services, assembly, @interface);
            return services;
        }

        private static void AddTypesImplementingOpenGenericInterfaceAsScoped(IServiceCollection services, Assembly assembly, Type openGenericInterface)
        {
            var implementations = assembly.DefinedTypes
                .Select(x => new {
                    concreteType = x,
                    interfaces = x.ImplementedInterfaces
                        .Where(y => y.IsGenericType && y.GetGenericTypeDefinition() == openGenericInterface).ToArray()
                })
                .Where(x => x.interfaces.Any())
                .ToArray();

            foreach (var implementation in implementations)
                foreach (var @interface in implementation.interfaces)
                    services.AddScoped(@interface, implementation.concreteType);
        }

        internal class EventInitializer : IAsyncInitializer
        {
            private readonly IEventDefinitionService _eventDefinitionService;
            private readonly IEnumerable<EventContainingAssembly> _eventAssemblies;

            public EventInitializer(IEventDefinitionService eventDefinitionService, IEnumerable<EventContainingAssembly> eventAssemblies)
            {
                _eventDefinitionService = eventDefinitionService ?? throw new ArgumentNullException(nameof(eventDefinitionService));
                _eventAssemblies = eventAssemblies ?? throw new ArgumentNullException(nameof(eventAssemblies));
            }

            public Task InitializeAsync()
            {
                var eventTypes = _eventAssemblies
                    .SelectMany(x => x.Assembly.GetTypes())
                    .Where(t => !t.GetTypeInfo().IsAbstract && typeof(IAggregateEvent).GetTypeInfo().IsAssignableFrom(t))
                    .ToArray();

                var unnamedEvents = eventTypes
                    .Where(t => t.GetCustomAttribute<EventVersionAttribute>() == null)
                    .ToArray();
                if (unnamedEvents.Any())
                {
                    var eventNames = string.Join("\n", unnamedEvents.Select(t => t.FullName));
                    throw new ApplicationException($"Following events do not have {nameof(EventVersionAttribute)} applied to them:\n{eventNames}");
                }

                var duplicateEvents = eventTypes
                    .GroupBy(x => ExtractEventName(x))
                    .Where(x => x.Count() > 1)
                    .ToArray();
                if (duplicateEvents.Any())
                {
                    var messages = duplicateEvents.Select(x => $"name {x.Key} is used by events: {string.Join(", ", x.Select(y => y.FullName))}");
                    throw new ApplicationException($"Ambiguous events found:\n{string.Join("\n", messages)}");
                }

                _eventDefinitionService.Load(eventTypes);

                return Task.CompletedTask;
            }

            private string ExtractEventName(Type t)
            {
                if (typeof(IAggregateEvent).IsAssignableFrom(t) == false)
                    throw new ArgumentException($"{nameof(t)} must be an {nameof(IAggregateEvent)}");
                var attribute = t.GetCustomAttribute<EventVersionAttribute>();
                return attribute?.Name ?? t.Name;
            }
        }

        internal class EventContainingAssembly
        {
            public Assembly Assembly { get; }
            public EventContainingAssembly(Assembly assembly) => Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }
    }
}
