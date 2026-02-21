using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using ElevatorControlSystem.Services;
using ElevatorControlSystem.Models;
using ElevatorControlSystem.Constants;
using ElevatorControlSystem.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElevatorControlSystem.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ElevatorController? _controller;
        private RandomRequestGenerator? _generator;
        private CancellationTokenSource? _generatorCts;
        private readonly Dictionary<int, Rectangle> _elevatorRects = new();
        private readonly Dictionary<(int Floor, Direction Dir), Button> _floorCallButtons = new();
        private bool _floorButtonsEnabled = false;
        private readonly Brush _floorHighlightBrush = Brushes.OrangeRed;
        private readonly DispatcherTimer _uiTimer = new();

        public MainWindow()
        {
            InitializeComponent();

            _uiTimer.Interval = TimeSpan.FromMilliseconds(500);
            _uiTimer.Tick += UiTimer_Tick;
            BuildFloorCallButtons();
            // initially disable floor call buttons until simulation starts
            UpdateFloorButtonsEnabled(false);
        }

        private void BuildFloorCallButtons()
        {
            FloorCallsPanel.Children.Clear();

            int floors = BuildingConstants.TotalFloors;

            // We want top floor at top of panel
            for (int floor = floors; floor >= 1; floor--)
            {
                var rowGrid = new Grid { Height = 40, Margin = new Thickness(0, 2, 0, 2) };
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

                // Up button (only if not top floor)
                if (floor < floors)
                {
                    var upBtn = new Button
                    {
                        Content = "↑",
                        Tag = (floor, Direction.Up),
                        Margin = new Thickness(2),
                        Padding = new Thickness(0),
                        Width = 30,
                        Height = 30,
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Background = Brushes.LightGray,
                        BorderBrush = Brushes.DarkSlateGray,
                        BorderThickness = new Thickness(2),
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };
                    int f = floor; // local copy for closure
                    upBtn.Click += (_, __) => OnFloorCall(f, Direction.Up);
                    Grid.SetColumn(upBtn, 0);
                    rowGrid.Children.Add(upBtn);
                    _floorCallButtons[(floor, Direction.Up)] = upBtn;
                }

                // Down button (only if not ground floor)
                if (floor > 1)
                {
                    var downBtn = new Button
                    {
                        Content = "↓",
                        Tag = (floor, Direction.Down),
                        Margin = new Thickness(2),
                        Padding = new Thickness(0),
                        Width = 30,
                        Height = 30,
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Background = Brushes.LightGray,
                        BorderBrush = Brushes.DarkSlateGray,
                        BorderThickness = new Thickness(2),
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };
                    int f2 = floor;
                    downBtn.Click += (_, __) => OnFloorCall(f2, Direction.Down);
                    Grid.SetColumn(downBtn, 1);
                    rowGrid.Children.Add(downBtn);
                    _floorCallButtons[(floor, Direction.Down)] = downBtn;
                }

                // Floor label
                var lbl = new TextBlock { Text = floor.ToString(), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                Grid.SetColumn(lbl, 2);
                rowGrid.Children.Add(lbl);

                FloorCallsPanel.Children.Add(rowGrid);
            }
            UpdateFloorButtonsVisuals();
        }

        private void UpdateFloorButtonsEnabled(bool enabled)
        {
            _floorButtonsEnabled = enabled;
            foreach (var btn in _floorCallButtons.Values)
            {
                btn.IsEnabled = enabled;
            }
        }

        private void UpdateFloorButtonsVisuals()
        {
            // Highlight buttons with active requests and disable the specific direction to prevent duplicates
            foreach (var kv in _floorCallButtons)
            {
                // default: clear
                kv.Value.ClearValue(Button.BackgroundProperty);
                // ensure enabled state follows global flag (may be overridden below if request active)
                kv.Value.IsEnabled = _floorButtonsEnabled;
            }

            if (_controller == null) return;

            var active = _controller.ActiveRequests.Select(r => (r.Floor, r.Direction)).ToHashSet();

            foreach (var kv in _floorCallButtons)
            {
                if (active.Contains((kv.Key.Floor, kv.Key.Dir)))
                {
                    kv.Value.Background = _floorHighlightBrush;
                    // disable the specific button while the request is active to avoid duplicates
                    kv.Value.IsEnabled = false;
                }
            }
        }

        private void OnFloorCall(int floor, Direction direction)
        {
            if (_controller == null)
            {
                AppendLog($"No simulation running. Start simulation to send requests.");
                return;
            }

            var request = new ElevatorRequest(floor, direction);
            _controller.RequestElevator(request);
            AppendLog($"Manual request: floor {floor} {direction}");
            RefreshRequests();
            UpdateFloorButtonsVisuals();
        }

        private void RefreshRequests()
        {
            if (_controller == null) return;

            RequestsListBox.Items.Clear();
            foreach (var r in _controller.ActiveRequests)
            {
                RequestsListBox.Items.Add($"Floor {r.Floor} - {r.Direction}");
            }
        }

        private void RefreshElevatorsList()
        {
            if (_controller == null) return;

            ElevatorsListBox.Items.Clear();
            foreach (var e in _controller.Elevators)
            {
                ElevatorsListBox.Items.Add($"E{e.Id}: Floor {e.CurrentFloor} - {e.State} ({e.CurrentDirection})");
            }
        }

        private void BuildShafts()
        {
            ShaftsPanel.Children.Clear();

            int floors = BuildingConstants.TotalFloors;
            int elevators = BuildingConstants.TotalElevators;

            var shaftsGrid = new Grid();
            shaftsGrid.HorizontalAlignment = HorizontalAlignment.Left;

            // create columns for each elevator
            for (int c = 0; c < elevators; c++)
            {
                shaftsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            }

            // create rows for floors (top to bottom)
            for (int r = 0; r < floors; r++)
            {
                shaftsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            }

            // For each elevator, create a canvas that spans all rows
            for (int i = 0; i < elevators; i++)
            {
                var canvas = new Canvas { Width = 110, Height = floors * 40 };

                // draw floor lines and labels
                for (int f = 0; f < floors; f++)
                {
                    var y = f * 40;
                    var line = new System.Windows.Shapes.Line
                    {
                        X1 = 0,
                        X2 = 110,
                        Y1 = y,
                        Y2 = y,
                        Stroke = Brushes.LightGray,
                        StrokeThickness = 1
                    };
                    canvas.Children.Add(line);

                    var floorLabel = new TextBlock
                    {
                        Text = (floors - f).ToString(),
                        Foreground = Brushes.Black
                    };
                    Canvas.SetTop(floorLabel, y + 2);
                    Canvas.SetLeft(floorLabel, 2);
                    canvas.Children.Add(floorLabel);
                }

                // elevator rectangle (will be positioned) with visual style and shadow
                var rect = new Rectangle
                {
                    Width = 60,
                    Height = 34,
                    RadiusX = 6,
                    RadiusY = 6,
                    Fill = new LinearGradientBrush(Colors.SteelBlue, Colors.LightSkyBlue, 90),
                    Stroke = Brushes.DarkBlue,
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(rect, 22);
                // default position: bottom floor
                Canvas.SetTop(rect, (floors - 1) * 40);
                rect.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 8, ShadowDepth = 2, Opacity = 0.45 };
                canvas.Children.Add(rect);

                // store reference by elevator id (1-based)
                _elevatorRects[i + 1] = rect;

                // place canvas into a border for visual separation
                var border = new Border { BorderBrush = Brushes.DarkGray, BorderThickness = new Thickness(1), Margin = new Thickness(4), Child = canvas };

                // wrap into a StackPanel with elevator label
                var panel = new StackPanel { Orientation = Orientation.Vertical };
                panel.Children.Add(new TextBlock { Text = $"Elevator {i + 1}", FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
                panel.Children.Add(border);

                Grid.SetColumn(panel, i);
                Grid.SetRowSpan(panel, floors);
                shaftsGrid.Children.Add(panel);
            }

            ShaftsPanel.Children.Add(shaftsGrid);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // set travel/load times from inputs
            if (int.TryParse(TravelTimeTextBox.Text, out var tt)) BuildingConstants.FloorTravelTimeSeconds = Math.Max(0, tt);
            if (int.TryParse(LoadTimeTextBox.Text, out var lt)) BuildingConstants.LoadingTimeSeconds = Math.Max(0, lt);

            // prepare elevators and controller
            var elevators = new List<Elevator>();
            for (int i = 1; i <= BuildingConstants.TotalElevators; i++)
            {
                elevators.Add(new Elevator(i, 1));
            }

            _controller = new ElevatorController(elevators, new SimpleElevatorDispatcher());

            // subscribe to elevator events for logging and UI updates
            foreach (var elev in _controller.Elevators)
            {
                elev.OnFloorChanged += Elevator_OnFloorChanged;
                elev.OnStateChanged += Elevator_OnStateChanged;
            }

            BuildShafts();

            _controller.Start();

            // enable floor call buttons now that simulation is running
            UpdateFloorButtonsEnabled(true);
            UpdateFloorButtonsVisuals();

            // start random generator
            _generator = new RandomRequestGenerator(_controller);
            _generatorCts = new CancellationTokenSource();
            _ = Task.Run(() => _generator.StartGeneratingRequestsAsync(_generatorCts.Token));

            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            _uiTimer.Start();

            AppendLog("Simulation started.");
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopButton.IsEnabled = false;
            _uiTimer.Stop();

            if (_generatorCts != null)
            {
                _generatorCts.Cancel();
                _generatorCts = null;
            }

            if (_controller != null)
            {
                await _controller.StopAsync();
                AppendLog("Simulation stopped.");
            }

            StartButton.IsEnabled = true;
            // disable floor call buttons when stopped
            UpdateFloorButtonsEnabled(false);
            UpdateFloorButtonsVisuals();
        }

        private void Elevator_OnStateChanged(int elevatorId, ElevatorState state)
        {
            Dispatcher.Invoke(() =>
            {
                AppendLog($"Elevator {elevatorId} state: {state}");
                RefreshElevatorPosition(elevatorId);
                UpdateFloorButtonsVisuals();
            });
        }

        private void Elevator_OnFloorChanged(int elevatorId, int floor, Direction direction)
        {
            Dispatcher.Invoke(() =>
            {
                AppendLog($"Elevator {elevatorId} floor: {floor} ({direction})");
                RefreshElevatorPosition(elevatorId);
                UpdateFloorButtonsVisuals();
            });
        }

        private void RefreshElevatorPosition(int elevatorId)
        {
            if (_controller == null) return;
            if (!_elevatorRects.ContainsKey(elevatorId)) return;

            var e = _controller.Elevators.FirstOrDefault(x => x.Id == elevatorId);
            if (e == null) return;

            var rect = _elevatorRects[elevatorId];
            int floors = BuildingConstants.TotalFloors;
            double floorHeight = 40;
            double top = (floors - e.CurrentFloor) * floorHeight;
            // animate movement to new top position
            var anim = new DoubleAnimation
            {
                To = top,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            rect.BeginAnimation(Canvas.TopProperty, anim);
        }

        private void AppendLog(string text)
        {
            LogTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - {text}\n");
            LogTextBox.ScrollToEnd();
        }

        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            RefreshRequests();
            RefreshElevatorsList();
            UpdateFloorButtonsVisuals();
        }
    }
}