using CSharpFunctionalExtensions;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Szlem.Recruitment.Impl.Entities;

namespace Szlem.Recruitment.Impl.Repositories
{
    internal interface ITrainingRepository
    {
        IQueryable<Training> Query();
        Task<IReadOnlyCollection<Training>> GetByIds(IReadOnlyCollection<int> ids);
        Task<Maybe<Training>> GetById(int id);

        Task<int> Insert(Training training);
        Task Update(Training training);
    }

    internal class TrainingRepository : ITrainingRepository
    {
        private readonly ISession _session;

        public TrainingRepository(DbSessionProvider dbConfiguration)
        {
            if (dbConfiguration is null)
                throw new ArgumentNullException(nameof(dbConfiguration));
            _session = dbConfiguration.CreateSession();
        }

        public IQueryable<Training> Query()
        {
            return _session.Query<Training>();
        }

        public async Task<IReadOnlyCollection<Training>> GetByIds(IReadOnlyCollection<int> ids)
        {
            return (await _session.QueryOver<Training>()
                .WhereRestrictionOn(x => x.ID).IsIn(ids.ToArray())
                .ListAsync()).ToArray();
        }
        
        public async Task<Maybe<Training>> GetById(int id)
        {
            return await _session.QueryOver<Training>()
                .Fetch(SelectMode.Fetch, x => x.Notes)
                .Where(x => x.ID == id)
                .SingleOrDefaultAsync();
        }

        public async Task<int> Insert(Entities.Training training)
        {
            using (var transaction = _session.BeginTransaction()) // has to be inside a transaction to properly save children
            {
                var id = (int)await _session.SaveAsync(training);
                await transaction.CommitAsync();
                return id;
            }
        }

        public async Task Update(Training training)
        {
            using (var transaction = _session.BeginTransaction()) // has to be inside a transaction to properly save children
            {
                await _session.UpdateAsync(training);
                await transaction.CommitAsync();
            }
        }
    }
}
