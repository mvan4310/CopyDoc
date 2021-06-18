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
        public int ProcessedFiles { get; set; }
        public int TotalFiles { get; set; }
        public string SourceDir { get; set; }
        public string TargetDir { get; set; }
        private BackgroundWorker bw;
        private Queue<long> snapshots = new(30);

        public MainWindow()
        {
            InitializeComponent();
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
                using (bw = new BackgroundWorker())
                {
                    var timer = new System.Timers.Timer(1000D);
                    timer.Elapsed += (sender, e) =>
                    {
                        // Remember only the last 30 snapshots; discard older snapshots
                        if (snapshots.Count == 30)
                        {
                            snapshots.Dequeue();
                        }

                        snapshots.Enqueue(Interlocked.Exchange(ref currentBytesTransferred, 0L));
                        var averageSpeed = snapshots.Average();
                        var bytesLeft = fileSize - totalBytesTransferred;
                        Console.WriteLine("Average speed: {0:#} MBytes / second", averageSpeed / (1024 * 1024));
                        if (averageSpeed > 0)
                        {
                            var timeLeft = TimeSpan.FromSeconds(bytesLeft / averageSpeed);
                            var timeLeftRounded = TimeSpan.FromSeconds(Math.Round(timeLeft.TotalSeconds));
                            Console.WriteLine("Time left: {0}", timeLeftRounded);
                        }
                        else
                        {
                            Console.WriteLine("Time left: Infinite");
                        }
                    };
                    bw.WorkerSupportsCancellation = true;
                    bw.DoWork += OnCopy;
                }
            }
            
        }

        private void OnCopy(object sender, DoWorkEventArgs e)
        {
            try
            {

            }
            catch (Exception)
            {

                throw;
            }
            throw new NotImplementedException();
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
    }
}
