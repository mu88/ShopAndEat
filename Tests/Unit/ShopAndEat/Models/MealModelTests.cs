using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using ShopAndEat.Models;

namespace Tests.Unit.ShopAndEat.Models;

[TestFixture]
[Category("Unit")]
public class MealModelTests
{
    [Test]
    public void Defaults_WhenNotConfigured_HasEmptyRecipeAndMealTypeNames()
    {
        // Arrange
        var testee = new MealModel();

        // Assert
        testee.RecipeName.Should().BeEmpty();
        testee.MealTypeName.Should().BeEmpty();
    }

    [Test]
    public void TodayOrFutureValidator_WithDateBeforeInjectedTimeProviderToday_ShouldFail()
    {
        // Arrange
        var timeProvider = new FixedTimeProvider(new DateTime(2026, 3, 15));
        var model = new MealModel { Date = new DateTime(2026, 3, 14) };
        var context = CreateValidationContext(model, timeProvider);

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(model.Date, context, results);

        // Assert
        isValid.Should().BeFalse();
        results.Should().ContainSingle(result => result.ErrorMessage == "Must be today or in future");
    }

    [Test]
    public void TodayOrFutureValidator_WithInjectedTimeProviderToday_ShouldSucceed()
    {
        // Arrange
        var timeProvider = new FixedTimeProvider(new DateTime(2026, 3, 15));
        var model = new MealModel { Date = new DateTime(2026, 3, 15) };
        var context = CreateValidationContext(model, timeProvider);

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(model.Date, context, results);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Test]
    public void TodayOrFutureValidator_WithDateAfterInjectedTimeProviderToday_ShouldSucceed()
    {
        // Arrange
        var timeProvider = new FixedTimeProvider(new DateTime(2026, 3, 15));
        var model = new MealModel { Date = new DateTime(2026, 3, 16) };
        var context = CreateValidationContext(model, timeProvider);

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(model.Date, context, results);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Test]
    public void TodayOrFutureValidator_WithoutRegisteredTimeProvider_ShouldFallBackToSystemTimeProvider()
    {
        // Arrange - no TimeProvider registered in the service provider, so the attribute must fall
        // back to TimeProvider.System rather than throwing.
        var model = new MealModel { Date = DateTime.Now.AddYears(10) };
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TimeProvider)).Returns((object?)null);
        var context = new ValidationContext(model, serviceProvider, null) { MemberName = nameof(MealModel.Date) };

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(model.Date, context, results);

        // Assert
        isValid.Should().BeTrue();
    }

    [Test]
    public void TodayOrFutureValidator_WithNonDateTimeValue_ShouldFail()
    {
        // Arrange - bypass Validator.TryValidateProperty's type enforcement to exercise the
        // attribute's own defensive "value is DateTime" pattern-match guard directly.
        var timeProvider = new FixedTimeProvider(new DateTime(2026, 3, 15));
        var model = new MealModel { Date = new DateTime(2026, 3, 15) };
        var context = CreateValidationContext(model, timeProvider);
        var attribute = new TodayOrFutureValidatorAttribute();

        // Act
        var result = attribute.GetValidationResult("not a date", context);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Must be today or in future");
    }

    private static ValidationContext CreateValidationContext(MealModel model, TimeProvider timeProvider)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TimeProvider)).Returns(timeProvider);

        return new ValidationContext(model, serviceProvider, null) { MemberName = nameof(MealModel.Date) };
    }

    private sealed class FixedTimeProvider(DateTime today) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => new(today, TimeSpan.Zero);
    }
}
