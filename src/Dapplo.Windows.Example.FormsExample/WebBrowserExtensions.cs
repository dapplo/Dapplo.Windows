using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;

namespace Dapplo.Windows.Example.FormsExample;

public static class WebBrowserExtensions
{
    /// <summary>
    /// Create an observable for the navigating events
    /// </summary>
    /// <param name="webBrowser">WebBrowser</param>
    /// <returns>IObservable which publishes WebBrowserNavigatingEventArgs</returns>
    public static IObservable<WebBrowserNavigatingEventArgs> OnNavigating(this WebBrowser webBrowser)
    {
        return Observable.Create<WebBrowserNavigatingEventArgs>(observer =>
        {
            void Handler(object sender, WebBrowserNavigatingEventArgs args)
            {
                observer.OnNext(args);
            }

            webBrowser.Navigating += Handler;
            return Disposable.Create(() =>
            {
                // Remove the event handlers
                webBrowser.Navigating -= Handler;
            });
        });
    }
}