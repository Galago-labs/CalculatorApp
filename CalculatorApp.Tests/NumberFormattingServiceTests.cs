using System;
using Xunit;
using CalculatorApp.Services;

namespace CalculatorApp.Tests
{
    public class NumberFormattingServiceTests
    {
        private readonly NumberFormattingService _service;

        public NumberFormattingServiceTests()
        {
            _service = new NumberFormattingService();
        }

        [Theory]
        [InlineData(5, "5")]
        [InlineData(1234, "1,234")]
        [InlineData(1234567, "1.234567E+06")] // Large numbers use scientific notation
        public void FormatResult_IntegerValues_ReturnsCorrectFormat(double value, string expected)
        {
            // Act
            string result = _service.FormatResult(value);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(3.14159, "3.14159")]
        [InlineData(0.00001, "1E-05")] // Very small numbers use scientific notation
        [InlineData(1234567.89, "1.23456789E+06")] // Large decimals use scientific notation
        public void FormatResult_DecimalValues_ReturnsCorrectFormat(double value, string expected)
        {
            // Act
            string result = _service.FormatResult(value);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(double.NaN, "Error")]
        [InlineData(double.PositiveInfinity, "Error")]
        [InlineData(double.NegativeInfinity, "Error")]
        public void FormatResult_SpecialValues_ReturnsError(double value, string expected)
        {
            // Act
            string result = _service.FormatResult(value);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetMaxHistorySize_ReturnsCorrectValue()
        {
            // Act
            int result = NumberFormattingService.GetMaxHistorySize();

            // Assert
            Assert.Equal(100, result);
        }

        [Fact]
        public void GetMaxCacheSize_ReturnsCorrectValue()
        {
            // Act
            int result = NumberFormattingService.GetMaxCacheSize();

            // Assert
            Assert.Equal(50, result);
        }
    }
}