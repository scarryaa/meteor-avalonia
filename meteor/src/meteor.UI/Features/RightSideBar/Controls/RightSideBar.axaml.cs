using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using meteor.Core.Models;
using meteor.UI.ViewModels;
using Color = Avalonia.Media.Color;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;

namespace meteor.UI.Features.RightSideBar.Controls
{
    public class RightSideBar : UserControl
    {
        private MainWindowViewModel _viewModel;
        private readonly IThemeManager _themeManager;

        public RightSideBar(IThemeManager themeManager)
        {
            _themeManager = themeManager;
            Background = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BackgroundColor));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is MainWindowViewModel viewModel)
            {
                _viewModel = viewModel;
            }
        }

        public void ToggleVisibility()
        {
            IsVisible = !IsVisible;
        }

        internal void UpdateBackground(Theme theme)
        {
            Background = new SolidColorBrush(Color.Parse(theme.BackgroundColor));
        }
    }
}
