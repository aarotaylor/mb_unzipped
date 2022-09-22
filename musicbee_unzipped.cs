using System;
using System.Configuration;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Lastfm.Services;
using Lastfm.Scrobbling;
using Lastfm;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Net;


/*

STATE:
    path can be entered in the plugin panel, and is saved to the persistentStoragePath
    path is persistent in the plugin panel
    checkbox for delete zip after unzip
    second textbox for destination path to move archive after unzip
    deletion is enabled when the checkbox is checked

TODO:
    add error handling for path not found exception
    add description for the delete checkbox and destination textbox

REFACTOR:
    use OpenFileDialog or similar for path selection
 
*/

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        public TextBox pathBox = new TextBox();
        public CheckBox deleteZip = new CheckBox(); // for deletion
        public TextBox destination = new TextBox();
        public string zipPath = "";

        // CALL NEWLY MADE FUNCTIONS HERE SO THEY WILL EXECUTE
        public PluginInfo Initialise(IntPtr apiInterfacePtr) 
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "MusicBee Unzipped";
            about.Description = "Allows MusicBee to see and Unzip .zip files in Monitored Folders. Under 'Tools'";
            about.Author = "Arkada";
            about.TargetApplication = "";   //  the name of a Plugin Storage device or panel header for a dockable panel
            about.Type = PluginType.General;
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 0;
            about.Revision = 0;
            about.MinInterfaceVersion = 30;
            about.MinApiRevision = 40;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 100;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

            createMenuItem();
            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            //string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            
            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle != IntPtr.Zero)
            {
                string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();

                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                Label prompt = new Label();
                prompt.AutoSize = true;
                prompt.Location = new Point(0, 0);
                prompt.Text = "File Path for Archives";

                pathBox = new TextBox();
                pathBox.Bounds = new Rectangle(60, 0, 200, pathBox.Height);
                pathBox.Location = new Point(0, 20);

                deleteZip = new CheckBox();
                deleteZip.Location = new Point(0, 40);
                // change back color

                Label delete = new Label();
                delete.AutoSize = true;
                delete.Location = new Point(10, 40);
                delete.Text = "Delete after unzipping";

                Label dest = new Label();
                dest.AutoSize = true;
                dest.Location = new Point(0, 50);
                dest.Text = "Destination for the archive";

                destination = new TextBox();
                destination.Bounds = new Rectangle(60, 0, 200, pathBox.Height);
                destination.Location = new Point(0, 70);

                configPanel.Controls.AddRange(new Control[] { prompt, pathBox, destination, deleteZip });

                if (File.Exists(dataPath + "/Unzipped.info"))
                {
                    string readText = File.ReadAllText(dataPath + "/Unzipped.info");
                    sendText("THESE ARE THE CONTENTS: " + readText + "|");
                    if (readText != "\r\n")
                    {
                        string[] readLines = File.ReadAllLines(dataPath + "/Unzipped.info");
                        pathBox.Text = readLines[0];
                        deleteZip.Checked = bool.Parse(readLines[1]);
                        destination.Text = readLines[2];
                        sendText(pathBox.Text);
                    }
                    
                }
            }
            return true;
        }
       
        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();

            // On save, %if both filepaths are valid% take the value in pathBox, deleteZip, and destination and save them to Unzipped.info
            using (StreamWriter writer = new StreamWriter(dataPath+"/Unzipped.info"))
            {
                string contents = pathBox.Text + "\n" + deleteZip.Checked + "\n" + destination.Text+"\n";
                writer.WriteLine(contents);
            }
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
            // Remove Unzipped.info
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.PluginStartup:
                    // perform startup initialisation
                    switch (mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                            Console.WriteLine("//////////////////////////////////////////\n//////////////////////////////");
                            MessageBox.Show("Error Message", "Error Title", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            break;
                        case PlayState.Paused:
                            // ...
                            break;
                    }
                    string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();

                    // Read the unzipped settings file to get the path for the monitored archive folder
                    if (File.Exists(dataPath+ "/Unzipped.info"))
                    {
                        string readText = File.ReadAllText(dataPath + "/Unzipped.info");
                        pathBox.Text = readText;
                        //sendText(readText);
                    }
                    else
                    {
                        using (StreamWriter writer = new StreamWriter(dataPath + "/Unzipped.info"))
                        {
                            writer.WriteLine("");
                        }
                    }
                    

                    break;
                case NotificationType.TrackChanged:
                    string artist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist);
                    // ...
                    break;
            }
        }


        // TODO: find a way to make text centered on windows forms
        // - Make it look nice
        private void createMenuItem() {
            mbApiInterface.MB_AddMenuItem("mnuTools/Unzip Inbox", "Unzip all zip archives in the inbox", unzip);
        }

        // Wrapper for sending text to the bottom of the screen
        private void sendText(string message)
        {
            mbApiInterface.MB_SetBackgroundTaskMessage(message);
        }

        private int countFiles(string file)
        {
            using (ZipArchive archive = ZipFile.Open(file, ZipArchiveMode.Read))
            {
                int count = 0;

                // We count only named (i.e. that are with files) entries
                foreach (var entry in archive.Entries)
                    if (!String.IsNullOrEmpty(entry.Name))
                        count += 1;

                return count;
            }
        }

        public string readData(int line) 
        {
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            if (File.Exists(dataPath + "/Unzipped.info"))
            {
                string readText = File.ReadAllText(dataPath + "/Unzipped.info");
                
                if (readText != "\r\n")
                {
                    string[] readLines = File.ReadAllLines(dataPath + "/Unzipped.info");
                    sendText(readLines[line]);
                    return readLines[line];
                }
            }
            return "";
        }

        // Unzip files, and either delete or move the original file
        private void unzip(object sender, EventArgs args) {
            string inbox = readData(0); // "F:\\_Inbox_";
            bool checkbox = bool.Parse(readData(1));
            string dest = readData(2);

            DirectoryInfo d = new DirectoryInfo(inbox);
            FileInfo[] Files = d.GetFiles();
            int zipCount = 0;
            int fileCount = 0;
            ///*
            foreach (FileInfo file in Files)
            {
                if (file.Name.Contains(".zip"))
                {
                    //sendText(file.FullName);
                    zipCount++;
                    ZipFile.ExtractToDirectory(file.FullName, inbox);
                    fileCount += countFiles(file.FullName);

                    // After each archive has been unzipped, move it to the destination or delete it
                    if (checkbox) // delete
                    {
                        File.Delete(file.FullName);
                        sendText(file.Name + " has been deleted");
                    }
                    // move
                    else
                    {
                        sendText(dest + "\\" + file.FullName);
                        File.Move(file.FullName, dest + "\\" + file.Name);
                    }

                }
               
            }
            
            //sendText(zipCount + " .zip files were unzipped resulting in " + fileCount + " new files in the inbox.");
            //*/
            


            /*/ TODO
             * Get the location of the library
             * - This can be done by using PersistentStoragePath to get to LibrarySettings.ini and saving the paths for <OrganizationMonitoredFolders>
             * - This file is located at Appdata\Library\MusicBeeLibrarySettings.ini
             * 
             * Once file paths are obtained, use directory.getfiles on each of the monitored folders to fill a list for tracks
             * 
             * 
             * 
             * 
             * 
             * 
             */
            //sendText(storage);

        }



        // Makes TimeSpan more readable. Sourced from this comment: https://stackoverflow.com/a/41966914
        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            Func<Tuple<int, string>, string> tupleFormatter = t => $"{t.Item1} {t.Item2}{(t.Item1 == 1 ? string.Empty : "s")}";
            var components = new List<Tuple<int, string>>
        {
            Tuple.Create((int) timeSpan.TotalDays, "day"),
            Tuple.Create(timeSpan.Hours, "hour"),
            Tuple.Create(timeSpan.Minutes, "minute"),
            Tuple.Create(timeSpan.Seconds, "second"),
        };

            components.RemoveAll(i => i.Item1 == 0);

            string extra = "";

            if (components.Count > 1)
            {
                var finalComponent = components[components.Count - 1];
                components.RemoveAt(components.Count - 1);
                extra = $" and {tupleFormatter(finalComponent)}";
            }

            return $"{string.Join(", ", components.Select(tupleFormatter))}{extra}";
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
        //public int OnDockablePanelCreated(Control panel)
        //{
        //  //    return the height of the panel and perform any initialisation here
        //  //    MusicBee will call panel.Dispose() when the user removes this panel from the layout configuration
        //  //    < 0 indicates to MusicBee this control is resizable and should be sized to fill the panel it is docked to in MusicBee
        //  //    = 0 indicates to MusicBee this control resizeable
        //  //    > 0 indicates to MusicBee the fixed height for the control.Note it is recommended you scale the height for high DPI screens(create a graphics object and get the DpiY value)
        //    float dpiScaling = 0;
        //    using (Graphics g = panel.CreateGraphics())
        //    {
        //        dpiScaling = g.DpiY / 96f;
        //    }
        //    panel.Paint += panel_Paint;
        //    return Convert.ToInt32(100 * dpiScaling);
        //}

        // presence of this function indicates to MusicBee that the dockable panel created above will show menu items when the panel header is clicked
        // return the list of ToolStripMenuItems that will be displayed
        //public List<ToolStripItem> GetHeaderMenuItems()
        //{
        //    List<ToolStripItem> list = new List<ToolStripItem>();
        //    list.Add(new ToolStripMenuItem("A menu item"));
        //    return list;
        //}

        //private void panel_Paint(object sender, PaintEventArgs e)
        //{
        //    e.Graphics.Clear(Color.Red);
        //    TextRenderer.DrawText(e.Graphics, "hello", SystemFonts.CaptionFont, new Point(10, 10), Color.Blue);
        //}

    }
}