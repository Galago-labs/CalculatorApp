using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CalculatorApp.Services
{
    public class ExpressionParser : IDisposable
    {
        // Максимальная глубина рекурсии для защиты от StackOverflow
        private const int MaxEvaluationDepth = 20;
        private int _currentDepth = 0;

        private readonly Dictionary<string, int> _operatorPrecedence = new Dictionary<string, int>
        {
            { "+", 1 },
            { "-", 1 },
            { "*", 2 },
            { "/", 2 },
            { "^", 3 }
        };

        private readonly Dictionary<string, string> _displayToInternalOperators = new Dictionary<string, string>
        {
            { "×", "*" },
            { "÷", "/" },
            { "√", "sqrt" }
        };

        // Predefined string constants to avoid repeated ToString() calls
        private static readonly string PlusString = "+";
        private static readonly string MinusString = "-";
        private static readonly string MultiplyString = "*";
        private static readonly string DivideString = "/";
        private static readonly string PowerString = "^";
        private static readonly string OpenParenString = "(";
        private static readonly string CloseParenString = ")";

        // LRU-кэш: словарь + список для отслеживания порядка использования
        private readonly Dictionary<string, double> _computationCache = new Dictionary<string, double>();
        private readonly LinkedList<string> _cacheOrder = new LinkedList<string>();

        public double Evaluate(string expression)
        {
            try
            {
                // Check cache first
                if (_computationCache.TryGetValue(expression, out double cachedResult))
                {
                    // Move to front (most recently used)
                    _cacheOrder.Remove(expression);
                    _cacheOrder.AddFirst(expression);
                    return cachedResult;
                }

                // Preprocess the expression
                string processedExpression = PreprocessExpression(expression);
                
                // Parse and evaluate
                var tokens = Tokenize(processedExpression);
                var rpn = InfixToRPN(tokens);
                double result = EvaluateRPN(rpn);

                // Cache the result (LRU eviction)
                if (_computationCache.Count >= CalculatorConfiguration.Instance.MaxCacheSize)
                {
                    // Remove least recently used entry
                    if (_cacheOrder.Last != null)
                    {
                        string lruKey = _cacheOrder.Last.Value;
                        _cacheOrder.RemoveLast();
                        _computationCache.Remove(lruKey);
                    }
                }
                _computationCache[expression] = result;
                _cacheOrder.AddFirst(expression);

                return result;
            }
            catch (CalculatorException)
            {
                // Re-throw calculator-specific exceptions
                throw;
            }
            catch (DivideByZeroException)
            {
                // Convert system DivideByZeroException to our specialized exception
                throw new DivisionByZeroException();
            }
            catch (OverflowException)
            {
                throw new CalculatorOverflowException();
            }
            catch (Exception ex)
            {
                throw new ExpressionEvaluationException($"Error evaluating expression: {ex.Message}", ex);
            }
        }

        public void ClearCache()
        {
            _computationCache.Clear();
            _cacheOrder.Clear();
        }

        #region IDisposable Implementation
        
        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Освобождаем управляемые ресурсы
                    _computationCache.Clear();
                    _cacheOrder.Clear();
                }

                // Освобождаем неуправляемые ресурсы (если есть)

                _disposed = true;
            }
        }

        #endregion

        private string PreprocessExpression(string expression)
        {
            var result = new StringBuilder(expression);
            
            // Сначала обрабатываем квадратные корни (до замены символов)
            // чтобы правильно обрабатывать вложенные √(√(…))
            result = ProcessSquareRoots(result.ToString());
            
            // Затем обрабатываем проценты
            result = ProcessPercentages(result.ToString());
            
            // Потом заменяем display-символы на внутренние
            foreach (var op in _displayToInternalOperators)
            {
                result.Replace(op.Key, op.Value);
            }
            
            return result.ToString();
        }

        private StringBuilder ProcessPercentages(string expression)
        {
            var result = new StringBuilder(expression);
            
            // Process from right to left to avoid index shifting issues
            for (int i = result.Length - 1; i >= 0; i--)
            {
                if (result[i] == '%')
                {
                    // Find the percentage number (digits and decimal point before %)
                    int numberEnd = i - 1;
                    while (numberEnd >= 0 && (char.IsDigit(result[numberEnd]) || result[numberEnd] == '.'))
                    {
                        numberEnd--;
                    }
                    numberEnd++; // Move to the start of the number
                    
                    if (numberEnd < i)
                    {
                        // Extract substring without calling ToString() repeatedly
                        string numberStr = ExtractSubstring(result, numberEnd, i - numberEnd);
                        if (double.TryParse(numberStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double percentageValue))
                        {
                            // Check if this is a contextual percentage operation (+ or - before the number)
                            int operatorPos = numberEnd - 1;
                            // Skip whitespace
                            while (operatorPos >= 0 && char.IsWhiteSpace(result[operatorPos]))
                            {
                                operatorPos--;
                            }
                            
                            // If we found a + or - operator, this is a contextual percentage
                            if (operatorPos >= 0 && (result[operatorPos] == '+' || result[operatorPos] == '-'))
                            {
                                // Find the base number for the operation
                                int baseNumberEnd = operatorPos - 1;
                                // Skip whitespace
                                while (baseNumberEnd >= 0 && char.IsWhiteSpace(result[baseNumberEnd]))
                                {
                                    baseNumberEnd--;
                                }
                                
                                // Find the start of the base number
                                int baseNumberStart = baseNumberEnd;
                                while (baseNumberStart >= 0 && (char.IsDigit(result[baseNumberStart]) || result[baseNumberStart] == '.'))
                                {
                                    baseNumberStart--;
                                }
                                baseNumberStart++;
                                
                                // If we found a valid base number
                                if (baseNumberStart <= baseNumberEnd)
                                {
                                    string baseNumberStr = ExtractSubstring(result, baseNumberStart, baseNumberEnd - baseNumberStart + 1);
                                    if (double.TryParse(baseNumberStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double baseValue))
                                    {
                                        // Calculate the contextual percentage
                                        // For "100+10%" we calculate 100 + (100 * 0.10) = 110
                                        // For "100-10%" we calculate 100 - (100 * 0.10) = 90
                                        double percentageDecimal = percentageValue / 100.0;
                                        double calculatedValue = result[operatorPos] == '+' ? 
                                            baseValue + (baseValue * percentageDecimal) : 
                                            baseValue - (baseValue * percentageDecimal);
                                            
                                        // Replace the entire expression part (from base number to %)
                                        result.Remove(baseNumberStart, i - baseNumberStart + 1);
                                        result.Insert(baseNumberStart, calculatedValue.ToString(CultureInfo.InvariantCulture));
                                    }
                                    else
                                    {
                                        // Couldn't parse base number, do simple percentage conversion
                                        double percentageDecimal = percentageValue / 100.0;
                                        result.Remove(numberEnd, i - numberEnd + 1);
                                        result.Insert(numberEnd, percentageDecimal.ToString(CultureInfo.InvariantCulture));
                                    }
                                }
                                else
                                {
                                    // No valid base number, do simple percentage conversion
                                    double percentageDecimal = percentageValue / 100.0;
                                    result.Remove(numberEnd, i - numberEnd + 1);
                                    result.Insert(numberEnd, percentageDecimal.ToString(CultureInfo.InvariantCulture));
                                }
                            }
                            else
                            {
                                // Simple percentage conversion (for * and / operations, or standalone)
                                double percentageDecimal = percentageValue / 100.0;
                                result.Remove(numberEnd, i - numberEnd + 1);
                                result.Insert(numberEnd, percentageDecimal.ToString(CultureInfo.InvariantCulture));
                            }
                        }
                        else
                        {
                            // Couldn't parse the percentage number, remove the %
                            result.Remove(i, 1);
                        }
                    }
                    else
                    {
                        // No number before %, remove the %
                        result.Remove(i, 1);
                    }
                }
            }
            
            return result;
        }

        private StringBuilder ProcessSquareRoots(string expression)
        {
            var result = new StringBuilder(expression);
            bool changed;
            
            // Итеративный проход: повторяем, пока есть изменения
            // Это гарантирует обработку вложенных √(√(…))
            do
            {
                changed = false;
                
                // Проходим справа налево для первого найденного √(…)
                // Находим самый внутренний √(…), обрабатывая справа
                for (int i = result.Length - 1; i >= 0; i--)
                {
                    if (result[i] == '√')
                    {
                        int sqrtPos = i;
                        
                        // Check if next char is '(' — sqrt(expression)
                        if (sqrtPos + 1 < result.Length && result[sqrtPos + 1] == '(')
                        {
                            // Find the matching closing parenthesis
                            int closeParen = FindMatchingParenthesis(result.ToString(), sqrtPos + 1);
                            if (closeParen != -1)
                            {
                                // Extract the expression inside the parentheses
                                string innerExpression = ExtractSubstring(result, sqrtPos + 2, closeParen - sqrtPos - 2);
                                
                                double innerValue = EvaluateSubExpression(innerExpression);
                                double sqrtValue = Math.Sqrt(Math.Abs(innerValue));
                                string sqrtStr = sqrtValue.ToString(CultureInfo.InvariantCulture);
                                
                                // Replace √(expression) with result
                                result.Remove(sqrtPos, closeParen - sqrtPos + 1);
                                result.Insert(sqrtPos, sqrtStr);
                                
                                changed = true;
                                break; // restart search from the end
                            }
                        }
                        else
                        {
                            // No parentheses — √ applies to the next number
                            // Find where the number starts (skip whitespace)
                            int numStart = sqrtPos + 1;
                            while (numStart < result.Length && char.IsWhiteSpace(result[numStart]))
                            {
                                numStart++;
                            }
                            
                            if (numStart < result.Length && (char.IsDigit(result[numStart]) || result[numStart] == '.'))
                            {
                                int numEnd = numStart;
                                while (numEnd < result.Length && (char.IsDigit(result[numEnd]) || result[numEnd] == '.'))
                                {
                                    numEnd++;
                                }
                                
                                string numberStr = ExtractSubstring(result, numStart, numEnd - numStart);
                                if (double.TryParse(numberStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double numberValue))
                                {
                                    double sqrtValue = Math.Sqrt(Math.Abs(numberValue));
                                    string sqrtStr = sqrtValue.ToString(CultureInfo.InvariantCulture);
                                    
                                    // Replace √number with result
                                    result.Remove(sqrtPos, numEnd - sqrtPos);
                                    result.Insert(sqrtPos, sqrtStr);
                                    
                                    changed = true;
                                    break; // restart search from the end
                                }
                            }
                        }
                    }
                }
            } while (changed);
            
            return result;
        }

        private double EvaluateSubExpression(string expression)
        {
            // Защита от чрезмерной глубины рекурсии
            if (_currentDepth >= MaxEvaluationDepth)
            {
                // Если превысили лимит глубины, выполняем прямую RPN-оценку
                var tokens = Tokenize(expression);
                var rpn = InfixToRPN(tokens);
                return EvaluateRPN(rpn);
            }
            
            _currentDepth++;
            try
            {
                return Evaluate(expression);
            }
            finally
            {
                _currentDepth--;
            }
        }

        private int FindMatchingParenthesis(string expression, int openParenIndex)
        {
            int count = 1;
            for (int i = openParenIndex + 1; i < expression.Length; i++)
            {
                if (expression[i] == '(')
                    count++;
                else if (expression[i] == ')')
                {
                    count--;
                    if (count == 0)
                        return i;
                }
            }
            return -1;
        }

        private string ExtractSubstring(StringBuilder sb, int startIndex, int length)
        {
            // Efficiently extract substring from StringBuilder
            char[] chars = new char[length];
            sb.CopyTo(startIndex, chars, 0, length);
            return new string(chars);
        }

        private List<string> Tokenize(string expression)
        {
            var tokens = new List<string>();
            var currentToken = new StringBuilder();

            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];

                if (char.IsWhiteSpace(c))
                {
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                }
                else if (char.IsDigit(c) || c == '.')
                {
                    currentToken.Append(c);
                }
                else if (c == '+' || c == '-' || c == '*' || c == '/' || c == '^' || c == '(' || c == ')')
                {
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                    
                    // Handle unary minus by checking context
                    if (c == '-' && (i == 0 || expression[i-1] == '(' || IsOperatorChar(expression[i-1])))
                    {
                        // This is likely a unary minus, but we'll handle it in the RPN conversion
                        tokens.Add("-");
                    }
                    else
                    {
                        // Use predefined strings to avoid ToString() calls
                        string tokenToAdd = c switch
                        {
                            '+' => PlusString,
                            '-' => MinusString,
                            '*' => MultiplyString,
                            '/' => DivideString,
                            '^' => PowerString,
                            '(' => OpenParenString,
                            ')' => CloseParenString,
                            _ => c.ToString() // Fallback for unexpected characters
                        };
                        tokens.Add(tokenToAdd);
                    }
                }
                else if (char.IsLetter(c))
                {
                    currentToken.Append(c);
                }
                else
                {
                    // Skip unrecognized characters
                }
            }

            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString());
            }

            return tokens;
        }

        private bool IsOperatorChar(char c)
        {
            return c == '+' || c == '-' || c == '*' || c == '/' || c == '^';
        }

        private List<string> InfixToRPN(List<string> infixTokens)
        {
            var outputQueue = new List<string>();
            var operatorStack = new Stack<string>();

            for (int i = 0; i < infixTokens.Count; i++)
            {
                string token = infixTokens[i];

                if (IsNumber(token))
                {
                    outputQueue.Add(token);
                }
                else if (token == "(")
                {
                    operatorStack.Push(token);
                }
                else if (token == ")")
                {
                    while (operatorStack.Count > 0 && operatorStack.Peek() != "(")
                    {
                        outputQueue.Add(operatorStack.Pop());
                    }
                    if (operatorStack.Count > 0)
                    {
                        operatorStack.Pop(); // Remove the "("
                    }
                }
                else if (IsOperator(token))
                {
                    // Check if this is a unary minus
                    if (token == "-" && (i == 0 || infixTokens[i - 1] == "(" || IsOperator(infixTokens[i - 1])))
                    {
                        // Handle unary minus by converting it to a special token
                        // We'll treat it as multiplying by -1
                        outputQueue.Add("0"); // Push 0 to stack
                        operatorStack.Push("-"); // Then subtract (which effectively negates)
                    }
                    else
                    {
                        // Handle binary operators normally
                        while (operatorStack.Count > 0 && 
                               operatorStack.Peek() != "(" &&
                               HasHigherOrEqualPrecedence(operatorStack.Peek(), token))
                        {
                            outputQueue.Add(operatorStack.Pop());
                        }
                        operatorStack.Push(token);
                    }
                }
            }

            while (operatorStack.Count > 0)
            {
                outputQueue.Add(operatorStack.Pop());
            }

            return outputQueue;
        }

        private double EvaluateRPN(List<string> rpnTokens)
        {
            var stack = new Stack<double>();

            foreach (string token in rpnTokens)
            {
                if (IsNumber(token))
                {
                    stack.Push(double.Parse(token, CultureInfo.InvariantCulture));
                }
                else if (IsOperator(token))
                {
                    if (stack.Count < 2)
                        throw new ExpressionEvaluationException("Invalid expression: insufficient operands");

                    double b = stack.Pop();
                    double a = stack.Pop();
                    double result = PerformOperation(a, b, token);
                    stack.Push(result);
                }
            }

            if (stack.Count != 1)
                throw new ExpressionEvaluationException("Invalid expression: incorrect format");

            return stack.Pop();
        }

        private bool IsNumber(string token)
        {
            return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }

        private bool IsOperator(string token)
        {
            return _operatorPrecedence.ContainsKey(token);
        }

        private bool HasHigherOrEqualPrecedence(string op1, string op2)
        {
            if (!_operatorPrecedence.ContainsKey(op1) || !_operatorPrecedence.ContainsKey(op2))
                return false;

            // For right associative operators like exponentiation, we don't want equal precedence to cause a pop
            if (op1 == "^" && op2 == "^")
                return false;

            return _operatorPrecedence[op1] >= _operatorPrecedence[op2];
        }

        private double PerformOperation(double a, double b, string op)
        {
            switch (op)
            {
                case "+": return a + b;
                case "-": return a - b;
                case "*": return a * b;
                case "/":
                    if (b == 0)
                        throw new DivisionByZeroException();
                    return a / b;
                case "^": 
                    double result = Math.Pow(a, b);
                    if (double.IsInfinity(result))
                        throw new CalculatorOverflowException();
                    return result;
                default:
                    throw new ExpressionEvaluationException($"Unknown operator: {op}");
            }
        }
    }
}
