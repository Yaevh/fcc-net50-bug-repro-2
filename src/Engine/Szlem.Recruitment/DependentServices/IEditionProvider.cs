using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Szlem.Recruitment.DependentServices
{
    public interface IEditionProvider
    {
        Task<Maybe<EditionDetails>> GetEdition(int editionId);
    }

    public class EditionDetails
    {
        public int Id { get; set; }
        public NodaTime.Instant StartDateTime { get; set; }
        public NodaTime.Instant EndDateTime { get; set; }
    }
}
