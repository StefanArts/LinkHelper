using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LinkHelper
{

    public partial class MainForm : Form
    {
        [DllImport("User32.dll")]
        protected static extern int
            SetClipboardViewer(int hWndNewViewer);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool
               ChangeClipboardChain(IntPtr hWndRemove,
                                    IntPtr hWndNewNext);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg,
                                             IntPtr wParam,
                                             IntPtr lParam);

        public static String version = "1.0-snapshot";
        public static String clipboard;
        private bool ready = false;

        MySettings settings = MySettings.Load();

        IntPtr nextClipboardViewer;

        public MainForm()
        {
            InitializeComponent();
            nextClipboardViewer = (IntPtr)SetClipboardViewer((int)
                         this.Handle);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            versionLabel.Text = versionLabel.Text.Replace("%s", version);
            shortSize.Value = settings.maxlength;
            ready = true;
        }

        private void onClose(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            trayIcon.Visible = true;
            trayIcon.ShowBalloonTip(1000);
            e.Cancel = true;
        }

        private void onClick(object sender, EventArgs e)
        {
            this.Show();
            trayIcon.Visible = false;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://stefanarts.net");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        void checkClipboard()
        {
            if(ready)
            {
                try
                {
                    IDataObject iData = new DataObject();
                    iData = Clipboard.GetDataObject();

                    if (iData.GetDataPresent(DataFormats.Text))
                    {
                        clipboard = (string)iData.GetData(DataFormats.Text);
                        if (Uri.IsWellFormedUriString(clipboard, UriKind.Absolute))
                        {
                            if (clipboard.Length > settings.maxlength)
                            {
                                if (isOnline())
                                {
                                    System.Windows.Forms.Clipboard.SetText(shortUrlWithSLS(clipboard));
                                }
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }
        }

        private String shortUrlWithSLS(String url)
        {
            using (WebClient client = new WebClient())
            {
                string raw = client.DownloadString("https://s-ls.de/?url=" + url + "&linkhelper");
                var link = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(raw);
                return "https://s-ls.de/" + link.link.shortname;
            }
        }

        protected override void
          WndProc(ref System.Windows.Forms.Message m)
        {
            // defined in winuser.h
            const int WM_DRAWCLIPBOARD = 0x308;
            const int WM_CHANGECBCHAIN = 0x030D;

            switch (m.Msg)
            {
                case WM_DRAWCLIPBOARD:
                    checkClipboard();
                    SendMessage(nextClipboardViewer, m.Msg, m.WParam,
                                m.LParam);
                    break;

                case WM_CHANGECBCHAIN:
                    if (m.WParam == nextClipboardViewer)
                        nextClipboardViewer = m.LParam;
                    else
                        SendMessage(nextClipboardViewer, m.Msg, m.WParam,
                                    m.LParam);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        public static bool isOnline()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://google.com/generate_204"))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        private void shortSize_ValueChanged(object sender, EventArgs e)
        {
            settings.maxlength = (int)shortSize.Value;
            settings.Save();
        }
    }

    class MySettings : AppSettings<MySettings>
    {
        public int maxlength = 25;
    }
}
