using Avalonia.Controls;
using Avalonia.Interactivity;
using CableConcentricityCalculator.Gui.ViewModels;

namespace CableConcentricityCalculator.Gui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "About Cable Concentricity Calculator",
            Width = 400,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Cable Concentricity Calculator",
                        FontSize = 18,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = "Version 1.0.0",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Foreground = Avalonia.Media.Brushes.Gray
                    },
                    new TextBlock
                    {
                        Text = "Design and visualize concentrically twisted cable harness assemblies.",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        TextAlignment = Avalonia.Media.TextAlignment.Center,
                        Margin = new Avalonia.Thickness(0, 12)
                    },
                    new TextBlock
                    {
                        Text = "Built with .NET 9 and Avalonia UI",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Foreground = Avalonia.Media.Brushes.Gray,
                        FontSize = 11
                    },
                    new Button
                    {
                        Content = "OK",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Padding = new Avalonia.Thickness(24, 8),
                        Margin = new Avalonia.Thickness(0, 12, 0, 0)
                    }
                }
            }
        };

        if (dialog.Content is StackPanel panel && panel.Children[^1] is Button okButton)
        {
            okButton.Click += (_, _) => dialog.Close();
        }

        dialog.ShowDialog(this);
    }

    private void OnPropertyChanged(object? sender, TextChangedEventArgs e)
    {
        ViewModel?.MarkChanged();
    }

    private void OnLayerPropertyChanged(object? sender, RoutedEventArgs e)
    {
        ViewModel?.MarkChanged();
        ViewModel?.UpdateCrossSectionImage();
    }

    private void OnLayerPropertyChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        ViewModel?.MarkChanged();
        ViewModel?.UpdateCrossSectionImage();
    }

    private void OnLayerPropertyChanged(object? sender, SelectionChangedEventArgs e)
    {
        ViewModel?.MarkChanged();
        ViewModel?.UpdateCrossSectionImage();
    }
}
