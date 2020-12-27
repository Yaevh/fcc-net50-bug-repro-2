using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Szlem.Recruitment.Impl.Entities;

namespace Szlem.Recruitment.Impl.Repositories
{
    internal interface ICampaignRepository
    {
        Task<IReadOnlyCollection<Entities.Campaign>> GetAll();
        Task<Entities.Campaign> GetById(int id);
        Task<IReadOnlyCollection<Entities.Campaign>> GetByEditionId(int editionId);

        Task<int> Insert(Entities.Campaign campaign);
        Task Update(Entities.Campaign campaign);
    }

    class CampaignRepository : ICampaignRepository, IDisposable
    {
        private readonly ISession _session;

        public CampaignRepository(DbSessionProvider dbConfiguration)
        {
            if (dbConfiguration is null)
                throw new ArgumentNullException(nameof(dbConfiguration));
            _session = dbConfiguration.CreateSession();
        }


        public async Task<IReadOnlyCollection<Entities.Campaign>> GetAll()
        {
            return (await _session.QueryOver<Entities.Campaign>()
                .ListAsync()).ToArray();
        }

        public async Task<Entities.Campaign> GetById(int id)
        {
            return await _session.QueryOver<Entities.Campaign>()
                .Where(x => x.Id == id)
                .SingleOrDefaultAsync();
        }

        public async Task<IReadOnlyCollection<Entities.Campaign>> GetByEditionId(int editionId)
        {
            return (await _session.QueryOver<Entities.Campaign>()
                .Where(x => x.EditionId == editionId)
                .ListAsync()).ToArray();
        }


        public async Task<int> Insert(Entities.Campaign campaign)
        {
            return (int)await _session.SaveAsync(campaign);
        }

        public async Task Update(Entities.Campaign campaign)
        {
            await _session.UpdateAsync(campaign);
        }

        public void Dispose()
        {
            _session.Dispose();
        }
    }
}
