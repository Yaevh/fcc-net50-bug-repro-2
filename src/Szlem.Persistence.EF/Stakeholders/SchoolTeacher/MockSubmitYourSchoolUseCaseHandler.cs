using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Szlem.Engine.Stakeholders.SchoolTeacher.SubmitYourSchoolUseCase;

namespace Szlem.Engine.Stakeholders.SchoolTeacher.Schools
{
    internal class MockSubmitYourSchoolUseCaseHandler : IRequestHandler<SubmitYourSchoolUseCase.Command, SubmitYourSchoolUseCase.Result>
    {
        public Task<SubmitYourSchoolUseCase.Result> Handle(SubmitYourSchoolUseCase.Command request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SubmitYourSchoolUseCase.Result() { SchoolID = Guid.NewGuid() });
        }
    }
}
