using System;
using System.ComponentModel;
using Xunit;
using CalculatorApp.Services;

namespace CalculatorApp.Tests
{
    public class CalculatorStateTests
    {
        private readonly CalculatorState _state;

        public CalculatorStateTests()
        {
            _state = new CalculatorState();
        }

        [Fact]
        public void InitialState_IsCorrect()
        {
            // Assert
            Assert.Empty(_state.CalculationHistory);
            Assert.Equal(0.0, _state.MemoryValue);
            Assert.False(_state.IsMemoryActive);
            Assert.Equal(-1, _state.HistoryIndex);
        }

        [Fact]
        public void AddToHistory_AddsEntryToList()
        {
            // Arrange
            string entry = "2+2 = 4";

            // Act
            _state.AddToHistory(entry);

            // Assert
            Assert.Single(_state.CalculationHistory);
            Assert.Contains(entry, _state.CalculationHistory);
            Assert.Equal(0, _state.HistoryIndex); // Should be reset to 0 when first item added
        }

        [Fact]
        public void AddToHistory_RespectsMaxSizeLimit()
        {
            // Arrange
            // Add more entries than the max size
            for (int i = 0; i <= NumberFormattingService.GetMaxHistorySize(); i++)
            {
                _state.AddToHistory($"Entry {i}");
            }

            // Act
            int count = _state.CalculationHistory.Count;

            // Assert
            Assert.Equal(NumberFormattingService.GetMaxHistorySize(), count);
        }

        [Fact]
        public void ClearHistory_RemovesAllEntries()
        {
            // Arrange
            _state.AddToHistory("Entry 1");
            _state.AddToHistory("Entry 2");

            // Act
            _state.ClearHistory();

            // Assert
            Assert.Empty(_state.CalculationHistory);
            Assert.Equal(-1, _state.HistoryIndex);
        }

        [Fact]
        public void MemoryValue_SetToNonZero_SetsIsMemoryActive()
        {
            // Act
            _state.MemoryValue = 5.0;

            // Assert
            Assert.True(_state.IsMemoryActive);
        }

        [Fact]
        public void MemoryValue_SetToZero_ClearsIsMemoryActive()
        {
            // Arrange
            _state.MemoryValue = 5.0; // Set to non-zero first

            // Act
            _state.MemoryValue = 0.0;

            // Assert
            Assert.False(_state.IsMemoryActive);
        }

        [Fact]
        public void AddToMemory_IncreasesMemoryValue()
        {
            // Arrange
            double initialValue = 10.0;
            double addToMemory = 5.0;
            _state.MemoryValue = initialValue;

            // Act
            _state.AddToMemory(addToMemory);

            // Assert
            Assert.Equal(initialValue + addToMemory, _state.MemoryValue);
        }

        [Fact]
        public void SubtractFromMemory_DecreasesMemoryValue()
        {
            // Arrange
            double initialValue = 10.0;
            double subtractFromMemory = 5.0;
            _state.MemoryValue = initialValue;

            // Act
            _state.SubtractFromMemory(subtractFromMemory);

            // Assert
            Assert.Equal(initialValue - subtractFromMemory, _state.MemoryValue);
        }

        [Fact]
        public void ClearMemory_SetsMemoryValueToZero()
        {
            // Arrange
            _state.MemoryValue = 10.0;

            // Act
            _state.ClearMemory();

            // Assert
            Assert.Equal(0.0, _state.MemoryValue);
            Assert.False(_state.IsMemoryActive);
        }

        [Fact]
        public void PropertyChanged_IsRaised_WhenMemoryValueChanges()
        {
            // Arrange
            bool propertyChangedRaised = false;
            _state.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(CalculatorState.MemoryValue))
                    propertyChangedRaised = true;
            };

            // Act
            _state.MemoryValue = 5.0;

            // Assert
            Assert.True(propertyChangedRaised);
        }

        [Fact]
        public void PropertyChanged_IsRaised_WhenIsMemoryActiveChanges()
        {
            // Arrange
            bool propertyChangedRaised = false;
            _state.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(CalculatorState.IsMemoryActive))
                    propertyChangedRaised = true;
            };

            // Act
            _state.MemoryValue = 5.0; // This should trigger IsMemoryActive change

            // Assert
            Assert.True(propertyChangedRaised);
        }
    }
}