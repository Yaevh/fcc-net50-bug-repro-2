using EventFlow.Aggregates;
using EventFlow.ReadStores;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.SchoolManagement.Impl.Events;

namespace Szlem.SchoolManagement.Impl
{
    internal class SchoolReadModel : IReadModel,
        IAmReadModelFor<SchoolAggregate, SchoolId, SchoolRegistered>,
        IAmReadModelFor<SchoolAggregate, SchoolId, InitialAgreementAchieved>,
        IAmReadModelFor<SchoolAggregate, SchoolId, PermanentAgreementSigned>,
        IAmReadModelFor<SchoolAggregate, SchoolId, FixedTermAgreementSigned>,
        IAmReadModelFor<SchoolAggregate, SchoolId, SchoolResignedFromCooperation>
    {
        public SchoolId Id { get; private set; }
        public string Name { get; private set; }
        public string City { get; private set; }
        public string Address { get; private set; }
        
        private GetSchools.SchoolStatus _status = GetSchools.SchoolStatus.Unknown;
        private NodaTime.LocalDate? _agreementEndDate;
        private NodaTime.LocalDate? _resignationEndDate;

        public void Apply(IReadModelContext context, IDomainEvent<SchoolAggregate, SchoolId, SchoolRegistered> domainEvent)
        {
            Id = domainEvent.AggregateIdentity;
            Name = domainEvent.AggregateEvent.Name;
            Address = domainEvent.AggregateEvent.Address;
            City = domainEvent.AggregateEvent.City;
        }

        public void Apply(IReadModelContext context, IDomainEvent<SchoolAggregate, SchoolId, InitialAgreementAchieved> domainEvent)
        {
            _status = GetSchools.SchoolStatus.HasAgreedInitially;
        }

        public void Apply(IReadModelContext context, IDomainEvent<SchoolAggregate, SchoolId, PermanentAgreementSigned> domainEvent)
        {
            _status = GetSchools.SchoolStatus.HasSignedAgreement;
            _agreementEndDate = null;
        }

        public void Apply(IReadModelContext context, IDomainEvent<SchoolAggregate, SchoolId, FixedTermAgreementSigned> domainEvent)
        {
            _status = GetSchools.SchoolStatus.HasSignedAgreement;
            _agreementEndDate = domainEvent.AggregateEvent.AgreementEndDate;
        }

        public void Apply(IReadModelContext context, IDomainEvent<SchoolAggregate, SchoolId, SchoolResignedFromCooperation> domainEvent)
        {
            _status = GetSchools.SchoolStatus.HasResigned;
            _resignationEndDate = domainEvent.AggregateEvent.PotentialNextContactDate;
        }

        public GetSchools.SchoolStatus GetEffectiveStatus(NodaTime.LocalDate today)
        {
            return _status switch {
                GetSchools.SchoolStatus.Unknown => GetSchools.SchoolStatus.Unknown,
                GetSchools.SchoolStatus.HasAgreedInitially => GetSchools.SchoolStatus.HasAgreedInitially,
                GetSchools.SchoolStatus.HasResigned => HasResignedEffectively(today) ? GetSchools.SchoolStatus.HasResigned : GetSchools.SchoolStatus.Unknown,
                GetSchools.SchoolStatus.HasSignedAgreement => IsAgreementEffective(today) ? GetSchools.SchoolStatus.HasSignedAgreement : GetSchools.SchoolStatus.Unknown,
                _ => GetSchools.SchoolStatus.Unknown
            };
        }

        public bool HasResignedEffectively(NodaTime.LocalDate today)
        {
            if (_status != GetSchools.SchoolStatus.HasResigned)
                return false;
            return _resignationEndDate.HasValue == false || _resignationEndDate > today;
        }

        public bool IsAgreementEffective(NodaTime.LocalDate today)
        {
            if (_status != GetSchools.SchoolStatus.HasSignedAgreement)
                return false;
            return _agreementEndDate.HasValue == false || _agreementEndDate > today;
        }
    }
}
