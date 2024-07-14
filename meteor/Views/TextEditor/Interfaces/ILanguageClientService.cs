using System;
using System.Threading.Tasks;

namespace meteor.Views.Interfaces;

public interface ILanguageClientService
{
    event EventHandler LspInitialized;
    Task StartServerAsync(string serverPath, string workspacePath);
    Task SendNotificationAsync(string method, object parameters);
    Task<T> SendRequestAsync<T>(string method, object parameters);
}