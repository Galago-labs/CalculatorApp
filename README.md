# WPF Calculator App

A modern, lightweight calculator application built with C# and WPF. Features a dark theme interface similar to iOS/Android calculators with a responsive design and keyboard support.

## Features

- Modern dark theme UI
- iOS/Android style layout with large equals button
- Configurable button press animations for better feedback
- Memory indicator for active memory state
- Advanced expression parsing with proper operator precedence
- Support for complex nested expressions (e.g., "2^3+√9", "√(√16+9)")
- Enhanced percentage operations (e.g., "100+10%", "50-15%")
- Unary minus support (e.g., "-5+3")
- Memory functions (M+, M-, MR, MC)
- Improved number formatting (thousands separators, scientific notation)
- Clean architecture with separated concerns
- Detailed error handling with specific error messages
- Proper resource management with IDisposable pattern
- Lazy initialization of services
- Configurable settings
- Optimized memory usage with history and cache limits
- Calculation history with arrow key navigation (limited to 100 entries)
- Keyboard support (numbers, operators, Enter, Escape, Backspace, Up/Down arrows, memory functions)
- Single-file portable executable
- Fast startup time (< 1 second)
- Small footprint (~10MB)

## Layout

The calculator follows the standard mobile calculator layout:
```
Row 0:  C    ←    %    ÷
Row 1:  7    8    9    ×
Row 2:  4    5    6    -
Row 3:  1    2    3    +
Row 4:  0    .         =
Row 5:  √    ^    ±    =
```

## Building the Project

### Prerequisites

- .NET 6.0 SDK or later
- Windows OS (for WPF)

### Project Structure

```
CalculatorApp/
├── Services/
│   ├── ExpressionParser.cs         # Advanced expression evaluation engine
│   ├── NumberFormattingService.cs   # Number formatting utilities
│   ├── CalculatorState.cs          # Calculator state management
│   ├── CalculatorExceptions.cs     # Specialized calculator exceptions
│   └── CalculatorConfiguration.cs  # Configuration settings
├── App.xaml                        # Application resources and styles
├── MainWindow.xaml                 # Main window UI
├── MainWindow.xaml.cs              # UI logic and event handlers
└── CalculatorApp.csproj            # Project configuration

CalculatorApp.Tests/                # Unit tests for the calculator logic
├── ExpressionParserTests.cs        # Tests for expression evaluation
├── NumberFormattingServiceTests.cs  # Tests for number formatting
├── CalculatorStateTests.cs         # Tests for calculator state management
└── CalculatorApp.Tests.csproj      # Test project configuration
```

### Running Tests

To run the unit tests:
```bash
cd CalculatorApp.Tests
dotnet test
```

The test suite includes:
- Expression parser tests for mathematical operations
- Number formatting tests for various number types
- Calculator state tests for history and memory management

### Local Build

1. Clone the repository:
```bash
git clone <repository-url>
```

2. Navigate to the project directory:
```bash
cd CalculatorApp
```

3. Build the project:
```bash
dotnet build
```

4. Run the application:
```bash
dotnet run
```

### Creating a Portable Executable

To create a single-file executable that can run without installation:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=true -o ./publish/
```

The resulting executable will be located in the `publish` folder.

### GitHub Actions Build

This repository includes a GitHub Actions workflow that automatically builds the portable executable on every push. The executable can be downloaded from the Actions tab.

## Keyboard Shortcuts

- **Numbers (0-9)**: Type directly
- **Operators**: +, -, *, /
- **Enter**: Calculate result (=)
- **Escape**: Clear display (C)
- **Backspace**: Delete last character (←)
- **Up/Down Arrows**: Navigate calculation history
- **Ctrl+M**: Memory Add (M+)
- **Shift+M**: Memory Subtract (M-)
- **Ctrl+R**: Memory Recall (MR)
- **Ctrl+L**: Memory Clear (MC)
- **.**: Decimal point

## Customization

You can customize the appearance by modifying the styles in `App.xaml`:
- Button colors
- Font sizes
- Spacing and margins

## License

MIT License