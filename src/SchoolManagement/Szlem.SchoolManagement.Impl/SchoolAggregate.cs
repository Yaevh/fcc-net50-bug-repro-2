using Ardalis.GuardClauses;
using Ardalis.SmartEnum;
using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.Core;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Domain.Exceptions;
using Szlem.Models.Users;
using Szlem.SchoolManagement.Impl.Events;
using Szlem.SharedKernel;

#nullable enable

namespace Szlem.SchoolManagement.Impl
{
    internal class SchoolId : Identity<SchoolId>
    {
        public SchoolId(string value) : base(value) { }
    }

    [AggregateName("Szlem.SchoolManagement.School")]
    internal class SchoolAggregate : AggregateRoot<SchoolAggregate, SchoolId>
    {
        public SchoolAggregate(SchoolId id) : base(id) { }


        public string SchoolName { get; private set; } = string.Empty;
        public string Address { get; private set; } = string.Empty;
        public string City { get; private set; } = string.Empty;
        public IReadOnlyCollection<ContactData> ContactData { get; private set; } = Array.Empty<ContactData>();

        private readonly IList<Note> _notes = new List<Note>();
        public IReadOnlyCollection<Note> Notes => _notes.ToList().AsReadOnly();

        public bool HasAgreedInitially { get; private set; }
        public bool HasSignedPermanentAgreement { get; private set; }
        private bool _hasSignedFixedTermAgreement;
        public LocalDate? AgreementEndDate { get; private set; }
        public bool HasResignedPermanently { get; private set; }
        private bool _hasResignedTemporarily;
        public LocalDate? ResignationEndDate { get; private set; }


        #region RegisterSchool
        public Result<Nothing, Error> RegisterSchool(Instant timestamp, RegisterSchool.Command command, ApplicationUser registeringUser)
        {
            Guard.Against.Default(timestamp, nameof(timestamp));
            Guard.Against.Null(command, nameof(command));

            return Validate(new RegisterSchool.Validator(), command)
                .Tap(() => Emit(new SchoolRegistered(
                    timestamp: timestamp, registeringUserId: registeringUser.Id,
                    name: command.Name, city: command.City, address: command.Address,
                    contactData: command.ContactData ?? Array.Empty<ContactData>())));
        }

        protected void Apply(SchoolRegistered e)
        {
            SchoolName = e.Name;
            Address = e.Address;
            City = e.City;
            ContactData = e.ContactData;
        }
        #endregion

        #region RecordContact
        public Result<Nothing, Error> RecordContact(RecordContact.Command command, ApplicationUser recordingUser, Instant now)
        {
            Guard.Against.Null(command, nameof(command));
            Guard.Against.Null(recordingUser, nameof(recordingUser));
            Guard.Against.Default(now, nameof(now));
            ValidateIdMatchOrThrow(command.SchoolId);

            return Validate(new RecordContact.Validator(), command)
                .Ensure(
                    _ => this.IsNew == false,
                    new Error.ResourceNotFound(Messages.School_not_found))
                .Ensure(
                    _ => now > command.ContactTimestamp,
                    new Error.DomainError(RecordContact_Messages.Contact_timestamp_cannot_be_later_than_current_timestamp))
                .Tap(
                    _ => Emit(new ContactOccured(
                        recordingUserId: recordingUser.Id,
                        contactTimestamp: command.ContactTimestamp,
                        communicationChannel: command.CommunicationChannel,
                        emailAddress: command.CommunicationChannel.IsEmail ? command.EmailAddress : null,
                        phoneNumber: command.CommunicationChannel.IsPhone ? command.PhoneNumber : null,
                        contactPersonName: command.ContactPersonName,
                        content: command.Content, additionalNotes: command.AdditionalNotes))
                );
        }

        protected void Apply(ContactOccured e) { }
        #endregion

        #region RecordInitialAgreement
        public Result<Nothing, Error> RecordInitialAgreement(Instant now, RecordInitialAgreement.Command command, ApplicationUser recordingUser)
        {
            Guard.Against.Null(command, nameof(command));
            Guard.Against.Null(recordingUser, nameof(recordingUser));
            ValidateIdMatchOrThrow(command.SchoolId);

            return Validate(new RecordInitialAgreement.Validator(), command)
                .Ensure(
                    _ => this.IsNew == false,
                    new Error.ResourceNotFound(Messages.School_not_found))
                .Tap(
                    _ => Emit(new InitialAgreementAchieved(
                        recordingUserId: recordingUser.Id,
                        agreeingPersonName: command.AgreeingPersonName,
                        additionalNotes: command.AdditionalNotes))
                );
        }

        protected void Apply(InitialAgreementAchieved e)
        {
            ResetAggregateState();
            HasAgreedInitially = true;
        }
        #endregion

        #region RecordAgreementSigned
        public Result<Guid, Error> RecordAgreementSigned(RecordAgreementSigned.Command command, ApplicationUser recordingUser, LocalDate today)
        {
            Guard.Against.Null(command, nameof(command));
            Guard.Against.Null(recordingUser, nameof(recordingUser));
            ValidateIdMatchOrThrow(command.SchoolId);

            var agreementId = Guid.NewGuid();

            return Validate(new RecordAgreementSigned.Validator(), command)
                .Ensure(
                    _ => this.IsNew == false,
                    new Error.ResourceNotFound(Messages.School_not_found))
                .Ensure(
                    _ => command.AgreementEndDate == null ||  command.AgreementEndDate > today,
                    new Error.DomainError(RecordAgreementSigned_Messages.AgreementEndDate_must_be_in_the_future))
                .TapIf(
                    command.AgreementEndDate.HasValue == false,
                    () => Emit(new PermanentAgreementSigned(
                        id: agreementId,
                        scannedDocument: command.ScannedDocument,
                        scannedDocumentExtension: command.ScannedDocumentExtension,
                        scannedDocumentContentType: command.ScannedDocumentContentType,
                        recordingUserId: recordingUser.Id,
                        additionalNotes: command.AdditionalNotes))
                )
                .TapIf(
                    command.AgreementEndDate.HasValue,
                    () => Emit(new FixedTermAgreementSigned(
                        id: agreementId,
                        scannedDocument: command.ScannedDocument,
                        scannedDocumentExtension: command.ScannedDocumentExtension,
                        scannedDocumentContentType: command.ScannedDocumentContentType,
                        agreementEndDate: command.AgreementEndDate ?? throw new InvalidOperationException($"{command.AgreementEndDate} has no value"),
                        recordingUserId: recordingUser.Id,
                        additionalNotes: command.AdditionalNotes))
                )
                .Map(_ => agreementId);
        }

        protected void Apply(PermanentAgreementSigned e)
        {
            ResetAggregateState();
            HasSignedPermanentAgreement = true;
        }

        protected void Apply(FixedTermAgreementSigned e)
        {
            ResetAggregateState();
            _hasSignedFixedTermAgreement = true;
            AgreementEndDate = e.AgreementEndDate;
        }

        public bool HasSignedFixedTermAgreement(LocalDate today)
        {
            if (_hasSignedFixedTermAgreement && AgreementEndDate is null)
                throw new ApplicationException($"invalid aggregate state: {nameof(SchoolAggregate)}");
            return _hasSignedFixedTermAgreement && AgreementEndDate.HasValue && today <= AgreementEndDate.Value;
        }
        #endregion

        #region RecordResignation
        public Result<Nothing, Error> RecordResignation(RecordResignation.Command command, ApplicationUser recordingUser, LocalDate now)
        {
            Guard.Against.Null(command, nameof(command));
            Guard.Against.Null(recordingUser, nameof(recordingUser));
            ValidateIdMatchOrThrow(command.SchoolId);

            return Validate(new RecordResignation.Validator(), command)
                .Ensure(
                    _ => this.IsNew == false,
                    new Error.ResourceNotFound(Messages.School_not_found))
                .Ensure(
                    _ => command.PotentialNextContactDate == null || command.PotentialNextContactDate > now,
                    new Error.DomainError(RecordResignation_Messages.PotentialNextContactDate_must_be_later_than_today))
                .Tap(
                    () => Emit(new SchoolResignedFromCooperation(
                        recordingUserId: recordingUser.Id,
                        potentialNextContactDate: command.PotentialNextContactDate,
                        additionalNotes: command.AdditionalNotes ?? string.Empty))
                );
        }

        protected void Apply(SchoolResignedFromCooperation e)
        {
            ResetAggregateState();
            if (e.PotentialNextContactDate is null)
            {
                HasResignedPermanently = true;
            }
            else
            {
                if (e.PotentialNextContactDate is null)
                    throw new ApplicationException($"invalid aggregate state: {nameof(SchoolAggregate)}");
                _hasResignedTemporarily = true;
                ResignationEndDate = e.PotentialNextContactDate.Value;
            }
        }

        public bool HasResignedTemporarily(LocalDate today)
        {
            if (_hasResignedTemporarily && ResignationEndDate is null)
                throw new ApplicationException($"invalid aggregate state: {nameof(SchoolAggregate)}");
            return _hasResignedTemporarily && ResignationEndDate.HasValue && today <= ResignationEndDate.Value;
        }
        #endregion

        #region Notes

        public Result<Guid, Error> AddNote(AddNote.Command command, ApplicationUser author, Instant now)
        {
            Guard.Against.Null(command, nameof(command));
            Guard.Against.Null(author, nameof(author));
            Guard.Against.Default(now, nameof(now));
            ValidateIdMatchOrThrow(command.SchoolId);

            System.Diagnostics.Debug.Assert(command.Content != null);

            return Validate(new AddNote.Validator(), command)
                .Ensure(
                    _ => this.IsNew == false,
                    new Error.ResourceNotFound(Messages.School_not_found))
                .Map(
                    _ => {
                        var noteId = Guid.NewGuid();
                        Emit(new NoteAdded(
                            timestamp: now,
                            noteId: noteId,
                            authorId: author.Id,
                            content: command.Content));
                        return noteId;
                    }
                );
        }

        public Result<Nothing, Error> DeleteNote(DeleteNote.Command command, ApplicationUser deletingUser, Instant now)
        {
            Guard.Against.Null(command, nameof(command));
            Guard.Against.Null(deletingUser, nameof(deletingUser));
            Guard.Against.Default(now, nameof(now));
            ValidateIdMatchOrThrow(command.SchoolId);

            return Validate(new DeleteNote.Validator(), command)
                .Ensure(
                    _ => this.IsNew == false,
                    new Error.ResourceNotFound(Messages.School_not_found))
                .Ensure(
                    _ => this.Notes.Any(x => x.NoteId == command.NoteId),
                    new Error.DomainError(DeleteNote_Messages.Note_does_not_exist)
                )
                .Tap(
                    _ => Emit(new NoteDeleted(
                        timestamp: now,
                        noteId: command.NoteId,
                        deletingUserId: deletingUser.Id))
                );
        }

        public Result<Nothing, Error> EditNote(EditNote.Command command, ApplicationUser editingUser, Instant now)
        {
            Guard.Against.Null(command, nameof(command));
            Guard.Against.Null(editingUser, nameof(editingUser));
            Guard.Against.Default(now, nameof(now));
            ValidateIdMatchOrThrow(command.SchoolId);
            return Validate(new EditNote.Validator(), command)
                .Ensure(
                    _ => this.IsNew == false,
                    new Error.ResourceNotFound(Messages.School_not_found))
                .Ensure(
                    _ => this.Notes.Any(x => x.NoteId == command.NoteId),
                    new Error.DomainError(DeleteNote_Messages.Note_does_not_exist)
                )
                .Tap(
                    _ => Emit(new NoteEdited(
                        timestamp: now,
                        noteId: command.NoteId,
                        editingUserId: editingUser.Id,
                        content: command.Content))
                );
        }

        protected void Apply(NoteAdded e)
        {
            _notes.Add(new Note(e.NoteId, e.AuthorId, e.Timestamp, e.Content));
        }

        protected void Apply(NoteDeleted e)
        {
            _notes.Remove(_notes.Single(x => x.NoteId == e.NoteId));
        }

        protected void Apply(NoteEdited e)
        {
            var note = _notes.Single(x => x.NoteId == e.NoteId);
            note.Content = e.Content;
            note.LastEditTimestamp = e.Timestamp;
        }

        #endregion

        #region supporting code
        private Result<Nothing, Error> Validate<T>(FluentValidation.IValidator<T> validator, T instance)
        {
            Guard.Against.Null(instance, nameof(instance));
            var result = validator.Validate(instance);
            if (result.IsValid)
                return Result.Success<Nothing, Error>(Nothing.Value);
            else
                return Result.Failure<Nothing, Error>(new Error.ValidationFailed(result));
        }

        /// <summary>
        /// Validates whether given GUID matches this aggregate's ID
        /// </summary>
        /// <param name="guid"></param>
        private void ValidateIdMatchOrThrow(Guid guid)
        {
            if (SchoolId.With(guid) != this.Id)
                throw new AggregateMismatchException($"ID mismatch in {nameof(SchoolAggregate)}; expected {Id.GetGuid()}, got {guid}");
        }

        private void ResetAggregateState()
        {
            HasAgreedInitially = false;
            HasSignedPermanentAgreement = false;
            _hasSignedFixedTermAgreement = false;
            AgreementEndDate = null;
            HasResignedPermanently = _hasResignedTemporarily = false;
            ResignationEndDate = null;
        }
        #endregion
    }
}
