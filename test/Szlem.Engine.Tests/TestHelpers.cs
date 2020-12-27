using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Szlem.Engine.Tests
{
    public static class TestHelpers
    {
        public static async Task RunWithEngineContextFromTestDb(Func<EngineContext, Task> test, Action<IServiceCollection> configureServices = null)
        {
            using (var context = await new EngineContextBuilder().BuildContextFromTestDatabase(configureServices))
            {
                await test(context);
            }
        }
    }
}
