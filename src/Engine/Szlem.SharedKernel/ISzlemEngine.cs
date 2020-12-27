using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Szlem.SharedKernel
{
    public interface ISzlemEngine
    {
        Task<TResult> Query<TResult>(IRequest<TResult> request);
        Task<TResult> Query<TResult>(IRequest<TResult> request, CancellationToken token = default);
        Task Execute(IRequest request);
        Task Execute(IRequest request, CancellationToken token = default);
        Task<TResponse> Execute<TResponse>(IRequest<TResponse> request);
        Task<TResponse> Execute<TResponse>(IRequest<TResponse> request, CancellationToken token = default);
    }
}
