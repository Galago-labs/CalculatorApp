using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CalculatorApp.Services
{
    public class CalculatorState : INotifyPropertyChanged
    {
        private readonly List<string> _calculationHistory = new List<string>();
        private int _historyIndex = -1;
        private double _memoryValue = 0;
        private bool _isMemoryActive = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public IReadOnlyList<string> CalculationHistory => _calculationHistory;
        public int HistoryIndex 
        { 
            get => _historyIndex; 
            set 
            { 
                _historyIndex = value; 
                OnPropertyChanged(nameof(HistoryIndex));
            } 
        }
        
        public double MemoryValue 
        { 
            get => _memoryValue; 
            set 
            { 
                _memoryValue = value; 
                IsMemoryActive = _memoryValue != 0;
                OnPropertyChanged(nameof(MemoryValue));
            } 
        }
        
        public bool IsMemoryActive 
        { 
            get => _isMemoryActive; 
            private set 
            { 
                _isMemoryActive = value; 
                OnPropertyChanged(nameof(IsMemoryActive));
            } 
        }

        public void AddToHistory(string entry)
        {
            _calculationHistory.Add(entry);
            
            // Limit history size to prevent memory issues
            if (_calculationHistory.Count > CalculatorConfiguration.Instance.MaxHistorySize)
            {
                _calculationHistory.RemoveAt(0);
                // Adjust history index if needed
                if (_historyIndex > 0)
                {
                    _historyIndex--;
                }
            }
            
            OnPropertyChanged(nameof(CalculationHistory));
        }

        public void ClearHistory()
        {
            _calculationHistory.Clear();
            _historyIndex = -1;
            OnPropertyChanged(nameof(CalculationHistory));
            OnPropertyChanged(nameof(HistoryIndex));
        }

        public void ClearMemory()
        {
            MemoryValue = 0;
        }

        public void AddToMemory(double value)
        {
            MemoryValue += value;
        }

        public void SubtractFromMemory(double value)
        {
            MemoryValue -= value;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}