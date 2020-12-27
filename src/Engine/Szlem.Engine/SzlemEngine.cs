using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.SharedKernel;

namespace Szlem.Engine
{
    public class SzlemEngine : ISzlemEngine
    {
        private readonly ISender _mediator;

        public SzlemEngine(ISender mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public Task<TResult> Query<TResult>(IRequest<TResult> request) => Query(request, CancellationToken.None);

        public async Task<TResult> Query<TResult>(IRequest<TResult> request, CancellationToken token = default)
        {
            return await _mediator.Send(request);
        }

        [DisplayName("Processing command {0}")]
        public Task Execute(IRequest request) => Execute(request, CancellationToken.None);

        [DisplayName("Processing command {0}")]
        public async Task Execute(IRequest request, CancellationToken token = default)
        {
            await _mediator.Send(request);
        }

        [DisplayName("Processing command {0}")]
        public Task<TResponse> Execute<TResponse>(IRequest<TResponse> request) => Execute(request, CancellationToken.None);

        [DisplayName("Processing command {0}")]
        public async Task<TResponse> Execute<TResponse>(IRequest<TResponse> request, CancellationToken token = default)
        {
            return await _mediator.Send(request, token);
        }
    }
}
