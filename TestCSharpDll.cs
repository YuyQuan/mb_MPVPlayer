using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic;
using System.IO;
using System.IO.Pipes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;



namespace MusicBeePlugin
{
    public partial class Plugin
    {
        
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        private string extraArgsConf;
        private string extConf;

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "MPVPlayer";
            about.Description = "Send videos to MPV without losing current windows focus";
            about.Author = "MPVPlayer";
            about.TargetApplication = "";   //  the name of a Plugin Storage device or panel header for a dockable panel
            about.Type = PluginType.General;
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 0;
            about.Revision = 1;
            about.MinInterfaceVersion = mbApiInterface.InterfaceVersion; //40
            about.MinApiRevision = mbApiInterface.ApiRevision; //53
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 360;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            StreamReader sr = new StreamReader(dataPath + "\\MPVPlayer.conf");
            extraArgsConf = sr.ReadLine();
            extConf = sr.ReadLine();
            sr.Close();
            Program.extraArgs = extraArgsConf;

            return about;
        }
        private TextBox textBox;
        private TextBox textBox2;

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            StreamReader sr = new StreamReader(dataPath + "\\MPVPlayer.conf");
            extraArgsConf = sr.ReadLine();
            extConf = sr.ReadLine();
            sr.Close();
            if (panelHandle != IntPtr.Zero)
            {
                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                Label prompt = new Label();
                prompt.AutoSize = true;
                prompt.Location = new Point(0, 0);
                prompt.Text = "MPV options(i.e. --mute=yes -ss 0)";
                textBox = new TextBox();
                textBox.Text = extraArgsConf;
                textBox.TextChanged += textBox_extraArgs;
                Program.extraArgs = textBox.Text;

                textBox.Bounds = new Rectangle(0, 25, 450, textBox.Height);
                configPanel.Controls.AddRange(new Control[] { prompt, textBox });

                Label prompt2 = new Label();
                prompt2.AutoSize = true;
                prompt2.Location = new Point(0, 50);
                prompt2.Text = "Play files (i.e. .mp4 .mov)";
                textBox2 = new TextBox();
                textBox2.Text = extConf;
                textBox2.TextChanged += textBox_ext;

                textBox2.Bounds = new Rectangle(0, 75, 450, textBox2.Height);
                configPanel.Controls.AddRange(new Control[] { prompt2, textBox2 });
            }
            return false;
        }
        private void textBox_extraArgs(object sender, EventArgs e)
        {
            extraArgsConf = textBox.Text;
            // save the value
        }

        private void textBox_ext(object sender, EventArgs e)
        {
            extConf = textBox2.Text;
            // save the value
        }

        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            StreamWriter sw = new StreamWriter(dataPath + "\\MPVPlayer.conf");
            sw.WriteLine(extraArgsConf);
            sw.WriteLine(extConf);
            sw.Close();
            Program.extraArgs = extraArgsConf;
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            // This will send an invalid command to MPV which will casue it to close along with MusicBee
            Program.MainHack("");
            // Program.MainHack("--quit"); 
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            // activate video only if valid extention
            string[] ext = extConf.Split(' ');
            bool tmpFlag = false;
            foreach (string i in ext)
            {
                if (sourceFileUrl.EndsWith(i))
                {
                    tmpFlag = true;
                }
            }
            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.PlayStateChanged:
                    if (tmpFlag)
                    {
                        switch (mbApiInterface.Player_GetPlayState())
                        {
                            case PlayState.Playing:
                                int tmp = mbApiInterface.Player_GetPosition();
                                double sec = tmp / 1000.0;
                                Program.MpvLoadFile2(sec.ToString(), 0);

                        break;
                            case PlayState.Paused:
                                Program.MpvLoadFile2("", 1);
                                break;
                        }
                    }
                    else
                    {
                        // clear MPV but still keep open
                        Program.MpvLoadFile2("", 2);
                    }
                    break;
                case NotificationType.PluginStartup:
                    // perform startup initialisation
                    break;
                case NotificationType.TrackChanged:
                    switch (mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                                if (tmpFlag)
                                {
                                    Program.MainHack(sourceFileUrl);
                                var t = Task.Factory.StartNew(() =>
                                {
                                    return Task.Factory.StartNew(() =>
                                    {
                                        Task.Delay(3000).Wait();
                                        Program.MpvLoadFile2((mbApiInterface.Player_GetPosition() / 1000.0).ToString(), 0);
                                    });
                                });
                                t.Wait();

                                }
                                else
                                {
                                    Program.MpvLoadFile2("", 2);
                                }
                            break;
                        case PlayState.Paused:
                            break;
                    }
                    break;
            }
        }

        // return an array of lyric or artwork provider names this plugin supports
        // the providers will be iterated through one by one and passed to the RetrieveLyrics/ RetrieveArtwork function in order set by the user in the MusicBee Tags(2) preferences screen until a match is found
        //public string[] GetProviders()
        //{
        //    return null;
        //}

        // return lyrics for the requested artist/title from the requested provider
        // only required if PluginType = LyricsRetrieval
        // return null if no lyrics are found
        //public string RetrieveLyrics(string sourceFileUrl, string artist, string trackTitle, string album, bool synchronisedPreferred, string provider)
        //{
        //    return null;
        //}

        // return Base64 string representation of the artwork binary data from the requested provider
        // only required if PluginType = ArtworkRetrieval
        // return null if no artwork is found
        //public string RetrieveArtwork(string sourceFileUrl, string albumArtist, string album, string provider)
        //{
        //    //Return Convert.ToBase64String(artworkBinaryData)
        //    return null;
        //}

        //  presence of this function indicates to MusicBee that this plugin has a dockable panel. MusicBee will create the control and pass it as the panel parameter
        //  you can add your own controls to the panel if needed
        //  you can control the scrollable area of the panel using the mbApiInterface.MB_SetPanelScrollableArea function
        //  to set a MusicBee header for the panel, set about.TargetApplication in the Initialise function above to the panel header text
        /*
        public int OnDockablePanelCreated(Control panel)
        {
        //  //    return the height of the panel and perform any initialisation here
        //  //    MusicBee will call panel.Dispose() when the user removes this panel from the layout configuration
        //  //    < 0 indicates to MusicBee this control is resizable and should be sized to fill the panel it is docked to in MusicBee
        //  //    = 0 indicates to MusicBee this control resizeable
        //  //    > 0 indicates to MusicBee the fixed height for the control.Note it is recommended you scale the height for high DPI screens(create a graphics object and get the DpiY value)
            float dpiScaling = 0;
            using (Graphics g = panel.CreateGraphics())
            {
                dpiScaling = g.DpiY / 96f;
            }
            panel.Paint += panel_Paint;
            return Convert.ToInt32(100 * dpiScaling);
        }
        */

        // presence of this function indicates to MusicBee that the dockable panel created above will show menu items when the panel header is clicked
        // return the list of ToolStripMenuItems that will be displayed
        //public List<ToolStripItem> GetHeaderMenuItems()
        //{
        //    List<ToolStripItem> list = new List<ToolStripItem>();
        //    list.Add(new ToolStripMenuItem("A menu item"));
        //    return list;
        //}
/*
        private void panel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Red);
            TextRenderer.DrawText(e.Graphics, "hello", SystemFonts.CaptionFont, new Point(10, 10), Color.Blue);
        }
*/



    }
}

    static class Program
    {
        static public string extraArgs = "";
        [STAThread]
        //public static void MainHack(String[] args)
        public static void MainHack(string args)
        {

            // passed multiple arguments. no need to deal with launcher ipc
            /*
            if (args.Length > 1)
            {
                for (int i = 1; i < args.Length; i++)
                {
                    extraArgs += args[i] + " ";
                }
                var pipe = MpvLaunch();

                //rest of the files get appended
                MpvLoadFile(args[0], false, pipe);

            }
            else if (args.Length == 1)
            {
                doIpc(args[0]);
            }
            else if (args.Length == 0)
            {
                MpvLaunch();
            }
            */
            var pipe = MpvLaunch();
            MpvLoadFile(args, false, pipe);
            //doIpc(args);
        }

        static private string pipePrefix = @"\\.\pipe\";
        static private string mpvPipe = "umpvw-mpv-pipe";
        static private string umpvwPipe = "umpvw-pipe";

        static private NamedPipeServerStream serverPipe;
        static private bool timeout = false;
        static private int timer = 300;

        static void serverTimeout()
        {
            Thread.Sleep(timer);
            timeout = true;
            var pipe = new NamedPipeClientStream(umpvwPipe);
            try
            {
                pipe.Connect();
            }
            catch (Exception)
            {
                Application.Exit();
            }
            pipe.Dispose();

        }

        static void doIpc(string arg)
        {
            bool createdNew;
            var m_Mutex = new Mutex(true, "umpvwMutex", out createdNew);

            if (createdNew) // server role
            {
                var pipe = MpvLaunch(); //start mpv first

                serverPipe = new NamedPipeServerStream(umpvwPipe);
                var pipeReader = new StreamReader(serverPipe);
                var thread = new Thread(new ThreadStart(serverTimeout));
                thread.Start();

                var list = new List<string>();
                list.Add(arg);

                while (timeout == false)
                {
                    serverPipe.WaitForConnection();
                    var s = pipeReader.ReadLine();
                    if (!String.IsNullOrEmpty(s))
                    {
                        list.Add(s);
                    }
                    serverPipe.Disconnect();
                }
                //new Thread(() => System.Windows.Forms.MessageBox.Show(String.Join(", ", list))).Start();
                list.Sort();
                MpvLoadFile(list.First(), false, pipe);
                for (int i = 1; i < list.Count; i++)
                {
                    MpvLoadFile(list.ElementAt(i), true, pipe);
                }

            }
            else
            {  // client role
                var clientPipe = new NamedPipeClientStream(umpvwPipe);
                try
                {
                    clientPipe.Connect(timer);
                }
                catch (Exception)
                {
                    return;
                }
                var pipeWriter = new StreamWriter(clientPipe);
                pipeWriter.Write(arg);
                pipeWriter.Flush();
            }
        }

        //static string mpvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mpv.exe");
        static string mpvPath = "mpv.exe";

        // launch mpv or get pipe 
        static NamedPipeClientStream MpvLaunch()
        {
            //if mpv is not running, start it
            if (!File.Exists(pipePrefix + mpvPipe))
            {
                //ensure we are launching the mpv executable from the current folder. also launch the .exe specifically as we don't need the command line.

                Process.Start(mpvPath, extraArgs + " " + @"--input-ipc-server=" + pipePrefix + mpvPipe);
            }
            var pipe = new NamedPipeClientStream(mpvPipe);
            pipe.Connect();
            return pipe;
        }

        // load file into mpv
        static void MpvLoadFile(string file, bool append, NamedPipeClientStream pipe)
        {
            var command = append ? "append" : "replace";
            WriteString("{ \"command\": [\"set_property\", \"pause\", false] }", pipe);
            WriteString("loadfile \"" + file.Replace("\\", "\\\\") + "\" " + command, pipe);
        }

        public static void MpvLoadFile2(string args, int seek)
        {
            var pipe = MpvLaunch();
            switch (seek)
            {
                case 0:
                    WriteString("{ \"command\": [\"set_property\", \"pause\", false] }", pipe);
                    WriteString("seek " + args + " absolute", pipe);
                    break;
                case 1:
                    WriteString("{ \"command\": [\"set_property\", \"pause\", true] }", pipe);
                    break;
                case 2:
                    // load a 1x1 transparent pixel (PNG)
                    WriteString("loadfile \"hex://89504E470D0A1A0A0000000D4948445200000001000000010804000000B51C0C020000000B4944415478DA6364600000000600023081D02F0000000049454E44AE426082\" replace", pipe);
                    // try to resize to be 1x1 player size?
                    //WriteString("{ \"command\": [\"set_property\", \"autofit\", \"1x1\"] }", pipe);
                    // pause so that mpv's "ontop" becomes ineffective
                    WriteString("{ \"command\": [\"set_property\", \"pause\", true] }", pipe);
                    break;
            }
        }

        // write to mpv stream in utf-8
        static public void WriteString(string outString, Stream ioStream)
        {
            byte[] outBuffer = Encoding.UTF8.GetBytes(outString + "\n");
            int len = outBuffer.Length;
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();
        }
    }

