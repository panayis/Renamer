using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ABPRenamer
{

    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }
        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (btnStart.Text == "Execute")
            {
                StartMethod();
            }
            else
            {
                StopMethod();
            }
        }
        private void StartMethod()
        {
            Arguments arguments = new Arguments
            {
                OldCompanyName = txtOldCompanyName.Text.Trim(),
                OldProjectName = txtOldProjectName.Text.Trim(),
                OldAreaName = txtOldAreaName.Text.Trim(),

                NewCompanyName = txtNewCompanyName.Text.Trim(),
                NewAreaName = txtNewAreaName.Text.Trim()
            };
            arguments.NewProjectName = txtNewProjectName.Text.Trim();
            if (string.IsNullOrEmpty(arguments.NewProjectName))
            {
                MessageBox.Show("Please select the project path!", "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Question);
                txtNewProjectName.Focus();
                return;
            }

            arguments.RootDir = txtRootDir.Text.Trim();
            if (string.IsNullOrWhiteSpace(arguments.RootDir))
            {
                if (DialogResult.Yes == MessageBox.Show("Please select the project path!", "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Question))
                {
                    BtnSelect_Click(null, null);
                }
                return;
            }
            if (!Directory.Exists(arguments.RootDir))
            {
                MessageBox.Show("Please select the correct project path!");
                return;
            }

            if (chk.Checked && string.IsNullOrWhiteSpace(arguments.NewAreaName))
            {
                MessageBox.Show("Please type new Area name!", "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            //Show progress bar
            progressBar1.Visible = true;

            backgroundWorker1.RunWorkerAsync(arguments);
        }
        private void StopMethod()
        {
            if (backgroundWorker1.IsBusy)
            {
                MessageBox.Show("Cancelling..");
                backgroundWorker1.CancelAsync();
            }
        }
        private void Log(string value)
        {
            if (Console.InvokeRequired)
            {
                Action<string> act = (text) =>
                {
                    Console.AppendText(text);
                };
                Console.Invoke(act, value);
            }
            else
            {
                Console.AppendText(value);
            }

        }

        #region workerEvent callback

        /// <summary>
        /// workerCallback method to start execution
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker work = (BackgroundWorker)sender;
            Arguments arguments = e.Argument as Arguments;

            //Backup RootDir; when recursive, RootDir was modified
            string backupRootDir = arguments.RootDir;


            Stopwatch sp = new Stopwatch();

            long spdir;

            sp.Start();

            RenameAllDir(work, e, arguments);
            sp.Stop();
            spdir = sp.ElapsedMilliseconds;

            Log($"================= Directory renaming completed =================time spent: {spdir}(s)\r\n");

            sp.Reset();
            sp.Start();

            //Restore RootDir
            arguments.RootDir = backupRootDir;

            RenameAllFileNameAndContent(work, e, arguments);
            sp.Stop();
            Log($"================= File name and content renaming completed =================time spent: {sp.ElapsedMilliseconds}(s)\r\n");

            Log($"================= Completed =================Time-spent catalog:{ spdir }s File time spent: { sp.ElapsedMilliseconds}s\r\n");

        }
        /// <summary>
        /// workerCallback method for returning reports
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //e.UserState Send back the custom parameters passed by the report

            Log(e.UserState.ToString());

            //Percentage of asynchronous tasks
            progressBar1.PerformStep();
        }
        /// <summary>
        /// workerExecution completed callback method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Restore the status of the start button
            btnStart.Text = "Execute";

            if (e.Cancelled)
            {
                MessageBox.Show("Task terminated");
            }
            else if (e.Error != null)
            {
                MessageBox.Show("Internal error", e.Error.Message);
                throw e.Error;
            }
            else//Your business logic
            {
                if (DialogResult.Yes == MessageBox.Show("Processing completed, whether to shut down the system？", "Prompt", MessageBoxButtons.YesNo, MessageBoxIcon.Information))
                {
                    BtnClose_Click(null, new MyEventArgs());
                }
            }

        }

        public class MyEventArgs : EventArgs
        {
            //This attribute is not used, using type judgment
            public bool IsCompleted { get; set; } = true;
        }

        #endregion       

        #region Recursively rename all directories

        /// <summary>
        /// Recursively rename all directories
        /// </summary>
        private void RenameAllDir(BackgroundWorker worker, DoWorkEventArgs e, Arguments arguments)
        {
            string[] allDir = Directory.GetDirectories(arguments.RootDir);

            int i = 0;
            foreach (string currDir in allDir)
            {

                // Check if you cancel the operation
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                else// Start processing content...
                {
                    arguments.RootDir = currDir;
                    RenameAllDir(worker, e, arguments);

                    DirectoryInfo dinfo = new DirectoryInfo(currDir);
                    if (dinfo.Name.Contains(arguments.OldCompanyName) || 
                    dinfo.Name.Contains(arguments.OldProjectName) ||
                    (chk.Checked && dinfo.Name.Contains(arguments.OldAreaName)))
                    {
                        string newName = dinfo.Name;

                        if (!string.IsNullOrEmpty(arguments.OldCompanyName))
                        {
                            newName = newName.Replace(arguments.OldCompanyName, arguments.NewCompanyName);
                        }
                        newName = newName.Replace(arguments.OldProjectName, arguments.NewProjectName);

                        if (chk.Checked)
                          newName = newName.Replace(arguments.OldAreaName, arguments.NewAreaName);

                        string newPath = Path.Combine(dinfo.Parent.FullName, newName);

                        if (dinfo.FullName != newPath)
                        {
                            //Send report  ,Here only the value of the progress is sent, and the second parameter can continue to send the relevant information.
                            worker.ReportProgress((i), $"{dinfo.FullName}\r\n=>\r\n{newPath}\r\n\r\n");
                            dinfo.MoveTo(newPath);
                        }

                    }

                } //Processing content ends

            }
        }

        #endregion

        #region Recursively rename all file names and file contents

        /// <summary>
        /// Recursively rename all file names and file contents
        /// </summary>
        private void RenameAllFileNameAndContent(BackgroundWorker worker, DoWorkEventArgs e, Arguments arguments)
        {
            //Get all files with the specified file extension in the current directory
            List<FileInfo> files = new DirectoryInfo(arguments.RootDir).GetFiles().Where(m => arguments.filter.Contains(m.Extension)).ToList();

            int i = 0;
            //Rename current directory file and file content
            foreach (FileInfo item in files)
            {

                // Check if you cancel the operation
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                else// Start processing content...
                {
                    string text = File.ReadAllText(item.FullName, Encoding.UTF8);
                    if (!string.IsNullOrEmpty(arguments.OldCompanyName))
                    {
                        text = text.Replace(arguments.OldCompanyName, arguments.NewCompanyName);
                    }

                    text = text.Replace(arguments.OldProjectName, arguments.NewProjectName);
                    if (chk.Checked)
                      text = text.Replace(arguments.OldAreaName, arguments.NewAreaName);

                    if (item.Name.Contains(arguments.OldCompanyName) || 
                    item.Name.Contains(arguments.OldProjectName) ||
                    (chk.Checked && item.Name.Contains(arguments.OldAreaName)))
                    {
                        string newName = item.Name;

                        if (!string.IsNullOrEmpty(arguments.OldCompanyName))
                        {
                            newName = newName.Replace(arguments.OldCompanyName, arguments.NewCompanyName);

                        }
                        newName = newName.Replace(arguments.OldProjectName, arguments.NewProjectName);
                        if (chk.Checked)
                          newName = newName.Replace(arguments.OldAreaName, arguments.NewAreaName);
                        string newFullName = Path.Combine(item.DirectoryName, newName);

                        if (newFullName != item.FullName)
                        {
                            //Record file name changes
                            worker.ReportProgress(i, $"\r\n{item.FullName}\r\n=>\r\n{newFullName}\r\n\r\n");
                            File.Delete(item.FullName);
                        }
                        File.WriteAllText(newFullName, text, Encoding.UTF8);
                    }
                    else
                    {
                        File.WriteAllText(item.FullName, text, Encoding.UTF8);

                    }
                    worker.ReportProgress(i, $"{item.Name}=>Complete\r\n");


                } //Processing content ends

            }
            //Rename current directory file and file content

            //Get subdirectory
            string[] dirs = Directory.GetDirectories(arguments.RootDir);
            foreach (string dir in dirs)
            {

                // Check if you cancel the operation
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                else// Start processing content...
                {

                    arguments.RootDir = dir;
                    RenameAllFileNameAndContent(worker, e, arguments);
                } //Processing content ends             
            }
            //Get subdirectory
        }

        #endregion

        #region Select file path
        /// <summary>
        /// Select file path
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSelect_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = "Please select the folder where the ABP project is located.(aspnet-zero-core-7.0)"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "Folder path cannot be empty", "Prompt");
                    return;
                }
                txtRootDir.Text = dialog.SelectedPath;
            }
        }

        #endregion

        #region Exit and save settings
        private void BtnClose_Click(object sender, EventArgs e)
        {

            if (!string.IsNullOrWhiteSpace(txtFilter.Text))
            {
                Settings1.Default.setFilter = txtFilter.Text.Trim();
            }
            if (!string.IsNullOrWhiteSpace(txtOldCompanyName.Text))
            {
                Settings1.Default.setOldCompanyName = txtOldCompanyName.Text.Trim();
            }
            if (!string.IsNullOrWhiteSpace(txtOldProjectName.Text))
            {
                Settings1.Default.setOldProjectName = txtOldProjectName.Text.Trim();
            }
            if (!string.IsNullOrWhiteSpace(txtOldAreaName.Text))
            {
                Settings1.Default.setOldAreaName = txtOldAreaName.Text.Trim();
            }
            if (!string.IsNullOrWhiteSpace(txtRootDir.Text))
            {
                Settings1.Default.setRootDir = txtRootDir.Text.Trim();
            }
            Settings1.Default.setNewCompanyName = txtNewCompanyName.Text.Trim();
            if (!string.IsNullOrWhiteSpace(txtNewProjectName.Text))
            {
                Settings1.Default.setNewProjectName = txtNewProjectName.Text.Trim();
            }
            if (!string.IsNullOrWhiteSpace(txtNewAreaName.Text))
            {
                Settings1.Default.setNewAreaName = txtNewAreaName.Text.Trim();
            }

            if (e is MyEventArgs)
            {
                Settings1.Default.setOldCompanyName = txtNewCompanyName.Text.Trim();
                Settings1.Default.setOldProjectName = txtNewProjectName.Text.Trim();
            }

            Settings1.Default.Save();
            Environment.Exit(0);
        }
        #endregion

        #region Start load settings
        private void FormMain_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Settings1.Default.setFilter))
            {
                txtFilter.Text = Settings1.Default.setFilter.Trim(); ;
            }
            if (!string.IsNullOrWhiteSpace(Settings1.Default.setOldCompanyName))
            {
                txtOldCompanyName.Text = Settings1.Default.setOldCompanyName.Trim();
            }
            if (!string.IsNullOrWhiteSpace(Settings1.Default.setOldProjectName))
            {
                txtOldProjectName.Text = Settings1.Default.setOldProjectName.Trim();
            }
            if (!string.IsNullOrWhiteSpace(Settings1.Default.setOldAreaName))
            {
                txtOldAreaName.Text = Settings1.Default.setOldAreaName.Trim();
            }
            if (!string.IsNullOrWhiteSpace(Settings1.Default.setRootDir))
            {
                txtRootDir.Text = Settings1.Default.setRootDir.Trim();
            }
            if (!string.IsNullOrWhiteSpace(Settings1.Default.setNewCompanyName))
            {
                txtNewCompanyName.Text = Settings1.Default.setNewCompanyName.Trim();
            }
            if (!string.IsNullOrWhiteSpace(Settings1.Default.setNewProjectName))
            {
                txtNewProjectName.Text = Settings1.Default.setNewProjectName.Trim();
            }
            if (!string.IsNullOrWhiteSpace(Settings1.Default.setNewAreaName))
            {
                txtNewAreaName.Text = Settings1.Default.setNewAreaName.Trim();
            }
        }
        #endregion

        #region Restore Defaults
        private void BtnReset_Click(object sender, EventArgs e)
        {
            txtFilter.Text = ".cs,.cshtml,.js,.ts,.csproj,.sln,.xml,.config,.DotSettings,.json";
        }

        #endregion

        private void lbOriginalName_Click(object sender, EventArgs e)
        {
            txtOldCompanyName.Text = "MyCompanyName";
        }

        private void lbOriginalProjectName_Click(object sender, EventArgs e)
        {
            txtOldProjectName.Text = "AbpZeroTemplate";
        }

        private void lbProjectPath_Click(object sender, EventArgs e)
        {
            txtRootDir.Text = "";
        }

        private void lbNewCompanyName_Click(object sender, EventArgs e)
        {
            txtNewCompanyName.Text = "";
        }

        private void lbNewProjectName_Click(object sender, EventArgs e)
        {
            txtNewProjectName.Text = "";
        }

        private void lbOriginalAreaName_Click(object sender, EventArgs e)
        {
            txtOldAreaName.Text = "AppAreaName";
        }

        private void chk_CheckedChanged(object sender, EventArgs e)
        {
            lbOriginalAreaName.Enabled = chk.Checked;
            txtOldAreaName.Enabled = chk.Checked;
            lbArrow3rd.Enabled = chk.Checked;
            lbNewAreaName.Enabled = chk.Checked;
            txtNewAreaName.Enabled = chk.Checked;
        }
  }
  public class Arguments
  {
      public readonly string filter = ".cs,.cshtml,.js,.ts,.csproj,.sln,.xml,.config,.DotSettings";
      private string _oldCompanyName = "MyCompanyName";
      public string OldCompanyName
      {
          get => string.IsNullOrWhiteSpace(NewCompanyName) ? _oldCompanyName + "." : _oldCompanyName;
          set => _oldCompanyName = value;

      }
      public string OldProjectName { get; set; } = "AbpZeroTemplate";
      public string NewCompanyName { get; set; }
      public string NewProjectName { get; set; }
      public string OldAreaName { get; set; } = "AppAreaName";
      public string NewAreaName { get; set; } = "App";
      public string RootDir { get; set; }
  }
}
