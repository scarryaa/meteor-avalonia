using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Services;
using meteor.UI.Features.SearchView.ViewModels;

namespace meteor.UI.Features.SearchView.Controls
{
    public partial class SearchView : UserControl
    {
        private readonly ISearchService _searchService;
        private readonly IThemeManager _themeManager;
        private SearchViewModel _viewModel;
        private TextBox _searchBox;
        private ListBox _resultsList;

        public SearchView(ISearchService searchService, IThemeManager themeManager)
        {
            _searchService = searchService;
            _themeManager = themeManager;
            _viewModel = new SearchViewModel(searchService);
            DataContext = _viewModel;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _searchBox = new TextBox { Name = "SearchBox", Margin = new Thickness(5) };
            _resultsList = new ListBox { Name = "ResultsList", Margin = new Thickness(5) };

            var stackPanel = new StackPanel
            {
                Children =
                {
                    _searchBox,
                    _resultsList
                }
            };

            Content = stackPanel;

            _searchBox.TextChanged += async (s, e) =>
            {
                await PerformSearch();
            };

            UpdateTheme();
            _themeManager.ThemeChanged += (_, _) => UpdateTheme();
        }

        private async Task PerformSearch()
        {
            await _viewModel.ExecuteSearchCommand.ExecuteAsync(null);
        }

        private void UpdateTheme()
        {
            if (_themeManager.CurrentTheme != null)
            {
                Background = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BackgroundColor));
            }
        }
    }
}
