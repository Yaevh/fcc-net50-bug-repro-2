using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Szlem.SharedKernel;

namespace Szlem.Engine.Stakeholders.Admin.Plumbing
{
    public static class GetInvalidItems
    {
        [Authorize(AuthorizationPolicies.AdminOnly)]
        public class Query : IRequest<IEnumerable<InvalidEntitiesGroup>>
        {

        }




        public class InvalidEntitiesGroup : IGrouping<ErrorDescriptor, InvalidEntity>
        {
            private readonly List<InvalidEntity> _impl;

            public ErrorDescriptor Key { get; }

            public InvalidEntitiesGroup(ErrorDescriptor key, IEnumerable<InvalidEntity> entites)
            {
                Key = key ?? throw new ArgumentNullException(nameof(key));
                _impl = entites?.ToList() ?? throw new ArgumentNullException(nameof(entites));
            }

            public IEnumerator<InvalidEntity> GetEnumerator() => _impl.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _impl.GetEnumerator();
        }

        public class ErrorDescriptor : IEquatable<ErrorDescriptor>
        {
            public Type EntityType { get; set; }
            public string PropertyName { get; set; }
            public string ErrorMessage { get; set; }
            
            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;
                return Equals(obj as ErrorDescriptor);
            }
            
            public bool Equals(ErrorDescriptor other)
            {
                return other != null &&
                       EqualityComparer<Type>.Default.Equals(EntityType, other.EntityType) &&
                       PropertyName == other.PropertyName &&
                       ErrorMessage == other.ErrorMessage;
            }

            public override int GetHashCode()
            {
                var hashCode = 2090498693;
                hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(EntityType);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PropertyName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ErrorMessage);
                return hashCode;
            }

            public static bool operator ==(ErrorDescriptor descriptor1, ErrorDescriptor descriptor2)
            {
                return EqualityComparer<ErrorDescriptor>.Default.Equals(descriptor1, descriptor2);
            }

            public static bool operator !=(ErrorDescriptor descriptor1, ErrorDescriptor descriptor2)
            {
                return !(descriptor1 == descriptor2);
            }
        }

        public class InvalidEntity
        {
            public object Entity { get; set; }

            public ValidationResult Result { get; set; }

            public override string ToString()
            {
                return $"{Entity} - {Result.Errors.Count} errors";
            }
        }
    }
}
