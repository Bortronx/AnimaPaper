﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using FirstFloor.ModernUI.Windows.Controls;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;
using Microsoft.Win32;

namespace VideoDesk
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {


        public static String file;
        public static Uri fileMedia;
        public static Window win2;
        public static MediaElement media = null;
        public static string path;
      //  public static List<MediaElement> mediaList;
      //  public static List<Grid> gridList;

        public static List<Window> windowList;
        public static List<System.Drawing.Rectangle> ScreenList;
        public static List<System.Windows.Controls.Button> ButtonList;


        public static IntPtr workerw;
        public static IntPtr workerwHidden;


        public static MediaPlayer player; //for thumbnails

        public static bool soundOrNot;
        public static bool currentlyPlaying;
        public static bool setStartupAutoPlay;

        System.Windows.Forms.NotifyIcon ni;
        public MainWindow()
        {
            InitializeComponent();

            media = null;
            soundOrNot = false;
            currentlyPlaying = false;
            setStartupAutoPlay = false;
            player = new MediaPlayer { Volume = 0, ScrubbingEnabled = true }; //Used for thumbnails, not yet implemented

            // Tray icon and balloon
            ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("./AnimePaper.ico");

            ni.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };


            /*
             * 
             * Check monitor numbers and put them in differents Arraylist
             * ScreenList is used to get the actual resolution of a screen
             * windowList will be used to create 1 window per monitor *Not yet implemented*
             * ButtonList will be used to create dynamically button in order to select and load a file for each monitor *Not yet implemented*
             */
            // mediaList = new List<MediaElement>();
            windowList = new List<Window>();
           // gridList = new List<Grid>();
            ScreenList = new List<System.Drawing.Rectangle>();
            ButtonList = new List<System.Windows.Controls.Button>();
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                ScreenList.Add(Screen.AllScreens[i].WorkingArea);
            }
            for (int i = 0; i < MainWindow.ScreenList.Count; i++)
            {
                windowList.Add(new Window());
               // mediaList.Add(new MediaElement());
            }

            for (int i = 0; i < MainWindow.ScreenList.Count; i++)
            {
                ButtonList.Add(new System.Windows.Controls.Button());
                ButtonList[i].Name = "Screen" + i.ToString();
                ButtonList[i].Content = "Screen " + i.ToString();
                
            }
            //Config file read / setup
            configFile();
        }

        //check the config file if there is a path to launch
        private void configFile()
        {
            path =  "config.ini";
            if (System.IO.File.Exists(path))
            {
                try
                {
                    if (new System.IO.FileInfo(path).Length > 0)
                    {
                        using (var reader = new System.IO.StreamReader(path))
                        {
                            file = String.Empty;
                            file = reader.ReadLine();
                            if (System.IO.File.Exists(file))
                            {
                                setStartupAutoPlay = true;
                            }
                            else
                                file = String.Empty;
                        }
                    }
                    
                } catch (Exception e)
                {
                    throw new ApplicationException("The config file does not point to an available file or file was deleted / moved", e);
                }
            }
            else
            {
                System.IO.FileStream fs = new System.IO.FileStream(path, System.IO.FileMode.CreateNew);
                fs.WriteByte(0);
                fs.Close();
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                ni.BalloonTipTitle = "Anima Paper";
                ni.BalloonTipText = "Anima Paper is minimized";
                ni.Visible = true;
                ni.ShowBalloonTip(250);
                this.Hide();
            }
            else if (WindowState.Normal == this.WindowState)
            {
                ni.Visible = false;
            }

            base.OnStateChanged(e);
        }

        public void closeAll()
        {
            if (MainWindow.media != null)
            {
                MainWindow.media.Stop();
                MainWindow.media = null;
                for (int i = 0; i < MainWindow.windowList.Count; i++)
                {
                    MainWindow.windowList[i].Close();
                }

            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {

            ni.Visible = false;

            if (MainWindow.media != null)
            {
                MainWindow.media.Stop();
                MainWindow.media = null;
                for (int i = 0; i < MainWindow.windowList.Count; i++)
                {
                    //MainWindow.mediaList[i].Stop();
                    //MainWindow.mediaList[i] = null;
                    MainWindow.windowList[i].Close();
                }

            }
            closeAll();
            base.OnClosing(e);
        }

        /// <summary>
        /// This is used to create and find the actual worker window.
        /// Program manager is the root of all windows
        /// Icons are drawn on a window
        /// Must attach a new worker to Progman in order to draw behind icons. 
        /// </summary>
        public static void findWorker()
        {
            IntPtr progman = W32.FindWindow("Progman", null);

            IntPtr result = IntPtr.Zero;

            W32.SendMessageTimeout(progman,
                                   0x052C,//user code
                                   new IntPtr(0),
                                   IntPtr.Zero,
                                   W32.SendMessageTimeoutFlags.SMTO_NORMAL,
                                   1000,
                                   out result);

            workerw = IntPtr.Zero;

            W32.EnumWindows(new W32.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                IntPtr p = W32.FindWindowEx(tophandle,
                                            IntPtr.Zero,
                                            "SHELLDLL_DefView",
                                            IntPtr.Zero);

                if (p != IntPtr.Zero)
                {
                    workerw = W32.FindWindowEx(IntPtr.Zero,
                                               tophandle,
                                               "WorkerW",
                                               IntPtr.Zero);
                    
                }

                
                return true;
            }), IntPtr.Zero);

            //
            /*
            W32.EnumWindows(new W32.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                IntPtr p = W32.FindWindowEx(tophandle,
                                            IntPtr.Zero,
                                            "Progman",
                                            IntPtr.Zero);
                    
                if (p != IntPtr.Zero)
                {
                    // Gets the WorkerW Window after the current one.
                    workerwHidden = W32.FindWindowEx(IntPtr.Zero,
                                               tophandle,
                                               "WorkerW",
                                               IntPtr.Zero);

                }

                return true;
            }), IntPtr.Zero);

    */
        }

    }
}
