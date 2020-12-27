using System;
using System.Collections.Generic;
using System.Linq;

namespace MockDataGenerator
{
    public class FullNameGenerator
    {
        private static readonly string[] PotentialNames = new[] {
            "Mateusz Morawiecki",
            "Beata Szydło",
            "Janusz Korwin Mikke",
            "Łukasz Stanisławowski",
            "Andrzej Gołota",
            "Maryla Rodowicz"
        };

        public string GetName()
        {
            return PotentialNames[new Random().Next(PotentialNames.Length)];
        }

        public IEnumerable<string> GetNames(int count)
        {
            return Enumerable.Repeat(new object(), count).Select(x => GetName());
        }

        public IEnumerable<string> GetNames(int min, int max)
        {
            return GetNames(new Random().Next(min, max + 1));
        }
    }
}
