using FluentAssertions;
using Fluid;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Szlem.Recruitment.Tests.Enrollments
{
    public class FluidTests
    {
        [Fact]
        public void DirectModelTest()
        {
            var model = new { Firstname = "Jacek", Lastname = "Sasin" };
            var source = "Hello {{ Firstname }} {{ Lastname }}";

            if (FluidTemplate.TryParse(source, out var template))
            {
                var context = new TemplateContext();
                foreach (var property in model.GetType().GetProperties())
                    context.SetValue(property.Name, property.GetValue(model));
                var result = template.Render(context);

                result.Should().Be("Hello Jacek Sasin");
            }
        }
    }
}
