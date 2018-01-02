using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Configuration;
using System.Collections.Generic;
using Newtonsoft.Json;
using discordrpc;

namespace discordplay {

    public class Button
    {
        public string Name { get; set; }
        public MenuItem MenuItem { get; set; }
    }

    public class Program {
        private static string DISCORD_CLIENT_ID;

        private static bool ready = false;

        private static NotifyIcon notifyIcon;
        private static DiscordRpc.RichPresence oldPresence;
        private static DiscordRpc.RichPresence presence;
        private static Timer timer = new Timer ();
        private static Timer timer_connecting = new Timer ();
        private static Dictionary<string, MenuItem> Buttons = new Dictionary<string, MenuItem> ();

        [STAThread]
        public static void Main () {
            InitializeTray ();

            DISCORD_CLIENT_ID = ConfigurationManager.AppSettings["discord_client_id"];

            Application.Run ();
        }

        static void InitializeTray ()
        {

            MenuItem Connecting = new MenuItem ("Connecting...")
            {
                Enabled = false
            };

            Button[] arrButtons = {
                new Button { Name = "init", MenuItem = new MenuItem ("Connect", DiscordConnect) },
                new Button { Name = "forc", MenuItem = new MenuItem ("Force Update", UpdatePresence) },
                new Button { Name = "quit", MenuItem = new MenuItem ("Quit", DiscordQuit) },
                new Button { Name = "stop", MenuItem = new MenuItem ("Pause", DiscordPause) },
                new Button { Name = "conn", MenuItem = Connecting },
            };
            
            foreach (Button button in arrButtons)
            {
                Buttons.Add (button.Name, button.MenuItem);
            }

            notifyIcon = new NotifyIcon
            {
                Text = "Discord Play",
                Icon = new Icon ("icon.ico"),
                ContextMenu = new ContextMenu (),
                Visible = true
            };


            Menu.MenuItemCollection menuIndex = notifyIcon.ContextMenu.MenuItems;
            menuIndex.Add (Buttons["init"]);
            menuIndex.Add (Buttons["quit"]);
        }

        static void DiscordQuit (object sender, EventArgs e)
        {
            TerminateProgram ();
        }

        static void TerminateProgram ()
        {
            notifyIcon.Visible = false;
            DiscordRpc.Shutdown ();
            Application.Exit ();
        }


        public static void DiscordPause (object sender, EventArgs e)
        {
            if (ready)
            {
                Menu.MenuItemCollection menuIndex = notifyIcon.ContextMenu.MenuItems;
                menuIndex.Clear ();
                menuIndex.Add (Buttons["init"]);
                menuIndex.Add (Buttons["quit"]);

                timer.Stop ();
            }
        }

        // Connect button handler
        public static void DiscordConnect (object sender, EventArgs e)
        {
            if (ready)
                return;

            if (DISCORD_CLIENT_ID == "")
            {
                MessageBox.Show ("Missing Discord Client ID!", "Discord Play",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                TerminateProgram ();
            }

            DiscordRpc.EventHandlers eventHander = new DiscordRpc.EventHandlers ();

            eventHander.readyCallback += Ready;
            eventHander.disconnectedCallback += Disconnected;
            eventHander.errorCallback += Error;

            Menu.MenuItemCollection menuIndex = notifyIcon.ContextMenu.MenuItems;
            menuIndex.Clear ();
            menuIndex.Add (Buttons["init"]);
            menuIndex.Add (Buttons["quit"]);


            timer_connecting.Tick += new EventHandler (CheckConnected);
            timer_connecting.Interval = 100;
            timer_connecting.Start ();

            DiscordRpc.Initialize (DISCORD_CLIENT_ID, ref eventHander, true, null);
        }

        private static void CheckConnected (object sender, EventArgs e)
        {
            if (ready)
                return;

            DiscordRpc.RunCallbacks ();
        }

        static void CreatePopup (string message, string title = "Google Play Music")
        {
            notifyIcon.BalloonTipText = message;
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.ShowBalloonTip (3);
        }

        static void Error (int errorCode, string message)
        {
            MessageBox.Show (message, "Discord Play",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            TerminateProgram ();
        }

        static void Disconnected (int errorCode, string message)
        {
            MessageBox.Show (message, "Discord Play",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        static void Ready ()
        {
            timer_connecting.Stop ();

            presence.largeImageKey = "google_banner";
            presence.largeImageText = "Google Play Music";

            ready = true;

            oldPresence = presence;

            Menu.MenuItemCollection menuIndex = notifyIcon.ContextMenu.MenuItems;

            menuIndex.Clear ();
            menuIndex.Add (Buttons["forc"]);
            menuIndex.Add (Buttons["stop"]);
            menuIndex.Add (Buttons["quit"]);

            timer.Tick += new EventHandler (UpdatePresence);
            timer.Interval = 2500;
            timer.Start ();

            timer.Enabled = true;

            CreatePopup ("Connected!");
        }

        public static void UpdatePresence (object sender, EventArgs e)
        {
            if (!ready)
                return;

            string filePath = Path.Combine (
                Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData),
                @"Google Play Music Desktop Player\json_store\playback.json"
            );


            string FileContents = "";

            try {

                FileContents = File.ReadAllText (filePath);
            }

            catch (FileNotFoundException)
            {
                MessageBox.Show ("Cannot find playback.json", "Discord Play",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                TerminateProgram ();
            }

            catch (IOException)
            {
                return;
            }


            GooglePlay result = JsonConvert.DeserializeObject<GooglePlay> (FileContents);
            presence.details = $"Song: {result.song["title"]}";
            presence.state = $"Artist: {result.song["artist"]}";
            // Album: {result.song["album"]}
            

            if (result.playing)
            {
                presence.smallImageKey = "status_play";
                presence.smallImageText = "Playing";
            }
            else
            {
                presence.smallImageKey = "status_pause";
                presence.smallImageText = "Paused";
            }

            /*var now = DateTime.UtcNow;
            presence.startTimestamp = GetCurrentUNIXTimestamp () - result.time ["current"];
            presence.endTimestamp   = presence.startTimestamp + result.time ["total"];*/


            if (presence.details != oldPresence.details || presence.state != oldPresence.state || presence.smallImageText != oldPresence.smallImageText)
            {
                oldPresence = presence;

                //CreatePopup ($"{result.song["title"]} by {result.song["artist"]}");

                DiscordRpc.UpdatePresence (ref presence);
            }

        }
    }
}
