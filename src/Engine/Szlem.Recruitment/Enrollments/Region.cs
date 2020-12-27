using Ardalis.SmartEnum;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Recruitment.Enrollments
{
    public class Region : SmartEnum<Region>
    {
        public static readonly Region Dolnośląskie = new Region("woj. dolnośląskie", 02);
        public static readonly Region KujawskoPomorskie = new Region("woj. kujawsko-pomorskie", 04);
        public static readonly Region Lubelskie = new Region("woj. lubelskie", 06);
        public static readonly Region Lubuskie = new Region("woj. lubuskie", 08);
        public static readonly Region Łódzkie = new Region("woj. łodzkie", 10);
        public static readonly Region Małopolskie = new Region("woj. małopolskie", 12);
        public static readonly Region Mazowieckie = new Region("woj. mazowieckie", 14);
        public static readonly Region Opolskie = new Region("woj. opolskie", 16);
        public static readonly Region Podkarpackie = new Region("woj. podkarpackie", 18);
        public static readonly Region Podlaskie = new Region("woj. podlaskie", 20);
        public static readonly Region Pomorskie = new Region("woj. pomorskie", 22);
        public static readonly Region Śląskie = new Region("woj. śląskie", 24);
        public static readonly Region Świętokrzyskie = new Region("woj. świętokrzyskie", 26);
        public static readonly Region WarmińskoMazurskie = new Region("woj. warmińsko-mazurskie", 28);
        public static readonly Region Wielkopolskie = new Region("woj. wielkopolskie", 30);
        public static readonly Region Zachodniopomorskie = new Region("woj. zachodniopomorskie", 32);


        private Region(string name, int value) : base(name, value) { }

    }
}
