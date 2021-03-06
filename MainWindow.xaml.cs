using System;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace UnityProjectCloner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.IO.FileSystemWatcher watcher;
        System.Windows.Forms.NotifyIcon notifyIcon;
        string outputDir;
        string rootDir;
        CancellationTokenSource cts;
        string[] exclusions = new string[] { "UnityLockFile", ".lck", ".lock"};


        public MainWindow()
        {
            InitializeComponent();
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Text = "Unity Project Cloner";
            notifyIcon.Icon = Properties.Resources.app;
            notifyIcon.MouseClick += NotifyIcon_MouseClick;
            txtOutput.Text = Properties.Settings.Default.lastOutputDir;
            txtProject.Text = Properties.Settings.Default.lastProjectDir;
        }

        private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.Show();
        }

        private void btnBrowseProject_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.Description = "Select the folder where your project files are located.";
            if(fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtProject.Text = fbd.SelectedPath;
            }
        }

        private void btnBrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.Description = "Select the folder where you want to clone/save the project files.";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtOutput.Text = fbd.SelectedPath;
            }
        }

        private async void btnRun_Click(object sender, RoutedEventArgs e)
        {
            //Check if the input directory is valid and exists
            if (string.IsNullOrEmpty(txtProject.Text) || System.IO.Directory.Exists(txtProject.Text) == false)
            {
                MessageBox.Show("Please set a valid input directory. ", "ERROR");
                return;
            }

            //Check if the output directory is valid. 
            if (string.IsNullOrEmpty(txtOutput.Text) || System.IO.Directory.Exists(txtOutput.Text) == false)
            {
                MessageBox.Show("Please set a valid output directory. Make sure the selected output directory exists!", "ERROR");
                return;
            }
            outputDir = Path.GetFullPath(txtOutput.Text);
            rootDir = Path.GetFullPath(txtProject.Text);

            //Symlink mode is handled here, because its simple to set up. We just call mklink on the target dir
            if(radSymLink.IsChecked==true)
            {
                var dirs = new string[2]
                {
                    "Assets",
                    "ProjectSettings"
                };
                foreach (var dir in dirs)
                {
                    var dirInput = Path.GetFullPath(rootDir + "\\" + dir);
                    var dirOutput = Path.GetFullPath(outputDir + "\\" + dir);
                    //Syntax for MKLINK is mklink /D [TARGET] [ORIGINAL]. Of course, requires to be ran as admin
                    var p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.Arguments = "/c mklink /D";
                    //Add quotation to inputs if they have spaces
                    var filteredInputDir = dirInput.Contains(" ") ? "\"" + dirInput + "\"" : dirInput;
                    var filteredOutputDir = dirOutput.Contains(" ") ? "\"" + dirOutput + "\"" : dirOutput;
                    p.StartInfo.Arguments += $" {dirOutput} {dirInput}";
                    p.StartInfo.Verb = "runas"; //To Run as Admin
                    p.Start();
                }
                MessageBox.Show("Symlinks are created in "+outputDir, "Unity Project Cloner");
                Environment.Exit(0);
            }

            //Initialize the file system watcher
            watcher = new System.IO.FileSystemWatcher(txtProject.Text);  //Avoid lock files. 
            watcher.IncludeSubdirectories = true;
            watcher.Changed += Watcher_Changed;
            watcher.Renamed += Watcher_Renamed;
            watcher.Deleted += Watcher_Deleted;
            watcher.Created += Watcher_Created;

            void _startActions()
            {
                //Set the notify icon
                notifyIcon.Visible = true;
                btnRun.IsEnabled = false;
                btnStop.IsEnabled = true;
                notifyIcon.Text = "Syncing Project Directories...";
                progressBar.Visibility = Visibility.Visible;
                lblInfo.Visibility = Visibility.Visible;
                lblInfo.Content = "Syncing Project, Please wait..";
                notifyIcon.Icon = Properties.Resources.warning;
                Properties.Settings.Default.lastProjectDir = rootDir;
                Properties.Settings.Default.lastOutputDir = outputDir;
                Properties.Settings.Default.Save();
            }
            void _stopActions()
            {
                watcher.EnableRaisingEvents = false;
                btnStop.IsEnabled = false;
                btnRun.IsEnabled = true;
                notifyIcon.Visible = false;
                progressBar.Visibility = Visibility.Hidden;
                lblInfo.Visibility = Visibility.Visible;
                watcher.Dispose();
            }

            _startActions();
            try
            {
                cts = new CancellationTokenSource();
                await SyncDirectory(rootDir, outputDir, this.cts.Token);
                if(cts.IsCancellationRequested)
                {
                    _stopActions();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error syncing project. Please make sure you have access to the directory. \r\nFor details, check the following error message. \n" + ex, "ERROR");
                _stopActions();
                return;
            }
            this.Hide();
            notifyIcon.Text = "Unity Project Cloner - Ready";
            notifyIcon.Icon = Properties.Resources.ready;
            progressBar.Visibility = Visibility.Hidden;
            lblInfo.Visibility = Visibility.Hidden;
            //Enable watcher.
            watcher.EnableRaisingEvents = true;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            watcher.EnableRaisingEvents = false;
            notifyIcon.Visible = false;
            btnStop.IsEnabled = false;
            cts.Cancel(); 
            btnRun.IsEnabled = true;
            progressBar.Visibility = Visibility.Hidden;
            lblInfo.Visibility = Visibility.Visible;
            watcher.Dispose();
        }

        private async void Watcher_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                if (CheckExclusion(e.FullPath)) return;
                SetNotifyIcon(true);
                await Task.Run(delegate
                {
                    //Subtract the root directory from the full path and then add it to the outputDir 
                    var fp = e.FullPath;
                    //Substring the rootDir? 
                    var relativePath = fp.Substring(rootDir.Length);
                    //Add this to outputDir for calculating final path
                    var finalPath = Path.GetFullPath(outputDir + "\\" + relativePath);
                    //Now, let's copy :V 
                    try
                    {
                        if (Directory.Exists(fp))
                        {
                            //Create the directory
                            Directory.CreateDirectory(finalPath);
                        }
                        else if (File.Exists(fp))
                        {
                            //Copy the file
                            File.Copy(fp, finalPath, true);
                        }
                    }
                    catch
                    {

                    }
                });
                SetNotifyIcon(false);
            }
        }

        private async void Watcher_Deleted(object sender, System.IO.FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                if (CheckExclusion(e.FullPath)) return;
                SetNotifyIcon(true);
                await Task.Run(delegate
                {
                    //Subtract the root directory from the full path and then add it to the outputDir 
                    var fp = e.FullPath;
                    //Substring the rootDir? 
                    var relativePath = fp.Substring(rootDir.Length);
                    //Add this to outputDir for calculating final path
                    var finalPath = Path.GetFullPath(outputDir + "\\" + relativePath);
                    //Now, let's delete
                    try
                    {
                        if (Directory.Exists(finalPath))
                        {
                            //Delete the directory
                            Directory.Delete(finalPath, true);
                        }
                        else if (File.Exists(finalPath))
                        {
                            //Delete the file

                            File.Delete(finalPath);
                        }
                    }
                    catch
                    {

                    }
                });
                SetNotifyIcon(false);
            }
        }

        private async void Watcher_Renamed(object sender, System.IO.RenamedEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Renamed)
            {
                if (CheckExclusion(e.FullPath)) return;
                SetNotifyIcon(true);
                await Task.Run(delegate
                {
                    //Subtract the root directory from the full path and then add it to the outputDir 
                    var fp = e.OldFullPath;
                    //new full path
                    var nfp = e.FullPath;
                    //Substring the rootDir? 
                    var relativePath = fp.Substring(rootDir.Length);
                    //new relative path
                    var nRelativePath = nfp.Substring(rootDir.Length);
                    //Add this to outputDir for calculating final path
                    var finalPath = Path.GetFullPath(outputDir + "\\" + relativePath);
                    //New final path
                    var nFinalPath = Path.GetFullPath(outputDir + "\\" + nRelativePath);
                    //Now, let's rename 
                    if (Directory.Exists(finalPath))
                    {
                        //Rename the directory
                        Directory.Move(finalPath, nFinalPath);
                    }
                    else if (File.Exists(finalPath))
                    {
                        //Move the file with new name. These events are not really handled well because these 
                        //files might already be in use, and moving them won't work. 
                        try
                        {
                            File.Move(finalPath, nFinalPath);
                        }
                        catch
                        {

                        }
                    }
                });
                SetNotifyIcon(false);
            }
        }

        private async void Watcher_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            //Here, a file is modified. We just need to replace it
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                if (CheckExclusion(e.FullPath)) return;
                SetNotifyIcon(true);
                await Task.Run(delegate
                {
                    //Subtract the root directory from the full path and then add it to the outputDir 
                    var fp = e.FullPath;
                    //Substring the rootDir? 
                    var relativePath = fp.Substring(rootDir.Length);
                    //Add this to outputDir for calculating final path
                    var finalPath = Path.GetFullPath(outputDir + "\\" + relativePath);
                    //Now, let's copy :V 
                    if (File.Exists(fp))
                    {
                        //Copy the file
                        try
                        {
                            File.Copy(fp, finalPath, true);
                        }
                        catch
                        {
                            //Sometimes we get read errors due to locks (such as on user-mapped files). In such case, we 
                            //can just ignore those errors because the important files are synced anyway
                        }
                    }
                });
                SetNotifyIcon(false);
            }
        }

        private bool CheckExclusion(string inp)
        {
            foreach (var s in exclusions)
            {
                if (inp.ToLower().Contains(s.ToLower())) return true;
            }
            return false;
        }

        /// <summary>
        /// Completely synchronizes (copies) source directory to destinition directory (replacing only old files and copying new files).
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        private async Task SyncDirectory(string source, string dest, CancellationToken token)
        {
            //Get each file in source, check if it exists in target. 
            int fCount = 0;
            await Task.Run(delegate
            {
                Queue<string> qDirs = new Queue<string>();
                qDirs.Enqueue(source);
                while (qDirs.Count > 0)
                {
                    if (token.IsCancellationRequested) return;
                    var cDir = qDirs.Dequeue();
                    if (CheckExclusion(cDir)) continue;
                    //Process cDir
                    foreach (var f in Directory.GetFiles(cDir))
                    {
                        if (token.IsCancellationRequested) return;
                        if (CheckExclusion(f)) continue;
                        var finalPath = Path.GetFullPath(dest + "\\" + f.Substring(source.Length)); //Exclude root directory, append rest of the filename.
                                                                                                    //Copy the file to finalPath (if its new) 
                        if (File.Exists(finalPath))
                        {
                            //Since we are copying files using System calls, the time-stamps should be same. 
                            if (File.GetLastWriteTimeUtc(f) > File.GetLastWriteTimeUtc(finalPath)) //f is newer than finalPath?
                            {
                                //Replace finalPath with f
                                try
                                {
                                    File.Copy(f, finalPath, true);
                                }
                                catch
                                {
                                    //Sometimes we get read errors due to locks (such as on user-mapped files). In such case, we 
                                    //can just ignore those errors because the important files are synced anyway
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                //Just copy.
                                if (Directory.Exists(Path.GetDirectoryName(finalPath)) == false)
                                    Directory.CreateDirectory(Path.GetDirectoryName(finalPath));
                                File.Copy(f, finalPath);
                            }
                            catch
                            {
                            }
                        }
                        fCount++;
                    }
                    //Get all directories and add them to the que
                    foreach (var dir in Directory.GetDirectories(cDir))
                    {
                        if (token.IsCancellationRequested) return;
                        if (CheckExclusion(dir)) continue;
                        qDirs.Enqueue(dir);
                    }
                }
            });
            notifyIcon.ShowBalloonTip(1500, "Unity Project Cloner", $"Synchronized {fCount} files. Your cloned project is ready to be used.", System.Windows.Forms.ToolTipIcon.Info);
        }

        /// <summary>
        /// Toggles the notify icon state. 
        /// </summary>
        /// <param name="isSyncing"></param>
        void SetNotifyIcon(bool isSyncing)
        {
            if (isSyncing)
            {
                this.notifyIcon.Icon = Properties.Resources.warning;
                this.notifyIcon.Text = "Unity Project Cloner - Syncing...";
            }
            else
            {
                this.notifyIcon.Icon = Properties.Resources.ready;
                this.notifyIcon.Text = "Unity Project Cloner - Ready";
            }
        }
    }
}
