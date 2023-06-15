using System;
using System.Linq;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Navigation;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Bikerental
{
    public partial class Page1 : Page
    {
        public Page1()
        {
            InitializeComponent();

            // if come back from Page2 show all contains
            string arriveFromPage2 = Environment.GetEnvironmentVariable("checkNavigationBack");
            if (arriveFromPage2 != null )
            {
                showAllContains();
            }
        }
        public void showAllContains()
        {
            btnCheck.Content = "RUNNING";
            btnCheck.IsEnabled = false;
            page1Label2.Visibility = visible;
            btnSelectInstall.Visibility = visible;
            page1Label2selected.Visibility = visible;
            page1Label3.Visibility = visible;
            btnSelectDatabase.Visibility = visible;
            page1Label3selected.Visibility = visible;
            page1Label4.Visibility = visible;
            btnSelectBackup.Visibility = visible;
            page1Label4selected.Visibility = visible;
            btnPage1Next.IsEnabled = true;
        }

        Visibility visible = Visibility.Visible;

        // several methods need the same value
        private string path1;
        public string selectInstall
        {
            get { return path1; }
            set { path1 = value; }
        }

        private string path2;
        public string selectDatabase
        {
            get { return path2; }
            set { path2 = value; }
        }

        private string path3;
        public string selectBackup
        {
            get { return path3; }
            set { path3 = value; }
        }
        
        // the next button is only active if all of three strings are filled
        public void checkEnableNextButton()
        { 
            if (!string.IsNullOrEmpty(selectInstall))
            {  
                if(!string.IsNullOrEmpty(selectDatabase))
                {       
                    if(!string.IsNullOrEmpty(selectBackup))
                    {
                        btnPage1Next.IsEnabled = true;
                    }
                }
            }          
        }
        public void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            checkSQLServiceStatus();
        }
        public void checkSQLServiceStatus()
        {
            string serviceName = "MSSQLSERVER";
            string serverAgent = "SQLSERVERAGENT";

            ServiceController services = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceName);
            ServiceController serviceAgent = new ServiceController(serverAgent);

            if (services != null)
            {
                if ((services.Status == ServiceControllerStatus.Stopped) || (services.Status == ServiceControllerStatus.StopPending))
                {
                    services.Start();
                    services.WaitForStatus(ServiceControllerStatus.Running);
                }

                if ((serviceAgent.Status == ServiceControllerStatus.Stopped) || (serviceAgent.Status == ServiceControllerStatus.StopPending))
                {
                    serviceAgent.Start();
                    serviceAgent.WaitForStatus(ServiceControllerStatus.Running);
                }

                btnCheck.Content = "RUNNING";
                btnCheck.IsEnabled = false;
                showContainsPart2();
            }
            else
            {
                if (System.Windows.MessageBox.Show("The SQL Server is not installed!"+Environment.NewLine+
                                                   "Please, copy the website address to download it."+Environment.NewLine+
                                                   "https://www.microsoft.com/en-us/sql-server/sql-server-downloads",
                                                   "Abort", MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.OK)
                {
                    Environment.Exit(1060);
                }
            }
        }
        public void showContainsPart2()
        {
            page1Label2.Visibility = visible;
            btnSelectInstall.Visibility = visible;
            page1Label2selected.Visibility = visible;
        }
        public void btnSelectInstall_Click(object sender, RoutedEventArgs e)
        {
            // use OpenFileDialog as FolderBrowserDialog to user can also see files not just folders
            // but user can only select folder
            OpenFileDialog folderBrowser = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Title = "Select ONLY folder in LEFT panel",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Folder Selection."
            };

            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                // check whether user selected the right folder or not
                path1 = Path.GetDirectoryName(folderBrowser.FileName);
                string pathFilename1 = path1 + @"\Bikerental.exe";

                if (!File.Exists(pathFilename1))
                {
                    if (System.Windows.MessageBox.Show("The database create file is not found!",
                                                       "Abort", MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.OK)
                    {
                        return;
                    }
                }
                else
                {
                    page1Label2selected.Text = path1;
                    selectInstall = path1;

                    // make environment variables to use it in another page
                    Environment.SetEnvironmentVariable("installFolder", path1, EnvironmentVariableTarget.Process);

                    showContainsPart3();
                    checkEnableNextButton();
                }
            }
        }
        public void showContainsPart3()
        {
            page1Label3.Visibility = visible;
            btnSelectDatabase.Visibility = visible;
            page1Label3selected.Visibility = visible;
        }
        public void btnSelectDatabase_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Please select database destination folder",
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ShowNewFolderButton = true
            };

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                path2 = folderBrowserDialog.SelectedPath;

                // check whether the free space(1GB) on the drive is enough
                string drive = Path.GetPathRoot(path2);
                DriveInfo driveInfo = new DriveInfo(drive);
                if (driveInfo.AvailableFreeSpace < 1073741824)
                {
                    System.Windows.Forms.MessageBox.Show("The database needs at least 1GB free space."+Environment.NewLine+
                                                         "Please choose another drive.",
                                                         "Abort",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                    return;
                }

                // check whether the database already exists
                if (File.Exists(path2 + @"\BR_db.mdf") || File.Exists(path2 + @"\BR_db.ldf"))
                {
                    var result=System.Windows.Forms.MessageBox.Show("The database already exists."+Environment.NewLine+
                                                                    "Do you want to reinstall it?",
                                                                    "Question",MessageBoxButtons.YesNo,MessageBoxIcon.Question);
                    if (result == DialogResult.No)
                    {
                        Environment.Exit(0);
                    }
                }

                // change permission of the directory that the installer can write in it
                // default permission is read-only
                DirectoryInfo dInfo = new DirectoryInfo(path2);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();
                dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                                        FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                        PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                dInfo.SetAccessControl(dSecurity);

                page1Label3selected.Text = path2;
                selectDatabase = path2;

                Environment.SetEnvironmentVariable("pathDatabase", path2, EnvironmentVariableTarget.Process);
                
                showContainsPart4();
                checkEnableNextButton();
            }
        }
        public void showContainsPart4()
        {
            page1Label4.Visibility = visible;
            btnSelectBackup.Visibility = visible;
            page1Label4selected.Visibility = visible;
        }
        public void btnSelectBackup_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Please select backup destination folder",
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ShowNewFolderButton = true
            };

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                // check that user try to set the same drive for the backup
                // recommend to use another
                string path3 = folderBrowserDialog.SelectedPath;
                string drive1 = Path.GetPathRoot(selectDatabase);
                string drive2 = Path.GetPathRoot(path3);

                if(drive1 == drive2)
                {
                    System.Windows.Forms.MessageBox.Show("You can use the same drive to save backup files"+Environment.NewLine+
                                                         "but recommend to choose a different drive.",
                                                         "Recommending", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                page1Label4selected.Text = path3;
                selectBackup = path3;

                Environment.SetEnvironmentVariable("pathBackup", path3, EnvironmentVariableTarget.Process);

                // replace paths of the agent script files with the selected
                string agentFull = File.ReadAllText(selectInstall + @"\backups\agent_full.txt");
                agentFull = agentFull.Replace("@pathBackup", path3);
                Environment.SetEnvironmentVariable("agent_full", agentFull, EnvironmentVariableTarget.Process);

                string agentTrans = File.ReadAllText(selectInstall + @"\backups\agent_trans.txt");
                agentTrans = agentTrans.Replace("@pathBackup", path3);
                Environment.SetEnvironmentVariable("agent_trans", agentTrans, EnvironmentVariableTarget.Process);

                string agentDiff = File.ReadAllText(selectInstall + @"\backups\agent_diff.txt");
                agentDiff = agentDiff.Replace("@pathBackup", path3);
                Environment.SetEnvironmentVariable("agent_diff", agentDiff, EnvironmentVariableTarget.Process);

                checkEnableNextButton();
            }
        }
        public void btnPage1Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Forms.MessageBox.Show("Do you want to abort the installation?",
                                                     "Abort", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                Environment.Exit(1223);
            }
            else return;
        }
        public void btnPage1Next_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Page2.xaml", UriKind.Relative));
        }
    }
}
