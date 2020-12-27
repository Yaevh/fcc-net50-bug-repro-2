using EventFlow.ReadStores.InMemory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Szlem.SchoolManagement.Impl
{
    internal interface ISchoolRepository
    {
        Task<IReadOnlyCollection<SchoolReadModel>> Get(Predicate<SchoolReadModel> predicate, CancellationToken token);
        Task<IReadOnlyCollection<SchoolReadModel>> GetAll(CancellationToken token);
    }

    internal class SchoolRepository : ISchoolRepository
    {
        private readonly IInMemoryReadStore<SchoolReadModel> _readStore;

        public SchoolRepository(IInMemoryReadStore<SchoolReadModel> readStore)
        {
            _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
        }

        public Task<IReadOnlyCollection<SchoolReadModel>> Get(Predicate<SchoolReadModel> predicate, CancellationToken token)
        {
            return _readStore.FindAsync(predicate, token);
        }

        public Task<IReadOnlyCollection<SchoolReadModel>> GetAll(CancellationToken token) => Get(x => true, token);
    }
}
