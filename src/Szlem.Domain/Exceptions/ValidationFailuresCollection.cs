using Ardalis.GuardClauses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Szlem.Domain.Exceptions
{
    [JsonConverter(typeof(ValidationFailuresCollection.JsonConverter))]
    public class ValidationFailuresCollection : IReadOnlyCollection<ValidationFailure>, IEnumerable<ValidationFailure>
    {
        private readonly Dictionary<string, ValidationFailure> _impl = new Dictionary<string, ValidationFailure>();


        public ValidationFailuresCollection() : this(Enumerable.Empty<ValidationFailure>()) { }

        public ValidationFailuresCollection(IEnumerable<ValidationFailure> failures)
        {
            Guard.Against.Null(failures, nameof(failures));

            foreach (var failure in failures)
                _impl[failure.PropertyName] = failure;
        }


        public IReadOnlyCollection<string> this[string key] { get => _impl[key].Errors; }

        public IEnumerable<string> Properties => _impl.Keys;

        public int Count => _impl.Count;

        public bool ContainsProperty(string property)
        {
            return _impl.ContainsKey(property);
        }

        public bool TryGetValue(string key, out IReadOnlyCollection<string> value)
        {
            value = null;
            if (this.ContainsProperty(key) == false)
                return false;

            value = this[key];
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<ValidationFailure>).GetEnumerator();
        }

        IEnumerator<ValidationFailure> IEnumerable<ValidationFailure>.GetEnumerator()
        {
            return _impl.Values.GetEnumerator();
        }

        public bool Contains(ValidationFailure item)
        {
            return _impl.Values.Contains(item);
        }


        /// <summary>
        /// for tests and internal purposes only, to be able to use collection initializer
        /// </summary>
        /// <param name="item"></param>
        internal void Add(ValidationFailure item)
        {
            _impl[item.PropertyName] = item;
        }


        public class JsonConverter : JsonConverter<ValidationFailuresCollection>
        {
            public override ValidationFailuresCollection ReadJson(JsonReader reader, Type objectType, ValidationFailuresCollection existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var dict = serializer.Deserialize<IDictionary<string, string[]>>(reader);
                return new ValidationFailuresCollection(dict.Select(x => new ValidationFailure(x.Key, x.Value)));
            }

            public override void WriteJson(JsonWriter writer, ValidationFailuresCollection value, JsonSerializer serializer)
            {
                var dict = value.ToDictionary(x => x.PropertyName, x => x.Errors);
                serializer.Serialize(writer, dict);
            }
        }
    }
}
