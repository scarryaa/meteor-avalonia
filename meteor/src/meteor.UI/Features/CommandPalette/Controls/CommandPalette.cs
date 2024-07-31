using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using System.Linq;

namespace meteor.UI.Features.CommandPalette.Controls
{
    public class CommandPalette : UserControl
    {
        private TextBox _searchBox;
        private ListBox _resultsList;
        private ObservableCollection<string> _commands;
        private readonly IThemeManager _themeManager;

        public CommandPalette(IThemeManager themeManager)
        {
            _themeManager = themeManager;
            _commands = new ObservableCollection<string> { "Switch Theme" };
            InitializeComponent();
            ApplyTheme();
            _themeManager.ThemeChanged += (_, _) => ApplyTheme();
        }

        private void InitializeComponent()
        {
            _searchBox = new TextBox
            {
                Watermark = "Type a command...",
                Margin = new Thickness(10),
            };
            _searchBox.TextChanged += SearchBox_TextChanged;

            _resultsList = new ListBox
            {
                ItemsSource = _commands,
                Margin = new Thickness(10, 0, 10, 10),
            };

            Content = new Border
            {
                MinHeight = 100,
                MaxHeight = 400,
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(10, 0, 10, 3),
                BoxShadow = new BoxShadows(new BoxShadow
                {
                    OffsetX = 0,
                    OffsetY = 1,
                    Blur = 6,
                    Spread = 0,
                    Color = Color.FromArgb(76, Colors.Black.R, Colors.Black.G, Colors.Black.B)
                }),
                Child = new StackPanel
                {
                    Children =
                    {
                        _searchBox,
                        _resultsList
                    }
                }
            };

            this.KeyDown += CommandPalette_KeyDown;
            this.PropertyChanged += (sender, args) =>
            {
                if (args.Property.Name == nameof(IsVisible))
                {
                    CommandPalette_IsVisibleChanged(sender, args);
                }
            };
        }

        private void ApplyTheme()
        {
            var theme = _themeManager.CurrentTheme;
            Background = new SolidColorBrush(Color.Parse(theme.BackgroundBrush));

            _searchBox.Foreground = new SolidColorBrush(Color.Parse(theme.TextBrush));
            _searchBox.Background = new SolidColorBrush(Color.Parse(theme.BackgroundBrush));
            _searchBox.BorderThickness = new Thickness(0);
            _searchBox.Classes.Add("command-palette-search");
            var styles = new Styles
        {
            new Style(x => x.OfType<ListBox>().Class("command-palette-results"))
            {
                Setters =
                {
                    new Setter(ListBox.BackgroundProperty, new SolidColorBrush(Color.Parse(theme.BackgroundBrush))),
                    new Setter(ListBox.ForegroundProperty, new SolidColorBrush(Color.Parse(theme.TextBrush))),
                    new Setter(ListBox.BorderThicknessProperty, new Thickness(0))
                }
            },
            new Style(x => x.OfType<TextBox>().Class("command-palette-search"))
            {
                Setters =
                {
                    new Setter(TextBox.BackgroundProperty, new SolidColorBrush(Color.Parse(theme.BackgroundBrush))),
                    new Setter(TextBox.ForegroundProperty, new SolidColorBrush(Color.Parse(theme.TextBrush))),
                    new Setter(TextBox.BorderThicknessProperty, new Thickness(0)),
                    new Setter(TextBox.PaddingProperty, new Thickness(10)),
                    new Setter(TextBox.FontSizeProperty, 16d)
                }
            },
            new Style(x => x.OfType<TextBox>().Class("command-palette-search").Class(":focus"))
            {
                Setters =
                {
                    new Setter(TextBox.BackgroundProperty, new SolidColorBrush(Color.Parse(theme.BackgroundBrush))),
                    new Setter(TextBox.BorderBrushProperty, new SolidColorBrush(Color.Parse(theme.BorderBrush)))
                }
            },
            new Style(x => x.OfType<TextBox>().Class("command-palette-search").Class(":pointerover"))
            {
                Setters =
                {
                    new Setter(TextBox.BackgroundProperty, new SolidColorBrush(Color.Parse(theme.BackgroundBrush)))
                }
            }
        };

            if (Content is Border border)
            {
                border.BorderBrush = new SolidColorBrush(Color.Parse(theme.BorderBrush));
                border.Background = new SolidColorBrush(Color.Parse(theme.BackgroundBrush));
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = _searchBox.Text.ToLower();
            _commands.Clear();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                if ("switch theme".Contains(searchText))
                {
                    _commands.Add("Switch Theme");
                }
            }
            else
            {
                _commands.Add("Switch Theme");
            }
        }

        private void CommandPalette_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Hide();
                    break;
                case Key.Up:
                    if (_resultsList.SelectedIndex > 0)
                    {
                        _resultsList.SelectedIndex--;
                    }
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (_resultsList.SelectedIndex < _resultsList.ItemCount - 1)
                    {
                        _resultsList.SelectedIndex++;
                    }
                    e.Handled = true;
                    break;
                case Key.Enter:
                    ExecuteSelectedCommand();
                    e.Handled = true;
                    break;
            }
        }

        private void ExecuteSelectedCommand()
        {
            if (_resultsList.SelectedItem is string selectedCommand)
            {
                if (selectedCommand == "Switch Theme")
                {
                    _themeManager.ApplyTheme(_themeManager.CurrentTheme.Name == "Light" ? "Dark" : "Light");
                    Hide();
                }
            }
        }

        private void CommandPalette_IsVisibleChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                Dispatcher.UIThread.InvokeAsync(() => _searchBox.Focus());
            }
        }

        public void Show()
        {
            IsVisible = true;
            _searchBox.Text = string.Empty;
            _resultsList.SelectedIndex = -1;
        }

        public void Hide()
        {
            IsVisible = false;
            _commands.Clear();
            _commands.Add("Switch Theme");
        }
    }
}
