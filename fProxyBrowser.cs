using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace proxy_win32
{
    public static class STORE
    {
        static StringBuilder _log = new StringBuilder();

        static List<string> _links = new List<string>();
        public static void link_Add(string url) { url = url.Split('?')[0].Trim(); lock (_links) if (_links.Contains(url) == false) _links.Add(url); }
        public static string link_getAll() { lock (_links) return string.Join(Environment.NewLine, _links); }
        public static string[] link_find(Func<string, bool> predicate) { lock (_links) return _links.Where(predicate).ToArray(); }
        public static void link_Clear() { lock (_links) _links.Clear(); }

        static ConcurrentDictionary<string, string> _data = new ConcurrentDictionary<string, string>();
        static STORE()
        {
            string[] fs = Directory.GetFiles("wcd");
            foreach (string file in fs) _data.TryAdd(Path.GetFileName(file), File.ReadAllText(file));
        }

        public static string cache_Get(string path) { if (_data.ContainsKey(path)) return _data[path]; return null; }
        public static void cache_Write(string path, string data) { if (_data.ContainsKey(path)) _data.TryAdd(path, data); }
        public static bool cache_Exist(string path) { return _data.ContainsKey(path); }

        public static void log_Write(string text) { lock (_log) _log.AppendLine(text); }
        public static string log_Read() { lock (_log) return _log.ToString(); }
        public static void log_Clear() { lock (_log) _log.Clear(); }

        public static void ClearAll()
        {
            _log.Clear();
            _links.Clear();
            _data.Clear();
        }
    }

    public class fProxyBrowser : Form
    {
        readonly ChromiumWebBrowser _browser;
        public fProxyBrowser()
        {
            ////this.FormBorderStyle = FormBorderStyle.None;
            this.Text = "";
            this.Icon = Resources.icon;

            _browser = new ChromiumWebBrowser("about:blank")
            {
                Dock = DockStyle.Fill,
                BrowserSettings =
                {
                    DefaultEncoding = "UTF-8",
                    WebGl = CefState.Disabled
                }
            };
            this.Controls.Add(_browser);
            _browser.MenuHandler = new MyCustomMenuHandler();
            _browser.IsBrowserInitializedChanged += (se, ev) =>
            {
                //Debug.WriteLine("FRAME_LOAD_START: " + ev.Url);
                _browser.ShowDevTools();
            };
            var lblMove = new Label()
            {
                Width = 15,
                Height = 15,
                BackColor = Color.Orange,
                Top = 0,
                Left = 0,
            };
            //////this.Controls.Add(lblMove);
            lblMove.MouseMove += f_form_move_MouseDown;
            lblMove.MouseDoubleClick += (se, ev) => this.Close();
            this.FormClosing += (se, ev) => _browser.Dispose();
            this.Shown += (se, ev) =>
            {
                lblMove.BringToFront();

                this.Top = 0;
                this.Left = 0;
                this.Width = 1024;
                this.Height = Screen.PrimaryScreen.WorkingArea.Height;

                _browser.Load("http://192.168.56.102:30000/wcd/spa_login.html");
            };
        }
        #region [ FORM MOVE ]

        enum MOUSE_XY { OUT, INT };

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        private void f_form_move_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }


        #endregion
    }

    public class CefSharpSchemeHandlerFactory : ISchemeHandlerFactory
    {
        static string PATH_ROOT = Path.GetDirectoryName(Application.ExecutablePath);

        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            string url = request.Url;
            STORE.link_Add(url);

            if (request.Method == "POST") return null;
            if (url.Contains("/api/") || url.Contains(".json") || url.Contains(".xml")) return null;

            if (url.Contains(".html") || url.Contains(".css") || url.EndsWith(".js"))
            {
                var uri = new Uri(url);
                var fileName = uri.AbsolutePath;

                //Debug.WriteLine("----> " + url);
                //if (url.Contains(".html") || url.Contains(".txt") || url.Contains(".js") || url.Contains(".css"))

                string resource;
                string file = fileName.Replace('/', '\\');
                if (file[0] == '\\') file = file.Substring(1);
                file = Path.Combine(PATH_ROOT, file);

                if (File.Exists(file))
                {
                    resource = File.ReadAllText(file);
                    var fileExtension = Path.GetExtension(fileName);
                    return ResourceHandler.FromString(resource, fileExtension);
                }
            }

            return null;
        }
    }

    public class MyCustomMenuHandler : IContextMenuHandler
    {
        static string PATH_ROOT = Path.GetDirectoryName(Application.ExecutablePath);

        public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            // Remove any existent option using the Clear method of the model
            //
            model.Clear();

            //Console.WriteLine("Context menu opened !");

            // You can add a separator in case that there are more items on the list
            if (model.Count > 0)
            {
                model.AddSeparator();
            }

            model.AddItem((CefMenuCommand)26503, "Reload Page");
            model.AddSeparator();


            // Add a new item to the list using the AddItem method of the model
            model.AddItem((CefMenuCommand)26501, "Show DevTools");
            model.AddItem((CefMenuCommand)26502, "Close DevTools");

            model.AddSeparator();
            model.AddItem((CefMenuCommand)26504, "Show resources");


            //model.AddSeparator();
            //model.AddItem((CefMenuCommand)26510, "Write files HTML, JSON, JS, CSS, TXT net exist");

            //model.AddSeparator();
            //model.AddItem((CefMenuCommand)26505, "Write files JS not exist to WCD");

            //model.AddSeparator();
            //model.AddItem((CefMenuCommand)26506, "Write files CSS not exist to WCD");

            //model.AddSeparator();
            //model.AddItem((CefMenuCommand)26507, "Write files TXT not exist to WCD");

            //model.AddSeparator();
            //model.AddItem((CefMenuCommand)26508, "Write files JSON not exist to WCD");

            //model.AddSeparator();
            //model.AddItem((CefMenuCommand)26509, "Write files HTML not exist to WCD");
        }

        public bool OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {
            // React to the first ID (show dev tools method)
            if (commandId == (CefMenuCommand)26501)
            {
                browser.GetHost().ShowDevTools();
                return true;
            }

            // React to the second ID (show dev tools method)
            if (commandId == (CefMenuCommand)26502)
            {
                browser.GetHost().CloseDevTools();
                return true;
            }

            if (commandId == (CefMenuCommand)26503)
            {
                browser.Reload();
                return true;
            }

            //Show resources
            if (commandId == (CefMenuCommand)26504)
            {
                string s = STORE.link_getAll();
                File.WriteAllText("url.txt", s);
                Process.Start("url.txt");
                return true;
            }

            //Write JS resources
            if (commandId == (CefMenuCommand)26505)
            {
                string[] fs = STORE.link_find(x => x.EndsWith(".js"));
                f_downloadResource(fs, "*.JS");
                return true;
            }

            //Write CSS resources
            if (commandId == (CefMenuCommand)26506)
            {
                string[] fs = STORE.link_find(x => x.EndsWith(".css"));
                f_downloadResource(fs, "*.CSS");
                return true;
            }

            //Write TXT resources
            if (commandId == (CefMenuCommand)26507)
            {
                string[] fs = STORE.link_find(x => x.EndsWith(".txt"));
                f_downloadResource(fs, "*.TXT");
                return true;
            }

            //Write JSON resources
            if (commandId == (CefMenuCommand)26508)
            {
                string[] fs = STORE.link_find(x => x.EndsWith(".json"));
                f_downloadResource(fs, "*.JSON");
                return true;
            }

            //Write HTML resources
            if (commandId == (CefMenuCommand)26509)
            {
                string[] fs = STORE.link_find(x => x.EndsWith(".html"));
                f_downloadResource(fs, "*.HTML");
                return true;
            }

            //Write HTML resources
            if (commandId == (CefMenuCommand)26510)
            {
                string[] fs = STORE.link_find(x => x.EndsWith(".html"));
                f_downloadResource(fs, "*.HTML");

                fs = STORE.link_find(x => x.EndsWith(".json"));
                f_downloadResource(fs, "*.JSON");

                fs = STORE.link_find(x => x.EndsWith(".js"));
                f_downloadResource(fs, "*.JS");

                fs = STORE.link_find(x => x.EndsWith(".css"));
                f_downloadResource(fs, "*.CSS");

                fs = STORE.link_find(x => x.EndsWith(".txt"));
                f_downloadResource(fs, "*.TXT");

                return true;
            }

            // Return false should ignore the selected option of the user !
            return false;
        }

        void f_downloadResource(string[] fs, string ext = "")
        {
            List<string> ls = new List<string>();
            if (fs.Length > 0)
            {
                foreach (string url in fs)
                {
                    var uri = new Uri(url);
                    var fileName = uri.AbsolutePath;

                    string file = fileName.Replace('/', '\\');
                    if (file[0] == '\\') file = file.Substring(1);
                    file = Path.Combine(PATH_ROOT, file);

                    if (!File.Exists(file))
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        try
                        {
                            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                Stream receiveStream = response.GetResponseStream();
                                StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
                                string data = readStream.ReadToEnd();

                                File.WriteAllText(file, data);
                                STORE.cache_Write(fileName, data);

                                response.Close();
                                readStream.Close();

                                ls.Add(url);
                            }
                        }
                        catch { }
                    }
                }
            }
            MessageBox.Show("Download done [" + ext + "]: \r\n" + string.Join(Environment.NewLine, ls));
        }

        public void OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {

        }

        public bool RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            return false;
        }
    }
}
