using System;
using Xunit;
using CalculatorApp.Services;

namespace CalculatorApp.Tests
{
    public class ExpressionParserTests
    {
        private readonly ExpressionParser _parser;

        public ExpressionParserTests()
        {
            _parser = new ExpressionParser();
        }

        [Theory]
        [InlineData("2+3", 5)]
        [InlineData("10-4", 6)]
        [InlineData("3*4", 12)]
        [InlineData("15/3", 5)]
        [InlineData("2^3", 8)]
        public void BasicOperations_ReturnCorrectResults(string expression, double expected)
        {
            // Act
            double result = _parser.Evaluate(expression);

            // Assert
            Assert.Equal(expected, result, precision: 10);
        }

        [Theory]
        [InlineData("2+3*4", 14)] // Multiplication has higher precedence
        [InlineData("10-4/2", 8)]  // Division has higher precedence
        [InlineData("2*3+4", 10)]  // Left-to-right for same precedence
        [InlineData("2^3^2", 512)] // Right-to-left associativity for exponentiation
        public void OperatorPrecedence_ReturnsCorrectResults(string expression, double expected)
        {
            // Act
            double result = _parser.Evaluate(expression);

            // Assert
            Assert.Equal(expected, result, precision: 10);
        }

        [Fact]
        public void ComplexExpression_ReturnsCorrectResult()
        {
            // Arrange
            string expression = "2^3+√9*2"; // 8 + 3 * 2 = 14
            double expected = 14;

            // Act
            double result = _parser.Evaluate(expression);

            // Assert
            Assert.Equal(expected, result, precision: 10);
        }

        [Fact]
        public void Parentheses_ChangePrecedenceCorrectly()
        {
            // Arrange
            string expression = "(2+3)*4"; // 5 * 4 = 20
            double expected = 20;

            // Act
            double result = _parser.Evaluate(expression);

            // Assert
            Assert.Equal(expected, result, precision: 10);
        }

        [Fact]
        public void DivisionByZero_ThrowsException()
        {
            // Arrange
            string expression = "5/0";

            // Act & Assert
            Assert.Throws<ExpressionEvaluationException>(() => _parser.Evaluate(expression));
        }

        [Fact]
        public void InvalidExpression_ThrowsException()
        {
            // Arrange
            string expression = "2+*3";

            // Act & Assert
            Assert.Throws<ExpressionEvaluationException>(() => _parser.Evaluate(expression));
        }

        [Theory]
        [InlineData("-5", -5)]
        [InlineData("-5+3", -2)]
        [InlineData("5+-3", 2)]
        [InlineData("(-2)", -2)]
        [InlineData("(-2)+3", 1)]
        [InlineData("5*(-3)", -15)]
        [InlineData("--5", 5)] // Double negative
        [InlineData("-(3+2)", -5)] // Negation of expression
        public void UnaryMinus_ReturnsCorrectResults(string expression, double expected)
        {
            // Act
            double result = _parser.Evaluate(expression);

            // Assert
            Assert.Equal(expected, result, precision: 10);
        }

        [Theory]
        [InlineData("50%", 0.5)]
        [InlineData("100+10%", 110)]
        [InlineData("200-15%", 170)]
        [InlineData("50*20%", 10)]
        [InlineData("100/25%", 400)]
        public void PercentageOperations_ReturnCorrectResults(string expression, double expected)
        {
            // Act
            double result = _parser.Evaluate(expression);

            // Assert
            Assert.Equal(expected, result, precision: 10);
        }

        [Theory]
        [InlineData("√9", 3)]
        [InlineData("√16", 4)]
        [InlineData("√(4+5)", 3)]
        [InlineData("√(√16+9)", 3.605551275)] // √(4+9) = √13 ≈ 3.605551275
        [InlineData("√(2^3+1)", 3)] // √(8+1) = √9 = 3
        public void SquareRootOperations_ReturnCorrectResults(string expression, double expected)
        {
            // Act
            double result = _parser.Evaluate(expression);

            // Assert
            Assert.Equal(expected, result, precision: 8);
        }
    }
}