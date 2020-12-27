using Fluid;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Engine.Infrastructure
{
    public interface IFluidTemplateRenderer
    {
        string Render<T>(string template, T model);
    }

    public class FluidTemplateRenderer : IFluidTemplateRenderer
    {
        public string Render<T>(string template, T model)
        {
            var fluidTemplate = FluidTemplate.Parse(template);
            var context = new TemplateContext();
            context.MemberAccessStrategy = new UnsafeMemberAccessStrategy() { IgnoreCasing = true };
            context.MemberAccessStrategy.Register(model.GetType()); // Allows any public property of the model to be used
            foreach (var property in model.GetType().GetProperties())
                context.SetValue(property.Name, property.GetValue(model));

            return fluidTemplate.Render(context);
        }

        private class UnsafeMemberAccessStrategy : IMemberAccessStrategy
        {
            public MemberNameStrategy MemberNameStrategy { get; set; } = MemberNameStrategies.Default;
            private readonly MemberAccessStrategy baseMemberAccessStrategy = new MemberAccessStrategy();
            public bool IgnoreCasing { get; set; }

            public IMemberAccessor GetAccessor(Type type, string name)
            {
                var accessor = baseMemberAccessStrategy.GetAccessor(type, name);
                if (accessor != null)
                    return accessor;

                baseMemberAccessStrategy.Register(type, name);
                return baseMemberAccessStrategy.GetAccessor(type, name);
            }
            public void Register(Type type, string name, IMemberAccessor getter)
            {
                baseMemberAccessStrategy.Register(type, name, getter);
            }
        }
    }
}
