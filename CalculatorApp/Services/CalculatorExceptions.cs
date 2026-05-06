using System;

namespace CalculatorApp.Services
{
    /// <summary>
    /// Base exception for calculator-related errors
    /// </summary>
    public class CalculatorException : Exception
    {
        public CalculatorException(string message) : base(message) { }
        public CalculatorException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when expression evaluation fails
    /// </summary>
    public class ExpressionEvaluationException : CalculatorException
    {
        public ExpressionEvaluationException(string message) : base(message) { }
        public ExpressionEvaluationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when division by zero occurs
    /// </summary>
    public class DivisionByZeroException : CalculatorException
    {
        public DivisionByZeroException() : base("Division by zero is not allowed") { }
        public DivisionByZeroException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when invalid syntax is detected
    /// </summary>
    public class InvalidSyntaxException : CalculatorException
    {
        public InvalidSyntaxException(string message) : base($"Invalid expression syntax: {message}") { }
        public InvalidSyntaxException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when overflow occurs
    /// </summary>
    public class CalculatorOverflowException : CalculatorException
    {
        public CalculatorOverflowException() : base("Calculation result is too large") { }
        public CalculatorOverflowException(string message) : base(message) { }
    }
}