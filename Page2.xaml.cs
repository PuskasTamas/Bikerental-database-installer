using System;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Navigation;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Bikerental
{
    public partial class Page2 : System.Windows.Controls.Page
    {
        public Page2()
        {
            InitializeComponent();

            getDataSources();
        }
        string selectedServer;
        public void getDataSources()
        {
            // check the registry to find all servers and instance names
            // whatever that the system is 32 or 64 bit
            string ServerName = Environment.MachineName;
            RegistryView registryView = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView))
            {
                RegistryKey instanceKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL", false);
                if (instanceKey != null)
                {
                    foreach (var instanceName in instanceKey.GetValueNames())
                    {
                        if (instanceName.Equals("MSSQLSERVER"))
                        {
                            // if user want to use the default instance it have to delete the instance name and only use the server name
                            // otherwise user get an error and it will not work
                            cbPage2.Items.Add(ServerName);
                            continue;
                        }
                        cbPage2.Items.Add(ServerName + @"\" + instanceName);
                    }
                }
            }
        }
        // connection by windows authenticator
        public void btnWindowsAuthor_Click(object sender, RoutedEventArgs e)
        {
            selectedServer = cbPage2.SelectedItem.ToString();

            if (selectedServer == null)
            {
                System.Windows.MessageBox.Show("Please select a server!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = @"Server=" + selectedServer + ";Database=master;Trusted_Connection=SSPI;";
            string connect = conn.ConnectionString;

            try
            {
                conn.Open();
                System.Windows.MessageBox.Show("Connection is successful!", "Connect", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                System.Windows.MessageBox.Show("Connection is failed!", "Abort", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Environment.SetEnvironmentVariable("connectString", connect, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("loginName", System.Security.Principal.WindowsIdentity.GetCurrent().Name, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("selectedServer", selectedServer, EnvironmentVariableTarget.Process);
            NavigationService.Navigate(new Uri("Page3.xaml", UriKind.Relative));
        }
        // connection by username and password
        public void btnUserPasswordAuthor_Click(object sender, RoutedEventArgs e)
        {
            selectedServer = cbPage2.SelectedItem.ToString();

            if (selectedServer == null)
            {
                System.Windows.MessageBox.Show("Please select a server!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Environment.SetEnvironmentVariable("selectedServer", selectedServer, EnvironmentVariableTarget.Process);

            // use popup window to get user name and password
            LoginWindow window = new LoginWindow();
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        public void btnPage2Back_Click(object sender, RoutedEventArgs e)
        {
            Environment.SetEnvironmentVariable("checkNavigationBack", "true", EnvironmentVariableTarget.Process);
            NavigationService.GoBack();
        }
        public void btnPage2Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Forms.MessageBox.Show("Do you want to abort the installation?", "Abort", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                Environment.Exit(1223);
            }
            else return;
        }
    }
}
