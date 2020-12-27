using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Szlem.Domain;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Enrollments
{
    public static class SubmitRecruitmentForm
    {
        [AllowAnonymous]
        public class Command : IRequest<Result<Nothing, Error>>
        {
            [Display(Name = "Imię")]
            public string FirstName { get; set; }
            [Display(Name = "Nazwisko")]
            public string LastName { get; set; }

            [Display(Name = "Adres e-mail")]
            public EmailAddress Email { get; set; }
            [Display(Name = "Numer telefonu")]
            public PhoneNumber PhoneNumber { get; set; }

            [Display(Name = "Napisz trochę o sobie: kim jesteś, czym się zajmujesz, skąd dowiedziałeś się o projekcie, dlaczego Cię zainteresował, czego oczekujesz od projektu, czy agmażujesz się w jakąś działalność społeczną (jeśli tak, to jaką?) itp.")]
            public string AboutMe { get; set; }

            [Display(Name = "Region/województwo")]
            public string Region { get; set; }

            [Display(Name = "Miasta w których możesz prowadzić zajęcia (oddzielone przecinkami)", Prompt = "np. Gdańsk, Sopot, Gdynia")]
            public IReadOnlyCollection<string> PreferredLecturingCities { get; set; } = Array.Empty<string>();

            [Display(Name = "Wybierz szkolenia, w których możesz wziąć udział")]
            public IReadOnlyCollection<int> PreferredTrainingIds { get; set; }

            [Display(Name = "Czy wyrażasz zgodę na przetwarzanie Twoich danych osobowych? Dane będą przetwarzane do potrzeb procesu rekrutacji i uczestnictwa w projekcie. Podanie danych jest dobrowolne, ale konieczne do uczestnictwa w projekcie.")]
            public bool GdprConsentGiven { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.FirstName).NotEmpty().WithMessage(SubmitRecruitmentForm_ErrorMessages.FirstNameIsRequired);
                RuleFor(x => x.LastName).NotEmpty().WithMessage(SubmitRecruitmentForm_ErrorMessages.LastNameIsRequired);

                RuleFor(x => x.Email).NotEmpty().WithMessage(SubmitRecruitmentForm_ErrorMessages.EmailIsRequired);
                RuleFor(x => x.Email).EmailAddress().WithMessage(SubmitRecruitmentForm_ErrorMessages.Invalid_email_address);
                RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage(SubmitRecruitmentForm_ErrorMessages.PhoneNumberIsRequired);

                RuleFor(x => x.AboutMe).NotEmpty().WithMessage(SubmitRecruitmentForm_ErrorMessages.AboutMeIsRequired);

                RuleFor(x => x.Region).NotEmpty().WithMessage(SubmitRecruitmentForm_ErrorMessages.RegionIsRequired);
                RuleFor(x => x.PreferredLecturingCities).NotEmpty().WithMessage(SubmitRecruitmentForm_ErrorMessages.PreferredLecturingCities_must_be_specified);
                RuleFor(x => x.PreferredLecturingCities).Must(x => x.HasDuplicates() == false).WithMessage(SubmitRecruitmentForm_ErrorMessages.PreferredLecturingCities_cannot_have_duplicates);
                RuleFor(x => x.PreferredTrainingIds).NotEmpty().WithMessage(SubmitRecruitmentForm_ErrorMessages.PreferredTrainingsMustBeSpecified);
                RuleFor(x => x.PreferredTrainingIds).Must(x => x.Distinct().Count() == x.Count).WithMessage(SubmitRecruitmentForm_ErrorMessages.DuplicatePreferredTrainingsWereSpecified);

                RuleFor(x => x.GdprConsentGiven).Must(x => x == true).WithMessage(SubmitRecruitmentForm_ErrorMessages.GdprConsentIsRequired);
            }
        }
    }
}
