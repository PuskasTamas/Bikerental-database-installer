using System;
using System.Data.SqlClient;
using System.Security;
using System.Windows;
using System.Windows.Controls;

namespace Bikerental
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        string userName;
        SecureString securePassword;
        string selectedServer = Environment.GetEnvironmentVariable("selectedServer");
        string connect;
        public void btnLogin_Click (object sender, RoutedEventArgs e)
        {
            userName = loginName.Text;
            securePassword = loginPassword.SecurePassword;
            
            try
            {
                using (SqlConnection conn = new SqlConnection())
                {
                    conn.ConnectionString = @"Server=" + selectedServer + ";Database=master; User Id=" + userName + ";Password=" + securePassword + ";";
                    connect = conn.ConnectionString;
                    conn.Open();
                    MessageBox.Show("Connection is successful!", "Connect", MessageBoxButton.OK, MessageBoxImage.Information);
                }   
            }
            catch
            {
                MessageBox.Show("Connection failed!", "Abort", MessageBoxButton.OK, MessageBoxImage.Error);   
            }

            Environment.SetEnvironmentVariable("connectString", connect, EnvironmentVariableTarget.Process);
            frame.NavigationService.Navigate(new Uri("Page3.xaml", UriKind.Relative));
        }
        // the lenght of the both of the name and password must be at least 7 characters
        // check it real time
        public void textBoxChanged(object sender, TextChangedEventArgs e)
        { 
            if(!string.IsNullOrWhiteSpace(loginName.Text))
            {
                if(loginName.Text.Length > 6)
                {
                    if (!string.IsNullOrWhiteSpace(loginPassword.Password))
                    {
                        if(loginPassword.Password.Length > 6)
                            btnLogin.IsEnabled = true;
                    }
                }
                if(loginName.Text.Length <= 6)
                {
                    btnLogin.IsEnabled = false;
                }
            }
        }
        public void passwordChanged(object sender, RoutedEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(loginPassword.Password))
            {
                if (loginPassword.Password.Length > 6)
                {
                    if (!string.IsNullOrWhiteSpace(loginName.Text))
                    {
                        if (loginName.Text.Length > 6)
                            btnLogin.IsEnabled = true;
                    }
                }
                if(loginPassword.Password.Length <= 6)
                {
                    btnLogin.IsEnabled = false;
                }
            }
        }
        public void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
