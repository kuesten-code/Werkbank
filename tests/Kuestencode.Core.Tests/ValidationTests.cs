using FluentAssertions;
using Kuestencode.Core.Validation;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Kuestencode.Core.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData("DE89370400440532013000", true)]
    [InlineData("DE12345678901234567890", true)]
    [InlineData("DE1234567890123456789", false)] // Too short
    [InlineData("DE123456789012345678901", false)] // Too long
    [InlineData("FR89370400440532013000", false)] // Wrong country
    [InlineData("", true)] // Empty is valid (Required handles this)
    [InlineData(null, true)]
    public void IbanAttribute_ValidatesCorrectly(string? iban, bool expected)
    {
        // Arrange
        var attribute = new IbanAttribute();
        var context = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult(iban, context);

        // Assert
        (result == ValidationResult.Success).Should().Be(expected);
    }

    [Theory]
    [InlineData("12345", true)]
    [InlineData("00001", true)]
    [InlineData("99999", true)]
    [InlineData("1234", false)] // Too short
    [InlineData("123456", false)] // Too long
    [InlineData("ABCDE", false)] // Not numeric
    [InlineData("", true)] // Empty is valid
    [InlineData(null, true)]
    public void GermanPostalCodeAttribute_ValidatesCorrectly(string? postalCode, bool expected)
    {
        // Arrange
        var attribute = new GermanPostalCodeAttribute();
        var context = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult(postalCode, context);

        // Assert
        (result == ValidationResult.Success).Should().Be(expected);
    }

    [Theory]
    [InlineData("Max Mustermann", true)]
    [InlineData("Anna Maria Schmidt", true)]
    [InlineData("Max", false)] // No space
    [InlineData("Mustermann", false)] // No space
    [InlineData(" Max ", false)] // Space but no last name
    [InlineData("", true)] // Empty is valid
    [InlineData(null, true)]
    public void FullNameAttribute_ValidatesCorrectly(string? name, bool expected)
    {
        // Arrange
        var attribute = new FullNameAttribute();
        var context = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult(name, context);

        // Assert
        (result == ValidationResult.Success).Should().Be(expected);
    }

    [Theory]
    [InlineData("K00001", true)]
    [InlineData("K12345", true)]
    [InlineData("K99999", true)]
    [InlineData("K0001", false)] // Too short
    [InlineData("K000001", false)] // Too long
    [InlineData("A00001", false)] // Wrong prefix
    [InlineData("00001", false)] // No prefix
    [InlineData("", false)] // Empty not valid for customer number
    [InlineData(null, false)]
    public void CustomerNumberAttribute_ValidatesCorrectly(string? number, bool expected)
    {
        // Arrange
        var attribute = new CustomerNumberAttribute();
        var context = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult(number, context);

        // Assert
        (result == ValidationResult.Success).Should().Be(expected);
    }

    [Fact]
    public void CustomerNumberAttribute_GenerateNext_ReturnsCorrectFormat()
    {
        // Act & Assert
        CustomerNumberAttribute.GenerateNext(0).Should().Be("K00001");
        CustomerNumberAttribute.GenerateNext(1).Should().Be("K00002");
        CustomerNumberAttribute.GenerateNext(99).Should().Be("K00100");
        CustomerNumberAttribute.GenerateNext(99999).Should().Be("K100000");
    }

    [Fact]
    public void IbanAttribute_Format_FormatsCorrectly()
    {
        // Act & Assert
        IbanAttribute.Format("DE89370400440532013000").Should().Be("DE89 3704 0044 0532 0130 00");
        IbanAttribute.Format("DE12345678901234567890").Should().Be("DE12 3456 7890 1234 5678 90");
        IbanAttribute.Format("").Should().Be("");
    }
}
