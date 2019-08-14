using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Text.RegularExpressions;

namespace Cardsharp
{
    public partial class MainWindow : Window
    {
        private readonly Regex fileRegex = new Regex(@"^.*(\d{4})\.?.*$");
        private readonly Mutex sourceMutex = new Mutex();
        private readonly Mutex destinationMutex = new Mutex();
        private readonly Mutex runningMutex = new Mutex();
        private readonly Mutex shuttingDownMutex = new Mutex();
        private bool isShuttingDown = false;
        private bool IsShuttingDown
        {
            get
            {
                shuttingDownMutex.WaitOne();
                var rv = isShuttingDown;
                shuttingDownMutex.ReleaseMutex();
                return rv;
            }
            set
            {
                shuttingDownMutex.WaitOne();
                isShuttingDown = value;
                shuttingDownMutex.ReleaseMutex();
            }
        }
        private bool isRunning = false;
        private bool IsRunning
        {
            get
            {
                runningMutex.WaitOne();
                var rv = isRunning;
                runningMutex.ReleaseMutex();
                return rv;
            }
            set
            {
                runningMutex.WaitOne();
                isRunning = value;
                runningMutex.ReleaseMutex();
            }
        }
        private String source;
        private String Source
        {
            get
            {
                sourceMutex.WaitOne();
                var rv = Directory.Exists(source) ? source : String.Empty;
                sourceMutex.ReleaseMutex();
                return rv;
            }
            set
            {
                sourceMutex.WaitOne();
                source = Directory.Exists(value) ? value : String.Empty;
                sourceMutex.ReleaseMutex();
            }
        }
        private string destination;
        private String Destination
        {
            get
            {
                destinationMutex.WaitOne();
                var rv = Directory.Exists(destination) ? destination : String.Empty;
                destinationMutex.ReleaseMutex();
                return rv;
            }
            set
            {
                destinationMutex.WaitOne();
                destination = Directory.Exists(value) ? value : String.Empty;
                destinationMutex.ReleaseMutex();
            }
        }
        private bool GoodToGo
        {
            get
            {
                sourceMutex.WaitOne();
                destinationMutex.WaitOne();
                var rv = source != destination && source != String.Empty && destination != String.Empty;
                destinationMutex.ReleaseMutex();
                sourceMutex.ReleaseMutex();
                return rv;
            }
        }
        private Thread thread;
        
        public MainWindow()
        {
            InitializeComponent();

            var sdir = Properties.Settings.Default.sourceDir;
            var ddir = Properties.Settings.Default.destinationDir;
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            sourceDir.Text = (sdir != String.Empty && Directory.Exists(sdir)) ? sdir : desktop;
            destinationDir.Text = (ddir != String.Empty && Directory.Exists(sdir)) ? ddir : desktop;

            thread = new Thread(() =>
            {
                var runNext = DateTime.Now;

                while (!IsShuttingDown)
                {
                    if (IsRunning && DateTime.Now >= runNext)
                    {
                        runNext = DateTime.Now.AddSeconds(60.0);
                        var timestampNow = Int32.Parse(DateTime.Now.ToString("HHmm"));
 
                        GetMatchingFilenames(Source).ForEach(fn =>
                        {
                            var fileTimestamp = Int32.Parse(fileRegex.Match(fn).Groups[1].ToString());
                            if (fileTimestamp <= timestampNow)
                            {
                                var originalFullPath = Path.Combine(Source, fn);
                                var newFullPath = Path.Combine(Destination, fn);
                                File.Move(originalFullPath, newFullPath);
                            }
                        });

                        sourceBox.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            sourceBox.Items.Clear();
                            GetMatchingFilenames(Source).ForEach(fn => sourceBox.Items.Add(Path.GetFileName(fn)));
                        }));
                        destinationBox.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            destinationBox.Items.Clear();
                            GetMatchingFilenames(Destination).ForEach(fn => destinationBox.Items.Add(Path.GetFileName(fn)));
                        }));
                    }
                    Thread.Sleep(500);
                }
            });
            thread.Start();
        }

        private List<String> GetMatchingFilenames(String dirname)
        {
            var names = (from fn in Directory.GetFiles(dirname)
                         where fileRegex.IsMatch(Path.GetFileName(fn))
                         select Path.GetFileName(fn)).ToList().
                         Where(fn => Int32.Parse(fileRegex.Match(fn).Groups[1].ToString()) <= 2400).
                         ToList();

            names.Sort();
            return names;
        }

        private void UpdateBox(bool whichKind)
        {
            ListBox lb = whichKind ? sourceBox : destinationBox;
            var dirname = whichKind ? Source : Destination;

            if (dirname != String.Empty)
            {
                lb.Items.Clear();
                GetMatchingFilenames(dirname).ForEach(fn => lb.Items.Add(Path.GetFileName(fn)));
            }
        }

        private void DoDirUpdated(bool whichKind)
        {
            UpdateBox(whichKind);

            if (GoodToGo)
            {
                startButton.IsEnabled = true;
                statusBar.Content = "Click ‘Start’ to begin.";
            }
            else
            {
                startButton.IsEnabled = false;
                statusBar.Content = "Set your directories.";
            }
        }

        private void SourceDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            Source = sourceDir.Text;
            DoDirUpdated(true);
            if (sourceDir.Text != String.Empty && Directory.Exists(sourceDir.Text))
            {
                Properties.Settings.Default.sourceDir = sourceDir.Text;
                Properties.Settings.Default.Save();
            }
        }

        private void DestinationDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            Destination = destinationDir.Text;
            DoDirUpdated(false);
            if (destinationDir.Text != String.Empty && Directory.Exists(destinationDir.Text))
            {
                Properties.Settings.Default.destinationDir = destinationDir.Text;
                Properties.Settings.Default.Save();
            }
        }

        private String GetFolder(String title)
        {
            var dlg = new CommonOpenFileDialog
            {
                Title = "Choose " + title + " directory",
                IsFolderPicker = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            return dlg.ShowDialog() == CommonFileDialogResult.Ok ? dlg.FileName : String.Empty;
        }

        private void SourceButton_Click(object sender, RoutedEventArgs e)
        {
            var newdir = GetFolder("source");
            if (newdir != String.Empty)
            {
                sourceDir.Text = newdir;
            }
            startButton.IsEnabled = GoodToGo;
            statusBar.Content = (GoodToGo) ? "Click ‘Start’ to begin." : "Set your directories.";
        }

        private void DestinationButton_Click(object sender, RoutedEventArgs e)
        {
            var newdir = GetFolder("destination");
            if (newdir != String.Empty)
            {
                destinationDir.Text = newdir;
            }
            startButton.IsEnabled = GoodToGo;
            statusBar.Content = (GoodToGo) ? "Click ‘Start’ to begin." : "Set your directories.";
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            sourceDir.IsEnabled = true;
            sourceButton.IsEnabled = true;
            destinationDir.IsEnabled = true;
            destinationButton.IsEnabled = true;
            stopButton.IsEnabled = false;
            startButton.IsEnabled = true;
            statusBar.Content = (GoodToGo) ? "Click ‘Start’ to begin." : "Set your directories.";
            IsRunning = false;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            sourceDir.IsEnabled = false;
            sourceButton.IsEnabled = false;
            destinationDir.IsEnabled = false;
            destinationButton.IsEnabled = false;
            stopButton.IsEnabled = true;
            startButton.IsEnabled = false;
            statusBar.Content = "Running. Click ‘Stop’ to stop.";
            IsRunning = true;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            startButton.IsEnabled = false;
            stopButton.IsEnabled = false;
            sourceDir.IsEnabled = false;
            destinationDir.IsEnabled = false;
            sourceButton.IsEnabled = false;
            destinationButton.IsEnabled = false;
            sourceBox.IsEnabled = false;
            destinationBox.IsEnabled = false;
            statusBar.Content = "Waiting for threads to terminate…";
            IsShuttingDown = true;
            IsRunning = false;
            thread.Join();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            new About().Show();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/rjhansen/MiscScripts/issues");
        }
    }
}
