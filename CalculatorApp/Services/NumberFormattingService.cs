using System;
using System.Globalization;

namespace CalculatorApp.Services
{
    public class NumberFormattingService
    {

        public string FormatResult(double value)
        {
            // Handle special cases
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "Error";
            }
            
            // Check if it's a whole number
            if (value == Math.Floor(value))
            {
                // For integers, use standard formatting with thousand separators
                long longValue = (long)value;
                if (Math.Abs(longValue) < 1000000) // Less than 1 million
                {
                    return longValue.ToString("N0", CultureInfo.InvariantCulture);
                }
                else
                {
                    // For very large numbers, use scientific notation
                    return value.ToString("G6", CultureInfo.InvariantCulture);
                }
            }
            else
            {
                // For decimal numbers
                string strValue = value.ToString(CultureInfo.InvariantCulture);
                
                // If the number is very small, use scientific notation
                if (Math.Abs(value) < 0.0001 && value != 0)
                {
                    return value.ToString("G6", CultureInfo.InvariantCulture);
                }
                
                // If the number is very large, use scientific notation
                if (Math.Abs(value) >= 1000000)
                {
                    return value.ToString("G6", CultureInfo.InvariantCulture);
                }
                
                // For regular decimal numbers, limit decimal places
                if (strValue.Length > 12) // If string representation is too long
                {
                    return value.ToString("F10", CultureInfo.InvariantCulture)
                               .TrimEnd('0') // Remove trailing zeros
                               .TrimEnd('.'); // Remove trailing decimal point if needed
                }
                else
                {
                    // Keep reasonable precision
                    return value.ToString("G", CultureInfo.InvariantCulture);
                }
            }
        }

        public static int GetMaxHistorySize() => CalculatorConfiguration.Instance.MaxHistorySize;
        public static int GetMaxCacheSize() => CalculatorConfiguration.Instance.MaxCacheSize;
    }
}