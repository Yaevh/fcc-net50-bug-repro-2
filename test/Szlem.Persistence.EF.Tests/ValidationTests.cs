using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Models.Editions;
using Szlem.Models.Editions.Validators;
using Szlem.Persistence.EF;
using Xunit;
using Xunit.Abstractions;

public class ValidationTests
{
    private readonly ITestOutputHelper _output;

    public ValidationTests(ITestOutputHelper output)
    {
        _output = output;
    }


    [Fact]
    public async Task WhenSavingInvalidEntity_ThenValidationShouldFail()
    {
        var options = BuildDbOptions(nameof(WhenSavingInvalidEntity_ThenValidationShouldFail));

        var validatorFactory = new Mock<IValidatorFactory>();
        validatorFactory
            .Setup(x => x.GetValidator(typeof(Edition)))
            .Returns(new EditionValidator());

        using (var context = new AppDbContext(options, validatorFactory.Object))
        {
            var edition = new Edition()
            {
                StartDate = new DateTime(2018, 02, 02), EndDate = new DateTime(2018, 02, 01)
            };
            
            context.Set<Edition>().Add(edition);
            
            var ex = await Assert.ThrowsAsync<ValidationException>(() => context.SaveChangesAsync());
            
            Assert.Equal(2, ex.Errors.Count());

            var invalidProperties = ex.Errors.Select(x => x.PropertyName);
            Assert.Contains(nameof(edition.StartDate), invalidProperties);
            Assert.Contains(nameof(edition.StartDate), invalidProperties);
        }
    }

    [Fact]
    public async Task WhenSavingValidEntity_ThenValidationShouldBeRunAndSucceed()
    {
        var options = BuildDbOptions(nameof(WhenSavingValidEntity_ThenValidationShouldBeRunAndSucceed));
        
        var validatorFactoryMock = new Mock<IValidatorFactory>();
        validatorFactoryMock
            .Setup(x => x.GetValidator(typeof(Edition)))
            .Returns(new EditionValidator());

        using (var context = new AppDbContext(options, validatorFactoryMock.Object))
        {
            var edition20172018 = new Edition()
            {
                StartDate = DateTime.Parse("01.09.2017"),
                EndDate = DateTime.Parse("30.06.2018"),
                Name = "Rok szkolny 2017/2018"
            };
            context.Set<Edition>().Add(edition20172018);
            await context.SaveChangesAsync();

            validatorFactoryMock.Verify(x => x.GetValidator(typeof(Edition)), Times.AtLeastOnce());
        }
    }

    [Fact]
    public async Task WhenSavingEntity_DatabaseOnlyRulesetRulesShouldBeRun()
    {
        var options = BuildDbOptions(nameof(WhenSavingInvalidEntity_ThenValidationShouldFail));

        var validatorFactory = new Mock<IValidatorFactory>();
        validatorFactory
            .Setup(x => x.GetValidator(typeof(Edition)))
            .Returns(new FailingRulesetValidator());

        using (var context = new AppDbContext(options, validatorFactory.Object))
        {
            context.Set<Edition>().Add(new Edition());

            var ex = await Assert.ThrowsAsync<ValidationException>(() => context.SaveChangesAsync());

            Assert.Collection(
                ex.Errors,
                first => Assert.Equal(string.Empty, first.PropertyName));
        }
    }

    [Fact]
    public async Task WhenSpecifyingAllRulesets_AllRulesetsShouldBeRun()
    {
        
        var validatorFactory = new Mock<IValidatorFactory>();
        validatorFactory
            .Setup(x => x.GetValidator(typeof(Edition)))
            .Returns(new FailingRulesetValidator());

        var validator = new FailingRulesetValidator() as IValidator;
        var entity = new Edition();
        var validationContext = ValidationContext<object>.CreateWithOptions(entity, options => options.IncludeRuleSets(Szlem.Models.RuleSetNames.All));

        var result = await validator.ValidateAsync(validationContext, CancellationToken.None);

        Assert.False(result.IsValid);
    }

    // this validator should always fail when "DatabaseOnly" ruleset is selected; otherwise it should always succeed
    public class FailingRulesetValidator : AbstractValidator<Edition>
    {
        public FailingRulesetValidator()
        {
            RuleSet(Szlem.Models.RuleSetNames.DatabaseOnly, () =>
            {
                RuleFor(x => x).Must(x => false).WithMessage("expected failure");
            });
        }
    }


    private DbContextOptions<AppDbContext> BuildDbOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;
    }
}
