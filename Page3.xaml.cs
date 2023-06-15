using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Data;
using System.Data.Common;
using System.Windows.Threading;
using System.Windows.Media;

namespace Bikerental
{
    // this method allows use the Refresh system method at ANY elements
    // past this outside the main class
    public static class ExtensionMethods
    {
        private static readonly Action EmptyDelegate = delegate { };
        public static void Refresh(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
    }
    public partial class Page3 : Page
    {
        public Page3()
        {
            InitializeComponent();
        }
        
        //  get envinronment variables to use they during install
        string connect = Environment.GetEnvironmentVariable("connectString");
        string install = Environment.GetEnvironmentVariable("installFolder");
        string pathDatabase = Environment.GetEnvironmentVariable("pathDatabase");
        string loginName = Environment.GetEnvironmentVariable("loginName");
        string selectedServer = Environment.GetEnvironmentVariable("selectedServer");

        string script;
        string getFileName;
        
        string allRight = " ..... OK";
        SolidColorBrush red = new SolidColorBrush(Color.FromRgb(167, 0, 0));

        public void btnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            btnBack.IsEnabled = false;
            btnCancel.IsEnabled = false;
            pbStatus.Visibility = Visibility.Visible;
            pbPercentage.Visibility = Visibility.Visible;
            txtBox.Visibility = Visibility.Visible;
            btnExit.Visibility = Visibility.Visible;
            createMainDatabase();
        }
        public void createMainDatabase()
        {
            // count sql files to determine the value of progressbar
            int countFiles = Directory.EnumerateFiles(install, "*.sql", SearchOption.AllDirectories).Count();
            pbStatus.Maximum = countFiles;

            // first it needs to build the main parts of the database in the appropriate order
            foreach (string files in Directory.GetFiles(install + @"\basics_database\").OrderBy(f => f))
            {
                runSqlScript(files);
            }
            buildDatabase();
        }
        public void buildDatabase()
        {
            // execute all sql files except basics of database
            foreach (string folder in Directory.GetDirectories(install, "*", SearchOption.AllDirectories).Where(d => !d.Contains("basics_database")))
            {
                foreach (string files in Directory.GetFiles(folder, "*.sql"))
                {
                    runSqlScript(files);
                }
            }
            exitInstaller();
        }
        public bool runSqlScript(string sqlFile)
        {
            try
            {
                script = File.ReadAllText(sqlFile);
                getFileName = Path.GetFileNameWithoutExtension(sqlFile);
                
                if (getFileName == "create_db_main")
                {
                // replace the path in sql script with the selected
                    script = script.Replace("@primary", pathDatabase+@"\BR_db.mdf").Replace("@log", pathDatabase+@"\BR_db.ldf");
                }

                // agent job script files
                if (getFileName == "BikeRental_Differential" || getFileName == "BikeRental_Full" || getFileName == "BikeRental_Transaction")
                {
                    checkAgentJobIsExists();

                // agent files must need to skip the remain part of this method
                // they was already created by another method
                    goto Missed;
                }

                StringBuilder errorMessages = new StringBuilder();

                // sqlcommand can not handle GO, so must split it
                IEnumerable<string> commandString = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                SqlConnection sqlConnection = new SqlConnection(connect);
                sqlConnection.Open();
                foreach (string commandIn in commandString)
                {
                    if (!string.IsNullOrWhiteSpace(commandIn.Trim()))
                    {
                        using (var command = new SqlCommand(commandIn, sqlConnection))
                        {
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (SqlException ex)
                            {
                                for (int i = 0; i < ex.Errors.Count; i++)
                                {
                                    errorMessages.Append("Index #" + i + "\n" +
                                        "Message: " + ex.Errors[i].Message + "\n" +
                                        "Error Number: " + ex.Errors[i].Number.ToString() + "\n" +
                                        "LineNumber: " + ex.Errors[i].LineNumber.ToString() + "\n" +
                                        "Source: " + ex.Errors[i].Source + "\n" +
                                        "Procedure: " + ex.Errors[i].Procedure + "\n");
                                }
                                txtBox.Foreground = red;
                                txtBox.AppendText(sqlFile + " ..... WRONG!!!" + Environment.NewLine);
                                txtBox.Refresh();
                                txtBox.ScrollToEnd();
                                System.Windows.Forms.MessageBox.Show(errorMessages.ToString(),
                                                                     "Error in the "+sqlFile+" SQL script file!",
                                                                     MessageBoxButtons.OK , MessageBoxIcon.Error);    
                                exitError();
                            }
                        }
                    }
                }
                appendFilenameTextbox(getFileName.ToUpper());

                sqlConnection.Close();
                Missed:
                    return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message,
                                                     "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
        public void appendFilenameTextbox(string fileName)
        {
            txtBox.AppendText(fileName + allRight + Environment.NewLine);
            txtBox.Refresh();
            txtBox.ScrollToEnd();
            pbStatus.Value++;
            pbStatus.Refresh();
            int percent = (int)(((double)pbStatus.Value / (double)pbStatus.Maximum) * 100);
            pbPercentage.Text = percent + "%";
            pbPercentage.Refresh();
        }
        public void checkAgentJobIsExists()
        {
            SqlConnection sqlConnection = new SqlConnection(connect);
            sqlConnection.Open();
            using (DbCommand command = new SqlCommand("SELECT job_id FROM msdb.dbo.sysjobs_view WHERE name = @jobName"))
            {
                command.Parameters.Add(new SqlParameter("@jobName", getFileName));
                command.Connection = sqlConnection;
                var result = command.ExecuteScalar();
                if (result != null)
                {
                    var result2 = System.Windows.Forms.MessageBox.Show("The "+ getFileName+" agent job already exists."+Environment.NewLine+
                                                                       "Do you want to delete it and make a new of it?",
                                                                       "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result2 == DialogResult.No)
                    {
                        pbStatus.Maximum--;
                        return;
                    }
                    else
                    {
                        using (DbCommand command2 = new SqlCommand("EXEC msdb.dbo.sp_delete_job @job_name = @jobName;"))
                        {
                            command2.Parameters.Add(new SqlParameter("@jobName", getFileName));
                            command2.Connection = sqlConnection;
                            command2.ExecuteNonQuery();
                        }
                        createAgentJobs();
                    }
                }
                else createAgentJobs();
            }
        }
        public void createAgentJobs()
        {
            string txtFile = Environment.GetEnvironmentVariable(getFileName);
            string formattedDate = DateTime.Now.ToString("yyyyMMdd");

            // replace datas in script files
            script = script.Replace("@loginName", loginName).
                            Replace("@selectedServer", selectedServer).
                            Replace(@"@" + getFileName, txtFile).
                            Replace("@date", formattedDate);

            // make temporary sql script then delete it
            string modifiedAgentFile = pathDatabase + @"\" + getFileName + "tmp.sql";
            File.WriteAllText(modifiedAgentFile, script);
            SqlConnection sqlConnection = new SqlConnection(connect);
            sqlConnection.Open();
            ProcessStartInfo info = new ProcessStartInfo("sqlcmd", @" -d BikeRental -i " + modifiedAgentFile)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true
            };
            try
            {
                using (Process p = Process.Start(info))
                    p.WaitForExit();
                    appendFilenameTextbox(getFileName.ToUpper());
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, 
                                                     "Error in the "+getFileName+" agent file!", MessageBoxButtons.OK,MessageBoxIcon.Error);
                File.Delete(modifiedAgentFile);
                txtBox.Foreground = red;
                txtBox.AppendText(getFileName + " ..... WRONG!!!" + Environment.NewLine);
                txtBox.Refresh();
                txtBox.ScrollToEnd();
                exitError();
            }
            File.Delete(modifiedAgentFile);
            sqlConnection.Close();
        }
        public void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
        public void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Forms.MessageBox.Show("Do you want to abort the installation?",
                                                     "Abort", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                Environment.Exit(1223);
            }
            else return;
        }
        public void exitInstaller()
        {
            pbPercentage.FontWeight = FontWeights.Bold;
            pbPercentage.Foreground = new SolidColorBrush(Color.FromRgb(69,162,71));
            string end = Environment.NewLine+"DATABASE IS INSTALLED!";
            appendFilenameTextbox(end);
            btnExit.IsEnabled = true;
        }
        public void exitError()
        {
            pbPercentage.FontWeight = FontWeights.Bold;
            pbPercentage.Foreground = red;
            txtBox.Foreground = red;
            string end = Environment.NewLine+ 
                         "DATABASE IS INCOMPLETE AND UNUSABLE!!!"+Environment.NewLine+
                         "PLEASE REPAIR THE ISSUE AND RESTART THIS INSTALLER!!!";
            appendFilenameTextbox(end);
            btnExit.IsEnabled = true;
        }
        public void btnExit_Click(object sender, RoutedEventArgs e)
        {
            if(txtBox.Text.Contains("WRONG") == true)
            {
                Environment.Exit(30);
            }
            Environment.Exit(0);
        }
    }
}