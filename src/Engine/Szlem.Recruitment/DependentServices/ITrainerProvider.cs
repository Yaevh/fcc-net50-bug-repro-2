using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Szlem.Recruitment.DependentServices
{
    public interface ITrainerProvider
    {
        Task<Maybe<TrainerDetails>> GetTrainerDetails(Guid guid);
        Task<IReadOnlyCollection<TrainerDetails>> GetTrainerDetails(IReadOnlyCollection<Guid> guids);
    }

    public class TrainerDetails
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
    }
}
