using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Threading;
using meteor.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace meteor.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private TabViewModel? _selectedTab;
    private double _windowWidth;
    private double _windowHeight;

    public MainWindowViewModel(
        StatusPaneViewModel statusPaneViewModel,
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel,
        ICursorPositionService cursorPositionService,
        IServiceProvider serviceProvider)
    {
        StatusPaneViewModel = statusPaneViewModel;

        NewTabCommand = ReactiveCommand.Create(NewTab);
        CloseTabCommand = ReactiveCommand.Create<TabViewModel>(CloseTab);

        Tabs = new ObservableCollection<TabViewModel>
        {
            new()
            {
                Title = "Tab 1",
                ScrollableTextEditorViewModel = new ScrollableTextEditorViewModel(
                    cursorPositionService,
                    fontPropertiesViewModel,
                    lineCountViewModel,
                    new TextBuffer()
                ),
                CloseTabCommand = CloseTabCommand
            },
            new()
            {
                Title = "Tab 2",
                ScrollableTextEditorViewModel = new ScrollableTextEditorViewModel(
                    cursorPositionService,
                    fontPropertiesViewModel,
                    lineCountViewModel,
                    new TextBuffer()
                ),
                CloseTabCommand = CloseTabCommand
            }
        };

        SelectedTab = Tabs.FirstOrDefault();

        this.WhenAnyValue(x => x.SelectedTab)
            .Subscribe(tab =>
            {
                if (tab != null)
                {
                    var scrollableTextEditorVm = tab.ScrollableTextEditorViewModel;
                    scrollableTextEditorVm.UpdateViewProperties();
                    scrollableTextEditorVm.WindowHeight = WindowHeight;
                    scrollableTextEditorVm.WindowWidth = WindowWidth;

                    scrollableTextEditorVm.TextEditorViewModel.TextBuffer =
                        tab.ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer;

                    scrollableTextEditorVm.TextEditorViewModel.OnInvalidateRequired();
                    cursorPositionService.UpdateCursorPosition(
                        scrollableTextEditorVm.TextEditorViewModel.CursorPosition,
                        scrollableTextEditorVm.TextEditorViewModel.TextBuffer.LineStarts);
                }
            });

        this.WhenAnyValue(x => x.WindowHeight)
            .Subscribe(height =>
            {
                foreach (var tab in Tabs) tab.ScrollableTextEditorViewModel.WindowHeight = height;
            });

        this.WhenAnyValue(x => x.WindowWidth)
            .Subscribe(width =>
            {
                foreach (var tab in Tabs) tab.ScrollableTextEditorViewModel.WindowWidth = width;
            });
    }

    public TabViewModel SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (_selectedTab != null)
            {
                _selectedTab.SavedVerticalOffset = _selectedTab.ScrollableTextEditorViewModel.VerticalOffset;
                _selectedTab.SavedHorizontalOffset = _selectedTab.ScrollableTextEditorViewModel.HorizontalOffset;
            }

            this.RaiseAndSetIfChanged(ref _selectedTab, value);

            if (_selectedTab != null)
                Dispatcher.UIThread.Post(() =>
                {
                    _selectedTab.ScrollableTextEditorViewModel.TextEditorViewModel.Focus();
                    _selectedTab.ScrollableTextEditorViewModel.VerticalOffset = _selectedTab.SavedVerticalOffset;
                    _selectedTab.ScrollableTextEditorViewModel.HorizontalOffset = _selectedTab.SavedHorizontalOffset;

                    _selectedTab.ScrollableTextEditorViewModel.UpdateLongestLineWidth();
                }, DispatcherPriority.Render);
        }
    }

    public ObservableCollection<TabViewModel> Tabs { get; }
    public StatusPaneViewModel StatusPaneViewModel { get; }

    public double WindowWidth
    {
        get => _windowWidth;
        set => this.RaiseAndSetIfChanged(ref _windowWidth, value);
    }

    public double WindowHeight
    {
        get => _windowHeight;
        set => this.RaiseAndSetIfChanged(ref _windowHeight, value);
    }

    public ICommand NewTabCommand { get; }
    public ICommand CloseTabCommand { get; }

    private void NewTab()
    {
        var newTab = new TabViewModel
        {
            Title = $"Tab {Tabs.Count + 1}",
            ScrollableTextEditorViewModel = new ScrollableTextEditorViewModel(
                App.ServiceProvider.GetRequiredService<ICursorPositionService>(),
                App.ServiceProvider.GetRequiredService<FontPropertiesViewModel>(),
                App.ServiceProvider.GetRequiredService<LineCountViewModel>(),
                new TextBuffer()
            ),
            CloseTabCommand = CloseTabCommand
        };

        Tabs.Add(newTab);
        SelectedTab = newTab;

        if (SelectedTab != null)
            SelectedTab.ScrollableTextEditorViewModel.Viewport = new Size(WindowWidth, WindowHeight);
    }

    private void CloseTab(TabViewModel tab)
    {
        if (Tabs.Contains(tab))
        {
            Tabs.Remove(tab);
            if (SelectedTab == tab) SelectedTab = Tabs.FirstOrDefault();
        }
    }
}