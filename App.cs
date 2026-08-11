using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CppCalculator;

public class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
        => new(new CalculatorPage());
}