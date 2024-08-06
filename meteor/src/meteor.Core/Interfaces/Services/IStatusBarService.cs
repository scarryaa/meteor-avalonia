using System;

namespace meteor.Core.Interfaces.Services
{
    public interface IStatusBarService
    {
        void SetLineAndColumn(int line, int column);
        void SetVimMode(string mode);
        void UpdateLeftSidebarButtonStyle(bool isSidebarOpen);
        void UpdateRightSidebarButtonStyle(bool isSidebarOpen);
        void UpdateTheme();
        void SetStatusBar(object statusBar);
    }
}
