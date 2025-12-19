using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CableConcentricityCalculator.Gui.ViewModels;
using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Services;
using CableConcentricityCalculator.Visualization;

namespace CableConcentricityCalculator.Gui.Views;

public partial class MainWindow : Window
{
    private Cable? _cableToMove;
    private CableLayer? _cableToMoveLayer;
    private bool _isMovingCable;

    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;

        // Wire up Add to Library button
        var addToLibraryButton = this.FindControl<Button>("AddToLibraryButton");
        if (addToLibraryButton != null)
        {
            addToLibraryButton.Click += OnAddToLibraryClick;
        }
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private async void OnAddToLibraryClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new AddToLibraryDialog();
        await dialog.ShowDialog(this);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel == null) return;

        // Delete key to remove selected element
        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            if (ViewModel.SelectedElement != null)
            {
                ViewModel.DeleteSelectedElementCommand.Execute(null);
                e.Handled = true;
            }
            else if (ViewModel.SelectedCable != null)
            {
                ViewModel.RemoveCableCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    public void OnImagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Image image || ViewModel == null) return;

        var point = e.GetPosition(image);
        var props = e.GetCurrentPoint(image).Properties;

        if (props.IsLeftButtonPressed)
        {
            // If we're in move mode, move the cable to the clicked position
            if (_isMovingCable && _cableToMove != null && _cableToMoveLayer != null)
            {
                // Convert screen coordinates to assembly coordinates
                if (ViewModel.InteractiveImage != null)
                {
                    var (mmX, mmY) = InteractiveVisualizer.ScreenToAssemblyCoords(
                        ViewModel.InteractiveImage, (float)point.X, (float)point.Y);

                    // Move the cable to nearest valley
                    MoveCableToNearestValley(_cableToMove, _cableToMoveLayer, mmX, mmY);
                }

                // Exit move mode
                _isMovingCable = false;
                _cableToMove = null;
                _cableToMoveLayer = null;
            }
            else
            {
                // Normal left click - identify element
                ViewModel.HandleImageClick((float)point.X, (float)point.Y);
            }
        }
        else if (props.IsRightButtonPressed)
        {
            // Right click - show context menu
            ShowElementContextMenu(image, point, e);
        }
    }

    public void OnImagePointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Image img) return;

        // Update cursor when in move mode
        if (_isMovingCable)
        {
            img.Cursor = new Cursor(StandardCursorType.Cross);
        }
        else
        {
            img.Cursor = new Cursor(StandardCursorType.Hand);
        }
    }

    public void OnImagePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // No longer needed for move mode
    }

    private void StartMoveCable(Cable? cable)
    {
        if (cable == null || ViewModel?.SelectedLayer == null) return;

        _cableToMove = cable;
        _cableToMoveLayer = ViewModel.SelectedLayer;
        _isMovingCable = true;

        if (ViewModel != null)
        {
            ViewModel.StatusMessage = $"Click where you want to move {cable.PartNumber}";
        }
    }

    private void MoveCableToNearestValley(Cable cable, CableLayer layer, double targetX, double targetY)
    {
        if (ViewModel == null) return;

        // Get previous layer positions
        var previousLayerIndex = layer.LayerNumber - 1;
        if (previousLayerIndex < 0 || previousLayerIndex >= ViewModel.Assembly.Layers.Count)
        {
            ViewModel.StatusMessage = "Cannot move cable - no previous layer";
            return;
        }

        var previousLayerPositions = ConcentricityCalculator.CalculateCablePositions(
            ViewModel.Assembly, previousLayerIndex);

        if (previousLayerPositions.Count == 0)
        {
            ViewModel.StatusMessage = "Cannot move cable - no previous layer positions";
            return;
        }

        // Get all current cables except the one being dragged
        var otherCables = layer.Cables.Where(c => c.CableId != cable.CableId).ToList();

        // Calculate all possible valley positions
        var validValleyPositions = new List<(double X, double Y, int Index1, int Index2)>();

        for (int i = 0; i < previousLayerPositions.Count; i++)
        {
            for (int j = i + 1; j < previousLayerPositions.Count; j++)
            {
                var cable1 = previousLayerPositions[i];
                var cable2 = previousLayerPositions[j];

                // Try to calculate valley position for this cable
                var valley = ConcentricityCalculator.CalculateValleyPosition(
                    cable1.X, cable1.Y, cable1.Diameter,
                    cable2.X, cable2.Y, cable2.Diameter,
                    cable.OuterDiameter);

                if (valley.HasValue)
                {
                    validValleyPositions.Add((valley.Value.x, valley.Value.y, i, j));
                }
            }
        }

        if (validValleyPositions.Count == 0)
        {
            ViewModel.StatusMessage = "No valid valley positions found";
            return;
        }

        // Find the closest valley to the target position
        var closestValley = validValleyPositions
            .OrderBy(v =>
            {
                var dx = v.X - targetX;
                var dy = v.Y - targetY;
                return Math.Sqrt(dx * dx + dy * dy);
            })
            .First();

        // Move the cable to the start of the list to give it priority in valley packing
        // This will cause the optimization algorithm to place it first
        var cableIndex = layer.Cables.IndexOf(cable);
        if (cableIndex >= 0)
        {
            layer.Cables.RemoveAt(cableIndex);
            layer.Cables.Insert(0, cable);

            ViewModel.MarkChanged();
            ViewModel.UpdateCrossSectionImage();
            ViewModel.StatusMessage = $"Moved {cable.PartNumber} to valley at ({closestValley.X:F2}, {closestValley.Y:F2}) mm";
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

                    // Add "Move Cable" option if partial layer optimization is enabled
                    if (ViewModel.SelectedLayer != null &&
                        ViewModel.SelectedLayer.UsePartialLayerOptimization &&
                        ViewModel.SelectedLayer.LayerNumber > 0)
                    {
                        contextMenu.Items.Add(CreateMenuItem("Move Cable...", () => StartMoveCable(element.Cable)));
                        contextMenu.Items.Add(new Separator());
                    }

                    contextMenu.Items.Add(CreateMenuItem("Edit Jacket Color...", () => ShowCableColorDialog(element.Cable)));
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

    private void ShowCableColorDialog(Cable? cable)
    {
        if (cable == null || ViewModel == null) return;

        var dialog = new Window
        {
            Title = $"Edit Jacket Color - {cable.PartNumber}",
            Width = 300,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var colorCombo = new ComboBox
        {
            ItemsSource = new[] { "Black", "White", "Red", "Green", "Blue", "Yellow", "Orange", "Brown", "Violet", "Purple", "Gray", "Pink", "Natural", "Clear", "Silver", "Tan", "Nylon" },
            SelectedItem = cable.JacketColor
        };

        var saveButton = new Button { Content = "Save", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        saveButton.Click += (_, _) =>
        {
            cable.JacketColor = colorCombo.SelectedItem?.ToString() ?? cable.JacketColor;
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
                new TextBlock { Text = "Jacket Color:", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                colorCombo,
                new Border { Height = 16 },
                saveButton
            }
        };

        dialog.ShowDialog(this);
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

    private async void OnAddCableClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedLayer == null)
        {
            ViewModel!.StatusMessage = "Select a layer first";
            return;
        }

        var dialog = new CableBrowserDialog();
        var result = await dialog.ShowDialog<List<Cable>?>(this);

        if (result != null && result.Count > 0)
        {
            foreach (var cable in result)
            {
                ViewModel.SelectedLayer.Cables.Add(cable);
            }
            ViewModel.SelectedCable = result.Last();
            ViewModel.MarkChanged();
            ViewModel.UpdateCrossSectionImage();
            ViewModel.StatusMessage = $"Added {result.Count} cable(s) to Layer {ViewModel.SelectedLayer.LayerNumber}";
        }
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

    private void OnPartialLayerToggled(object? sender, RoutedEventArgs e)
    {
        // When checkbox is toggled, recalculate and update visualization
        if (sender is CheckBox checkbox && ViewModel?.SelectedLayer != null)
        {
            // CRITICAL FIX: Manually set the property because the binding hasn't updated yet
            ViewModel.SelectedLayer.UsePartialLayerOptimization = checkbox.IsChecked ?? false;

            DebugLogger.Log($"[UI] Partial layer checkbox toggled for Layer {ViewModel.SelectedLayer.LayerNumber}, IsChecked={checkbox.IsChecked}, UsePartialLayerOptimization={ViewModel.SelectedLayer.UsePartialLayerOptimization}");

            // Valley packing requires same twist direction as previous layer
            if (checkbox.IsChecked == true && ViewModel.SelectedLayer.LayerNumber > 0)
            {
                var prevLayer = ViewModel.Assembly.Layers.FirstOrDefault(l => l.LayerNumber == ViewModel.SelectedLayer.LayerNumber - 1);
                if (prevLayer != null && prevLayer.TwistDirection != ViewModel.SelectedLayer.TwistDirection)
                {
                    DebugLogger.Log($"[UI] Auto-correcting twist direction for valley packing: {ViewModel.SelectedLayer.TwistDirection} -> {prevLayer.TwistDirection}");
                    ViewModel.SelectedLayer.TwistDirection = prevLayer.TwistDirection;
                    ViewModel.StatusMessage = $"Layer {ViewModel.SelectedLayer.LayerNumber} twist direction auto-corrected to {prevLayer.TwistDirection} to match previous layer (required for valley packing)";
                }
            }
        }
        ViewModel?.MarkChanged();
        ViewModel?.UpdateCrossSectionImage();
    }

    private async void OnDuplicateCableClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedLayer == null || ViewModel.SelectedCable == null)
        {
            ViewModel!.StatusMessage = "No cable selected to duplicate";
            return;
        }

        var cableToDuplicate = ViewModel.SelectedCable;

        // Create a simple dialog to get the quantity
        var dialog = new Window
        {
            Title = $"Duplicate Cable: {cableToDuplicate.PartNumber}",
            Width = 350,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var quantityUpDown = new NumericUpDown
        {
            Value = 1,
            Minimum = 1,
            Maximum = 50,
            Increment = 1
        };

        var duplicateButton = new Button
        {
            Content = "Duplicate",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(0, 0, 8, 0)
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        bool? dialogResult = null;

        duplicateButton.Click += (_, _) =>
        {
            dialogResult = true;
            dialog.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            dialogResult = false;
            dialog.Close();
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8,
            Children = { duplicateButton, cancelButton }
        };

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "How many copies would you like to add?",
                    FontWeight = Avalonia.Media.FontWeight.SemiBold
                },
                new TextBlock
                {
                    Text = $"Cable: {cableToDuplicate.PartNumber}",
                    Foreground = Avalonia.Media.Brushes.Gray,
                    FontSize = 11
                },
                new Border { Height = 8 },
                new TextBlock { Text = "Quantity:" },
                quantityUpDown,
                new Border { Height = 16 },
                buttonPanel
            }
        };

        await dialog.ShowDialog(this);

        if (dialogResult == true)
        {
            int quantity = (int)quantityUpDown.Value;

            for (int i = 0; i < quantity; i++)
            {
                // Create a deep clone of the cable
                var clonedCable = CloneCable(cableToDuplicate);
                ViewModel.SelectedLayer.Cables.Add(clonedCable);
            }

            ViewModel.MarkChanged();
            ViewModel.UpdateCrossSectionImage();
            ViewModel.StatusMessage = $"Added {quantity} duplicate(s) of {cableToDuplicate.PartNumber} to Layer {ViewModel.SelectedLayer.LayerNumber}";
        }
    }

    private Cable CloneCable(Cable original)
    {
        // Create a deep clone of the cable with a new unique ID
        var clone = new Cable
        {
            CableId = Guid.NewGuid().ToString("N")[..8], // New unique ID
            PartNumber = original.PartNumber,
            Manufacturer = original.Manufacturer,
            Name = original.Name,
            Type = original.Type,
            JacketThickness = original.JacketThickness,
            JacketColor = original.JacketColor,
            HasShield = original.HasShield,
            ShieldType = original.ShieldType,
            ShieldThickness = original.ShieldThickness,
            ShieldCoverage = original.ShieldCoverage,
            HasDrainWire = original.HasDrainWire,
            DrainWireDiameter = original.DrainWireDiameter,
            IsFiller = original.IsFiller,
            FillerMaterial = original.FillerMaterial,
            Description = original.Description,
            SpecifiedOuterDiameter = original.SpecifiedOuterDiameter
        };

        // Clone cores
        foreach (var core in original.Cores)
        {
            var clonedCore = new CableCore
            {
                CoreId = Guid.NewGuid().ToString("N")[..6], // New unique ID
                ConductorDiameter = core.ConductorDiameter,
                InsulationThickness = core.InsulationThickness,
                InsulationColor = core.InsulationColor,
                SignalName = core.SignalName,
                SignalDescription = core.SignalDescription,
                SignalType = core.SignalType,
                PinA = core.PinA,
                PinB = core.PinB
            };
            clone.Cores.Add(clonedCore);
        }

        return clone;
    }

    private async void OnRotateLayerClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is CableLayer layer && ViewModel != null)
        {
            // Create rotation dialog
            var dialog = new Window
            {
                Title = $"Rotate Layer {layer.LayerNumber}",
                Width = 350,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var currentAngleText = new TextBlock
            {
                Text = $"Current rotation: {layer.RotationAngle:F1}°",
                Foreground = Avalonia.Media.Brushes.Gray,
                FontSize = 11,
                Margin = new Avalonia.Thickness(0, 0, 0, 8)
            };

            var rotationUpDown = new NumericUpDown
            {
                Value = (decimal)layer.RotationAngle,
                Minimum = 0,
                Maximum = 360,
                Increment = 1,
                FormatString = "0.0°"
            };

            var applyButton = new Button
            {
                Content = "Apply",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Avalonia.Thickness(0, 0, 8, 0)
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };

            bool? dialogResult = null;

            applyButton.Click += (_, _) =>
            {
                dialogResult = true;
                dialog.Close();
            };

            cancelButton.Click += (_, _) =>
            {
                dialogResult = false;
                dialog.Close();
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 8,
                Children = { applyButton, cancelButton }
            };

            dialog.Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Enter rotation angle (0-360°):",
                        FontWeight = Avalonia.Media.FontWeight.SemiBold
                    },
                    currentAngleText,
                    new TextBlock { Text = "New rotation angle:" },
                    rotationUpDown,
                    new Border { Height = 16 },
                    buttonPanel
                }
            };

            await dialog.ShowDialog(this);

            if (dialogResult == true)
            {
                layer.RotationAngle = (double)(rotationUpDown.Value ?? 0);
                ViewModel.MarkChanged();
                ViewModel.UpdateCrossSectionImage();
                ViewModel.StatusMessage = $"Layer {layer.LayerNumber} rotated to {layer.RotationAngle:F1}°";
            }
        }
    }
}
