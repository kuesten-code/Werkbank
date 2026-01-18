using FluentAssertions;
using Kuestencode.Core.Models;
using Kuestencode.Core.Enums;
using Xunit;

namespace Kuestencode.Core.Tests;

public class ModelTests
{
    [Fact]
    public void Company_DisplayName_ReturnsBusinessNameWhenSet()
    {
        // Arrange
        var company = new Company
        {
            OwnerFullName = "Max Mustermann",
            BusinessName = "IT-Solutions GmbH"
        };

        // Act & Assert
        company.DisplayName.Should().Be("IT-Solutions GmbH");
    }

    [Fact]
    public void Company_DisplayName_ReturnsOwnerNameWhenNoBusinessName()
    {
        // Arrange
        var company = new Company
        {
            OwnerFullName = "Max Mustermann",
            BusinessName = null
        };

        // Act & Assert
        company.DisplayName.Should().Be("Max Mustermann");
    }

    [Fact]
    public void Company_IsEmailConfigured_ReturnsTrueWhenComplete()
    {
        // Arrange
        var company = new Company
        {
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            SmtpUsername = "user",
            SmtpPassword = "pass",
            EmailSenderEmail = "sender@example.com"
        };

        // Act & Assert
        company.IsEmailConfigured().Should().BeTrue();
    }

    [Fact]
    public void Company_IsEmailConfigured_ReturnsFalseWhenIncomplete()
    {
        // Arrange
        var company = new Company
        {
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            SmtpUsername = "user",
            // Missing password and sender email
        };

        // Act & Assert
        company.IsEmailConfigured().Should().BeFalse();
    }

    [Fact]
    public void Company_HasRequiredData_ReturnsTrueWhenComplete()
    {
        // Arrange
        var company = new Company
        {
            OwnerFullName = "Max Mustermann",
            Address = "Musterstraße 1",
            PostalCode = "12345",
            City = "Musterstadt",
            TaxNumber = "12/345/67890",
            Email = "max@example.com",
            BankName = "Sparkasse",
            BankAccount = "DE89370400440532013000"
        };

        // Act & Assert
        company.HasRequiredData().Should().BeTrue();
    }

    [Fact]
    public void Company_GetFormattedAddress_FormatsCorrectly()
    {
        // Arrange
        var company = new Company
        {
            Address = "Musterstraße 1",
            PostalCode = "12345",
            City = "Musterstadt",
            Country = "Deutschland"
        };

        // Act
        var address = company.GetFormattedAddress();

        // Assert
        address.Should().Be("Musterstraße 1\n12345 Musterstadt\nDeutschland");
    }

    [Fact]
    public void Customer_GetFormattedAddress_FormatsCorrectly()
    {
        // Arrange
        var customer = new Customer
        {
            Address = "Kundenstraße 5",
            PostalCode = "54321",
            City = "Kundenort",
            Country = "Deutschland"
        };

        // Act
        var address = customer.GetFormattedAddress();

        // Assert
        address.Should().Be("Kundenstraße 5\n54321 Kundenort\nDeutschland");
    }

    [Fact]
    public void SmtpConfiguration_FromCompany_ReturnsNullWhenNotConfigured()
    {
        // Arrange
        var company = new Company();

        // Act
        var config = SmtpConfiguration.FromCompany(company);

        // Assert
        config.Should().BeNull();
    }

    [Fact]
    public void SmtpConfiguration_FromCompany_ReturnsConfigWhenComplete()
    {
        // Arrange
        var company = new Company
        {
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            SmtpUseSsl = true,
            SmtpUsername = "user",
            SmtpPassword = "pass",
            EmailSenderEmail = "sender@example.com",
            EmailSenderName = "Sender Name"
        };

        // Act
        var config = SmtpConfiguration.FromCompany(company);

        // Assert
        config.Should().NotBeNull();
        config!.Host.Should().Be("smtp.example.com");
        config.Port.Should().Be(587);
        config.UseSsl.Should().BeTrue();
        config.SenderEmail.Should().Be("sender@example.com");
        config.SenderName.Should().Be("Sender Name");
    }

    [Theory]
    [InlineData(Country.Deutschland, "DE")]
    [InlineData(Country.Oesterreich, "AT")]
    [InlineData(Country.Schweiz, "CH")]
    public void Country_ToIsoCode_ReturnsCorrectCode(Country country, string expected)
    {
        country.ToIsoCode().Should().Be(expected);
    }

    [Theory]
    [InlineData("DE", Country.Deutschland)]
    [InlineData("Deutschland", Country.Deutschland)]
    [InlineData("GERMANY", Country.Deutschland)]
    [InlineData("AT", Country.Oesterreich)]
    [InlineData("Österreich", Country.Oesterreich)]
    public void Country_FromString_ParsesCorrectly(string input, Country expected)
    {
        CountryExtensions.FromString(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(PaymentMethod.BankTransfer, "58")]
    [InlineData(PaymentMethod.Cash, "10")]
    [InlineData(PaymentMethod.DirectDebit, "59")]
    public void PaymentMethod_ToPaymentMeansCode_ReturnsCorrectCode(PaymentMethod method, string expected)
    {
        method.ToPaymentMeansCode().Should().Be(expected);
    }
}
