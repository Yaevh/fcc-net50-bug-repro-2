using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Szlem.Domain;

namespace Szlem.Models.Editions
{
    /// <summary>
    /// Edycja LEM - zamknięty okres czasowy, w którym prowadzone są zajęcia (np. rok szkolny 2017/2018)
    /// </summary>
    public class Edition
    {
        public virtual int ID { get; protected set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public virtual DateTime StartDate { get; set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public virtual DateTime EndDate { get; set; }
        
        public virtual string Name { get; set; }

        public virtual EditionStatistics ThisEditionStatistics { get; protected set; } = new EditionStatistics();

        public virtual EditionStatistics CumulativeStatistics { get; protected set; } = new EditionStatistics();
    }
}
