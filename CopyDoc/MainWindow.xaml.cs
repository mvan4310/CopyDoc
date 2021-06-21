using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace CopyDoc
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool IsCopying { get; set; }
        public long LastProcessed { get; set; }
        public long TotalProcessed { get; set; }
        public long TotalAmount { get; set; }
        public string SourceDir { get; set; }
        public string TargetDir { get; set; }
        private BackgroundWorker bw;
        private Queue<long> Snapshots = new(30);
        private bool OverwriteExisting = false;

        private System.Timers.Timer _elapsedTimer;

        public MainWindow()
        {
            InitializeComponent();
            _elapsedTimer = new(1000D);
            _elapsedTimer.AutoReset = true;
            _elapsedTimer.Elapsed += (sender, e) =>
            {
                // Remember only the last 30 snapshots; discard older snapshots
                if (Snapshots.Count == 30)
                {
                    Snapshots.Dequeue();
                }
                var _processed = LastProcessed;
                Snapshots.Enqueue(Interlocked.Exchange(ref _processed, 0L));
                LastProcessed = _processed;

                var averageSpeed = Snapshots.Average();
                var bytesLeft = TotalProcessed - TotalAmount;
                Console.WriteLine("Average speed: {0:#} MBytes / second", averageSpeed / (1024 * 1024));
                if (averageSpeed > 0)
                {
                    var timeLeft = TimeSpan.FromSeconds(bytesLeft / averageSpeed);
                    var timeLeftRounded = TimeSpan.FromSeconds(Math.Round(timeLeft.TotalSeconds));
                    this.Dispatcher.Invoke(() =>
                    {
                        txtStatus.Text = "Time left: " + timeLeftRounded;
                    });
                    
                }
                else
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        txtStatus.Text = "Time left: Infinite";
                    });
                    
                }
            };
        }

        public void StartCopy()
        {
            if (IsCopying)
            {
                bw.CancelAsync();
                pbProgress.IsIndeterminate = true;
            }
            else
            {
                _elapsedTimer.Start();
                using (bw = new BackgroundWorker())
                {
                    bw.WorkerSupportsCancellation = true;
                    bw.DoWork += OnCopy;
                    bw.RunWorkerCompleted += OnComplete;
                    bw.RunWorkerAsync();
                }
            }
            
        }

        private void OnComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            //Stop the clock on the estimator
            _elapsedTimer.Stop();

            if (e.Cancelled)
            {
                this.Dispatcher.Invoke(() =>
                {
                    txtStatus.Text = "Copy Cancelled";
                });
                
            }
            else if (e.Error is not null)
            {
                if (e.Error.InnerException is not null)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        txtStatus.Text = e.Error.InnerException.Message;
                    });
                }
                else
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        txtStatus.Text = e.Error.Message;
                    });
                }
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    txtStatus.Text = "Copy Completed";
                });
                
            }
        }

        private void OnCopy(object sender, DoWorkEventArgs e)
        {
            try
            {
                //Grab the files from the source directory
                DirectoryInfo sourceInfo = new(SourceDir);
                FileInfo[] sourceFiles = sourceInfo.GetFiles();

                for (int i = 0; i < sourceFiles.Length; i++)
                {
                    FileInfo info = sourceFiles[i];
                    TotalAmount += info.Length;
                }

                //Grab the files for the target (we might not need this later, but grabbing this now JIC)
                DirectoryInfo targetInfo = new(TargetDir);
                FileInfo[] targetFiles = sourceInfo.GetFiles();

                //Lets move the files over now
                for (int i = 0; i < sourceFiles.Length; i++)
                {
                    //Cancel if we have received the instruction to do so
                    if (bw.CancellationPending)
                    {
                        return;
                    }

                    FileInfo info = sourceFiles[i];
                    
                    //Point at the new directory with the current file name
                    string newFullName = targetInfo.FullName + @"\" + info.Name;

                    //Copy
                    info.CopyTo(newFullName, OverwriteExisting);

                    //Update the total amount of data moved so far
                    TotalProcessed += info.Length;

                    //Update the ProgressBar, invoking to avoid exceptions from modifying UI components from another thread
                    this.Dispatcher.Invoke(() =>
                    {
                        pbProgress.Value = Math.Round((double)TotalProcessed / (double)TotalAmount) * 100;
                    });
                }
            }
            catch (Exception)
            {
                //We wont do anything here, for now, but we should do something at some point...
            }
        }

        public void OpenSource()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.ShowNewFolderButton = true;
                dialog.UseDescriptionForTitle = true;
                dialog.Description = "Select a Source Path";
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                SourceDir = dialog.SelectedPath;
                txtSource.Text = SourceDir;
            }
        }

        public void OpenTarget()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.ShowNewFolderButton = true;
                dialog.UseDescriptionForTitle = true;
                dialog.Description = "Select a Target Path";
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                TargetDir = dialog.SelectedPath;
                txtTarget.Text = TargetDir;
            }
        }

        private void btnTargetSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenTarget();
        }

        private void btnSourceSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenSource();
        }

        private void btnAction_Click(object sender, RoutedEventArgs e)
        {
            StartCopy();
        }

        private void cbOverwrite_Checked(object sender, RoutedEventArgs e)
        {
            OverwriteExisting = !OverwriteExisting;
        }
    }
}
