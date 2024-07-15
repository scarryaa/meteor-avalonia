using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using meteor.Core.Interfaces;

namespace meteor.App.Services
{
    public class AvaloniaClipboardService : IClipboardService
    {
        private readonly Func<Visual?> _getVisual;
        private IClipboard? _clipboard;

        public AvaloniaClipboardService(Func<Visual?> getVisual)
        {
            _getVisual = getVisual;
        }

        private IClipboard Clipboard => _clipboard ??= GetClipboard();

        private IClipboard GetClipboard()
        {
            var visual = _getVisual() ?? throw new InvalidOperationException("Visual is not available.");
            var clipboard = TopLevel.GetTopLevel(visual)?.Clipboard;
            return clipboard ?? throw new InvalidOperationException("Clipboard is not available.");
        }

        public async Task<string> GetTextAsync()
        {
            try
            {
                var clipboardText = await Clipboard.GetTextAsync();
                return clipboardText ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting text from clipboard: {ex.Message}");
                return string.Empty;
            }
        }

        public Task SetTextAsync(string text)
        {
            return Clipboard.SetTextAsync(text);
        }
    }
}