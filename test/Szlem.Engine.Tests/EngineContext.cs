using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Persistence.EF;
using Szlem.SharedKernel;

namespace Szlem.Engine.Tests
{
    public class EngineContext : IDisposable
    {
        public Guid Guid { get; }
        public ISzlemEngine Engine { get; }
        public AppDbContext DbContext { get; }

        private readonly string _dbPath;
        private readonly IServiceProvider _serviceProvider;

        public EngineContext(Guid guid, string dbPath, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(dbPath))
                throw new ArgumentException($"No {nameof(dbPath)} provided, cannot connect to DB", nameof(dbPath));

            Guid = guid;
            _dbPath = dbPath;
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            Engine = _serviceProvider.GetRequiredService<ISzlemEngine>();
            DbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        }

        public void Dispose()
        {
            System.IO.File.Delete(_dbPath);
        }
    }
}
