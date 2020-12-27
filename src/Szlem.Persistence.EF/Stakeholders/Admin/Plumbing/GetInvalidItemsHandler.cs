using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Persistence.EF;

namespace Szlem.Engine.Stakeholders.Admin.Plumbing
{
    internal class GetInvalidItemsHandler : IRequestHandler<GetInvalidItems.Query, IEnumerable<GetInvalidItems.InvalidEntitiesGroup>>
    {
        private readonly AppDbContext _dbContext;
        private readonly IValidatorFactory _validatorFactory;

        public GetInvalidItemsHandler(AppDbContext dbContext, IValidatorFactory validatorFactory)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _validatorFactory = validatorFactory ?? throw new ArgumentNullException(nameof(validatorFactory));
        }

        public async Task<IEnumerable<GetInvalidItems.InvalidEntitiesGroup>> Handle(GetInvalidItems.Query request, CancellationToken cancellationToken)
        {
            var results = new List<GetInvalidItems.InvalidEntity>();

            var asNoTrackingMethodPrototype = typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions)
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(x => x.Name == nameof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking))
                .OrderBy(x => x.GetParameters().Length)
                .First();

            foreach (var entityType in _dbContext.Model.GetEntityTypes())
            {
                var validator = _validatorFactory.GetValidator(entityType.ClrType);
                if (validator == null)
                    continue;

                var dbSet = typeof(AppDbContext)
                    .GetMethod("Set")
                    .MakeGenericMethod(new[] { entityType.ClrType })
                    .Invoke(_dbContext, new object[0]);
                var queryMethod = asNoTrackingMethodPrototype.MakeGenericMethod(new[] { entityType.ClrType });
                var query = queryMethod.Invoke(null, new object[] { dbSet }) as System.Collections.IEnumerable;

                foreach (var entity in query)
                {
                    var result = await validator.ValidateAsync(new ValidationContext<object>(entity), cancellationToken);
                    if (result.IsValid == false)
                        results.Add(new GetInvalidItems.InvalidEntity() { Entity = entity, Result = result });
                }
            }
            
            return results
                .SelectMany(x => x.Result.Errors.Select(y => new { Data = x, Error = y }))
                .GroupBy(x => new GetInvalidItems.ErrorDescriptor() { EntityType = x.Data.Entity.GetType(), PropertyName = x.Error.PropertyName, ErrorMessage = x.Error.ErrorMessage }, x => x.Data)
                .Select(x => new GetInvalidItems.InvalidEntitiesGroup(x.Key, x))
                .ToList();
        }
    }
}
