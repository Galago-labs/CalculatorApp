using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CalculatorApp.Services;

namespace CalculatorApp
{
    public partial class MainWindow : Window, IDisposable
    {
        private bool _isNewOperation = true;
        private string _lastOperation = "";
        private readonly Lazy<ExpressionParser> _expressionParser = new Lazy<ExpressionParser>(() => new ExpressionParser());
        private readonly Lazy<CalculatorState> _calculatorState = new Lazy<CalculatorState>(() => new CalculatorState());
        private readonly Lazy<NumberFormattingService> _numberFormattingService = new Lazy<NumberFormattingService>(() => new NumberFormattingService());
        
        // Static animations to reduce memory allocations
        private static readonly DoubleAnimation _buttonPressScaleXAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.95,
            Duration = TimeSpan.FromMilliseconds(CalculatorConfiguration.Instance.ButtonPressAnimationDurationMs),
            AutoReverse = true
        };

        private static readonly DoubleAnimation _buttonPressScaleYAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.95,
            Duration = TimeSpan.FromMilliseconds(CalculatorConfiguration.Instance.ButtonPressAnimationDurationMs),
            AutoReverse = true
        };

        private static readonly DoubleAnimation _computingOpacityAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.7,
            Duration = TimeSpan.FromMilliseconds(CalculatorConfiguration.Instance.ComputingAnimationDurationMs),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(2)
        };

        // Reusable ScaleTransform to reduce allocations
        private static readonly ScaleTransform _sharedScaleTransform = new ScaleTransform(1.0, 1.0);

        private void AnimateButtonPress(Button button)
        {
            // Check if animations are enabled
            if (!CalculatorConfiguration.Instance.EnableAnimations)
                return;

            // Reuse static animations to reduce memory allocations
            // Apply the animation to the button's render transform
            var transform = button.RenderTransform as ScaleTransform;
            if (transform == null)
            {
                transform = new ScaleTransform(1.0, 1.0);
                button.RenderTransform = transform;
                button.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            transform.BeginAnimation(ScaleTransform.ScaleXProperty, _buttonPressScaleXAnimation);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, _buttonPressScaleYAnimation);
        }

        public MainWindow()
        {
            InitializeComponent();
            ResultBox.Text = "0";
            UpdateMemoryIndicator(); // Initialize memory indicator
        }

        private void Append_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            AnimateButtonPress(button);
            
            string content = button.Content.ToString();

            // Convert symbols to operators for calculation
            string displayValue = content;
            string calcValue = content;

            switch (content)
            {
                case "×":
                    displayValue = "×";
                    calcValue = "*";
                    break;
                case "÷":
                    displayValue = "÷";
                    calcValue = "/";
                    break;
                case "√":
                    displayValue = "√";
                    calcValue = "√";
                    break;
                case "%":
                    displayValue = "%";
                    calcValue = "%";
                    break;
                case "^":
                    displayValue = "^";
                    calcValue = "^";
                    break;
            }

            if (_isNewOperation)
            {
                if ("+-×÷√^".Contains(content))
                {
                    // If starting with an operator, prepend the current value
                    if (ResultBox.Text != "0")
                    {
                        _lastOperation = ResultBox.Text + calcValue;
                        ResultBox.Text = "0";
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    ResultBox.Text = displayValue;
                    _isNewOperation = false;
                }
            }
            else
            {
                // Prevent multiple decimal points in a number
                if (content == "." && ResultBox.Text.Contains("."))
                {
                    string[] parts = ResultBox.Text.Split(new char[] { '+', '-', '×', '÷', '√', '^' });
                    if (parts[parts.Length - 1].Contains("."))
                        return;
                }

                ResultBox.Text += displayValue;
            }

            _lastOperation += calcValue;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
                AnimateButtonPress(button);
                
            ResultBox.Text = "0";
            _lastOperation = "";
            _isNewOperation = true;
            _calculatorState.Value.ClearHistory();
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
                AnimateButtonPress(button);

            if (_isNewOperation || ResultBox.Text.Length <= 1)
            {
                ResultBox.Text = "0";
                _isNewOperation = true;
            }
            else
            {
                ResultBox.Text = ResultBox.Text.Substring(0, ResultBox.Text.Length - 1);
            }

            if (_lastOperation.Length > 0)
            {
                _lastOperation = _lastOperation.Substring(0, _lastOperation.Length - 1);
            }
        }



        private void ToggleSign_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
                AnimateButtonPress(button);

            if (ResultBox.Text != "0")
            {
                if (ResultBox.Text.StartsWith("-"))
                {
                    ResultBox.Text = ResultBox.Text.Substring(1);
                }
                else
                {
                    ResultBox.Text = "-" + ResultBox.Text;
                }
            }
        }

        private void Equals_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
                AnimateButtonPress(button);

            try
            {
                // Show computing feedback
                AnimateComputingFeedback();

                if (!string.IsNullOrEmpty(_lastOperation))
                {
                    // Use the expression parser for all calculations
                    double result = _expressionParser.Value.Evaluate(_lastOperation);
                    
                    // Format the result with improved formatting
                    string formattedResult = FormatResult(result);
                    
                    // Save to history
                    _calculatorState.Value.AddToHistory($"{_lastOperation} = {formattedResult}");
                    
                    ResultBox.Text = formattedResult;
                    _isNewOperation = true;
                    _lastOperation = formattedResult;
                }
            }
            catch (DivisionByZeroException)
            {
                ResultBox.Text = "Cannot divide by zero";
                _isNewOperation = true;
                _lastOperation = "";
            }
            catch (CalculatorOverflowException)
            {
                ResultBox.Text = "Result too large";
                _isNewOperation = true;
                _lastOperation = "";
            }
            catch (InvalidSyntaxException)
            {
                ResultBox.Text = "Invalid expression";
                _isNewOperation = true;
                _lastOperation = "";
            }
            catch (ExpressionEvaluationException)
            {
                ResultBox.Text = "Calculation error";
                _isNewOperation = true;
                _lastOperation = "";
            }
            catch (Exception)
            {
                ResultBox.Text = "Error";
                _isNewOperation = true;
                _lastOperation = "";
            }
        }

        private void MemoryAdd()
        {
            if (double.TryParse(ResultBox.Text, out double currentValue))
            {
                _calculatorState.Value.AddToMemory(currentValue);
                UpdateMemoryIndicator();
            }
        }

        private void MemorySubtract()
        {
            if (double.TryParse(ResultBox.Text, out double currentValue))
            {
                _calculatorState.Value.SubtractFromMemory(currentValue);
                UpdateMemoryIndicator();
            }
        }

        private void MemoryRecall()
        {
            ResultBox.Text = _calculatorState.Value.MemoryValue.ToString(CultureInfo.InvariantCulture);
            _isNewOperation = true;
            UpdateMemoryIndicator();
        }

        private void MemoryClear()
        {
            _calculatorState.Value.ClearMemory();
            UpdateMemoryIndicator();
        }

        private void UpdateMemoryIndicator()
        {
            // Show memory indicator if memory has a non-zero value
            MemoryIndicator.Visibility = _calculatorState.Value.MemoryValue != 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AnimateComputingFeedback()
        {
            // Check if animations are enabled
            if (!CalculatorConfiguration.Instance.EnableAnimations)
                return;

            // Reuse static animation to reduce memory allocations
            ResultBox.BeginAnimation(UIElement.OpacityProperty, _computingOpacityAnimation);
        }

        private string FormatResult(double value)
        {
            return _numberFormattingService.Value.FormatResult(value);
        }

        private void NavigateHistory(int direction)
        {
            if (_calculatorState.Value.CalculationHistory.Count == 0) return;

            _calculatorState.Value.HistoryIndex += direction;

            // Wrap around
            if (_calculatorState.Value.HistoryIndex < 0)
                _calculatorState.Value.HistoryIndex = _calculatorState.Value.CalculationHistory.Count - 1;
            else if (_calculatorState.Value.HistoryIndex >= _calculatorState.Value.CalculationHistory.Count)
                _calculatorState.Value.HistoryIndex = 0;

            ResultBox.Text = _calculatorState.Value.CalculationHistory[_calculatorState.Value.HistoryIndex];
            _isNewOperation = true;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            switch (e.Key)
            {
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
                    if (_expressionParser.IsValueCreated)
                        _expressionParser.Value.Dispose();
                    if (_calculatorState.IsValueCreated)
                        _calculatorState.Value.Dispose();
                }

                // Освобождаем неуправляемые ресурсы (если есть)

                _disposed = true;
            }
        }

        #endregion
                case Key.Enter:
                    Equals_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    Clear_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.Back:
                    Backspace_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.Up:
                    NavigateHistory(-1);
                    e.Handled = true;
                    break;
                case Key.Down:
                    NavigateHistory(1);
                    e.Handled = true;
                    break;
                case Key.M:
                    if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) || e.KeyboardDevice.IsKeyDown(Key.RightCtrl))
                    {
                        // Ctrl+M - Memory Add
                        MemoryAdd();
                        e.Handled = true;
                    }
                    else if (e.KeyboardDevice.IsKeyDown(Key.LeftShift) || e.KeyboardDevice.IsKeyDown(Key.RightShift))
                    {
                        // Shift+M - Memory Subtract
                        MemorySubtract();
                        e.Handled = true;
                    }
                    break;
                case Key.R:
                    if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) || e.KeyboardDevice.IsKeyDown(Key.RightCtrl))
                    {
                        // Ctrl+R - Memory Recall
                        MemoryRecall();
                        e.Handled = true;
                    }
                    break;
                case Key.L:
                    if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) || e.KeyboardDevice.IsKeyDown(Key.RightCtrl))
                    {
                        // Ctrl+L - Memory Clear
                        MemoryClear();
                        e.Handled = true;
                    }
                    break;
                case Key.D0:
                case Key.NumPad0:
                    Append_Click(new Button { Content = "0" }, null);
                    e.Handled = true;
                    break;
                case Key.D1:
                case Key.NumPad1:
                    Append_Click(new Button { Content = "1" }, null);
                    e.Handled = true;
                    break;
                case Key.D2:
                case Key.NumPad2:
                    Append_Click(new Button { Content = "2" }, null);
                    e.Handled = true;
                    break;
                case Key.D3:
                case Key.NumPad3:
                    Append_Click(new Button { Content = "3" }, null);
                    e.Handled = true;
                    break;
                case Key.D4:
                case Key.NumPad4:
                    Append_Click(new Button { Content = "4" }, null);
                    e.Handled = true;
                    break;
                case Key.D5:
                case Key.NumPad5:
                    Append_Click(new Button { Content = "5" }, null);
                    e.Handled = true;
                    break;
                case Key.D6:
                case Key.NumPad6:
                    Append_Click(new Button { Content = "6" }, null);
                    e.Handled = true;
                    break;
                case Key.D7:
                case Key.NumPad7:
                    Append_Click(new Button { Content = "7" }, null);
                    e.Handled = true;
                    break;
                case Key.D8:
                case Key.NumPad8:
                    Append_Click(new Button { Content = "8" }, null);
                    e.Handled = true;
                    break;
                case Key.D9:
                case Key.NumPad9:
                    Append_Click(new Button { Content = "9" }, null);
                    e.Handled = true;
                    break;
                case Key.OemPeriod:
                case Key.Decimal:
                    Append_Click(new Button { Content = "." }, null);
                    e.Handled = true;
                    break;
                case Key.Add:
                case Key.OemPlus:
                    Append_Click(new Button { Content = "+" }, null);
                    e.Handled = true;
                    break;
                case Key.Subtract:
                case Key.OemMinus:
                    Append_Click(new Button { Content = "-" }, null);
                    e.Handled = true;
                    break;
                case Key.Multiply:
                    Append_Click(new Button { Content = "×" }, null);
                    e.Handled = true;
                    break;
                case Key.Divide:
                    Append_Click(new Button { Content = "÷" }, null);
                    e.Handled = true;
                    break;
                case Key.R:
                    Append_Click(new Button { Content = "√" }, null);
                    e.Handled = true;
                    break;
                case Key.D5:
                    if (e.KeyboardDevice.IsKeyDown(Key.LeftShift) || e.KeyboardDevice.IsKeyDown(Key.RightShift))
                    {
                        Append_Click(new Button { Content = "%" }, null);
                        e.Handled = true;
                    }
                    break;
                case Key.D6:
                    if (e.KeyboardDevice.IsKeyDown(Key.LeftShift) || e.KeyboardDevice.IsKeyDown(Key.RightShift))
                    {
                        Append_Click(new Button { Content = "^" }, null);
                        e.Handled = true;
                    }
                    break;
            }
        }
    }
}