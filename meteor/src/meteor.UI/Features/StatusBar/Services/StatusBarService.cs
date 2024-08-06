using System;
using meteor.Core.Interfaces.Services;
using meteor.UI.Features.StatusBar.Controls;

namespace meteor.UI.Features.StatusBar.Services
{
    public class StatusBarService : IStatusBarService
    {
        private StatusBar.Controls.StatusBar _statusBar;

        public void SetLineAndColumn(int line, int column)
        {
            _statusBar.SetLineAndColumn(line, column);
        }

        public void SetVimMode(string mode)
        {
            _statusBar.SetVimMode(mode);
        }

        public void UpdateLeftSidebarButtonStyle(bool isSidebarOpen)
        {
            _statusBar.UpdateLeftSidebarButtonStyle(isSidebarOpen);
        }

        public void UpdateRightSidebarButtonStyle(bool isSidebarOpen)
        {
            _statusBar.UpdateRightSidebarButtonStyle(isSidebarOpen);
        }

        public void UpdateTheme()
        {
            _statusBar.UpdateTheme();
        }

        public void SetStatusBar(object statusBar)
        {
            _statusBar = statusBar as StatusBar.Controls.StatusBar;
        }
    }
}
