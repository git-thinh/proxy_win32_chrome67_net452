using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using System.Reflection;
using System.Diagnostics;

namespace proxy_win32
{
    static class App
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string pathCache = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Cache");
            if (!Directory.Exists(pathCache)) Directory.CreateDirectory(pathCache);
            if (!Directory.Exists("wcd")) Directory.CreateDirectory("wcd");

            CefSettings settings = new CefSettings() { CachePath = pathCache };
            //settings.RemoteDebuggingPort = 55555;
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "http",
                DomainName = "192.168.56.102",
                SchemeHandlerFactory = new CefSharpSchemeHandlerFactory()
            });

            if (!Cef.Initialize(settings))
            {
                Console.WriteLine("Couldn't initialise CEF");
                return;
            }

            //CEF.RegisterScheme("test", new TestSchemeHandlerFactory());
            //CEF.RegisterJsObject("bound", new BoundObject());

            //Application.Run(new TabulationDemoForm());
            Application.Run(new fProxyBrowser());

            STORE.ClearAll();
            Cef.Shutdown();
        }
    }
}
