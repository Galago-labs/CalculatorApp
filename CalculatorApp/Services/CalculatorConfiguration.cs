namespace CalculatorApp.Services
{
    public class CalculatorConfiguration
    {
        // Maximum number of entries in calculation history
        public int MaxHistorySize { get; set; } = 100;
        
        // Maximum number of entries in expression cache
        public int MaxCacheSize { get; set; } = 50;
        
        // Animation duration for button press in milliseconds
        public int ButtonPressAnimationDurationMs { get; set; } = 50;
        
        // Animation duration for computing feedback in milliseconds
        public int ComputingAnimationDurationMs { get; set; } = 100;
        
        // Enable or disable animations
        public bool EnableAnimations { get; set; } = true;
        
        // Singleton instance
        private static readonly CalculatorConfiguration _instance = new CalculatorConfiguration();
        
        public static CalculatorConfiguration Instance => _instance;
        
        // Private constructor to prevent instantiation
        private CalculatorConfiguration() { }
    }
}