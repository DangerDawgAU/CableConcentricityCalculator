using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CableConcentricityCalculator.Gui.ViewModels;
using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Visualization;

namespace CableConcentricityCalculator.Gui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    public void OnImagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Image image || ViewModel == null) return;

        var point = e.GetPosition(image);
        var props = e.GetCurrentPoint(image).Properties;

        if (props.IsLeftButtonPressed)
        {
            // Left click - identify element
            ViewModel.HandleImageClick((float)point.X, (float)point.Y);
        }
        else if (props.IsRightButtonPressed)
        {
            // Right click - show context menu
            ShowElementContextMenu(image, point, e);
        }
    }

    private void ShowElementContextMenu(Image image, Avalonia.Point point, PointerPressedEventArgs e)
    {
        if (ViewModel == null) return;

        // First, select the element under the cursor
        ViewModel.HandleImageClick((float)point.X, (float)point.Y);

        var contextMenu = new ContextMenu();

        if (ViewModel.SelectedElement != null)
        {
            var element = ViewModel.SelectedElement;

            // Add element-specific menu items
            switch (element.Type)
            {
                case VisualElementType.Cable:
                    contextMenu.Items.Add(new MenuItem
                    {
                        Header = $"Cable: {element.Cable?.PartNumber ?? "Unknown"}",
                        IsEnabled = false
                    });
                    contextMenu.Items.Add(new Separator());
                    contextMenu.Items.Add(CreateMenuItem("Delete Cable", () => ViewModel.DeleteSelectedElementCommand.Execute(null)));
                    break;

                case VisualElementType.Core:
                    contextMenu.Items.Add(new MenuItem
                    {
                        Header = $"Core: {element.Core?.CoreId ?? "Unknown"} ({element.Core?.InsulationColor})",
                        IsEnabled = false
                    });
                    contextMenu.Items.Add(new Separator());
                    contextMenu.Items.Add(CreateMenuItem("Assign Signal...", () => ShowSignalAssignmentDialog(element.Core)));
                    break;

                case VisualElementType.Filler:
                    contextMenu.Items.Add(new MenuItem
                    {
                        Header = "Filler",
                        IsEnabled = false
                    });
                    contextMenu.Items.Add(new Separator());
                    contextMenu.Items.Add(CreateMenuItem("Delete Filler", () => ViewModel.DeleteSelectedElementCommand.Execute(null)));
                    break;

                case VisualElementType.Annotation:
                    contextMenu.Items.Add(new MenuItem
                    {
                        Header = $"Annotation #{element.Annotation?.BalloonNumber}",
                        IsEnabled = false
                    });
                    contextMenu.Items.Add(new Separator());
                    contextMenu.Items.Add(CreateMenuItem("Edit Note...", () => EditAnnotation(element.Annotation)));
                    contextMenu.Items.Add(CreateMenuItem("Delete Annotation", () => ViewModel.DeleteSelectedElementCommand.Execute(null)));
                    break;
            }
        }
        else
        {
            // Generic menu when clicking on empty space
            contextMenu.Items.Add(CreateMenuItem("Add Annotation Here", () =>
            {
                if (ViewModel.InteractiveImage != null)
                {
                    var (mmX, mmY) = InteractiveVisualizer.ScreenToAssemblyCoords(ViewModel.InteractiveImage, (float)point.X, (float)point.Y);
                    AddAnnotationAt(mmX, mmY);
                }
            }));
        }

        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(CreateMenuItem("Add Custom Cable...", ShowCustomCableDialog));

        contextMenu.Open(image);
    }

    private static MenuItem CreateMenuItem(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private async void ShowCustomCableDialog()
    {
        if (ViewModel == null) return;

        var dialog = new CustomCableDialog();
        var result = await dialog.ShowDialog<Cable?>(this);

        if (result != null)
        {
            ViewModel.AddCustomCable(result);
        }
    }

    private void ShowSignalAssignmentDialog(CableCore? core)
    {
        if (core == null || ViewModel == null) return;

        // Simple signal assignment dialog
        var dialog = new Window
        {
            Title = $"Assign Signal to Core {core.CoreId}",
            Width = 350,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var signalNameBox = new TextBox { Text = core.SignalName, Watermark = "Signal Name (e.g., GND, VCC)" };
        var signalDescBox = new TextBox { Text = core.SignalDescription, Watermark = "Description" };
        var pinABox = new TextBox { Text = core.PinA, Watermark = "Pin A" };
        var pinBBox = new TextBox { Text = core.PinB, Watermark = "Pin B" };
        var signalTypeCombo = new ComboBox
        {
            ItemsSource = Enum.GetValues<SignalType>(),
            SelectedItem = core.SignalType
        };

        var saveButton = new Button { Content = "Save", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        saveButton.Click += (_, _) =>
        {
            core.SignalName = signalNameBox.Text ?? "";
            core.SignalDescription = signalDescBox.Text ?? "";
            core.PinA = pinABox.Text ?? "";
            core.PinB = pinBBox.Text ?? "";
            core.SignalType = (SignalType)(signalTypeCombo.SelectedItem ?? SignalType.Unassigned);
            ViewModel?.MarkChanged();
            dialog.Close();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Signal Name:", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                signalNameBox,
                new TextBlock { Text = "Signal Type:" },
                signalTypeCombo,
                new TextBlock { Text = "Description:" },
                signalDescBox,
                new TextBlock { Text = "Pin A:" },
                pinABox,
                new TextBlock { Text = "Pin B:" },
                pinBBox,
                new Border { Height = 16 },
                saveButton
            }
        };

        dialog.ShowDialog(this);
    }

    private void EditAnnotation(Annotation? annotation)
    {
        if (annotation == null || ViewModel == null) return;

        var dialog = new Window
        {
            Title = $"Edit Annotation #{annotation.BalloonNumber}",
            Width = 350,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var referenceBox = new TextBox { Text = annotation.ReferenceText, Watermark = "Reference" };
        var noteBox = new TextBox { Text = annotation.NoteText, Watermark = "Note text", AcceptsReturn = true, Height = 100 };

        var saveButton = new Button { Content = "Save", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        saveButton.Click += (_, _) =>
        {
            annotation.ReferenceText = referenceBox.Text ?? "";
            annotation.NoteText = noteBox.Text ?? "";
            ViewModel?.MarkChanged();
            ViewModel?.UpdateCrossSectionImage();
            dialog.Close();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Reference:", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                referenceBox,
                new TextBlock { Text = "Note:" },
                noteBox,
                new Border { Height = 16 },
                saveButton
            }
        };

        dialog.ShowDialog(this);
    }

    private void AddAnnotationAt(double mmX, double mmY)
    {
        if (ViewModel == null) return;

        var nextNumber = ViewModel.Assembly.Annotations.Count > 0
            ? ViewModel.Assembly.Annotations.Max(a => a.BalloonNumber) + 1
            : 1;

        var annotation = new Annotation
        {
            BalloonNumber = nextNumber,
            X = mmX,
            Y = mmY,
            ReferenceText = $"Note {nextNumber}",
            NoteText = ""
        };

        ViewModel.Assembly.Annotations.Add(annotation);
        ViewModel.MarkChanged();
        ViewModel.UpdateCrossSectionImage();

        // Open edit dialog immediately
        EditAnnotation(annotation);
    }

    private void OnCustomCableClick(object? sender, RoutedEventArgs e)
    {
        ShowCustomCableDialog();
    }

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
