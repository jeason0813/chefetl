using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using System.IO;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Dts.Runtime;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.IntegrationServices;
using System.Threading;


namespace AdminUtility
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int QueueId = 0;
       // private string packagePath = string.Empty;
        private string connectionString =string.Empty;
        bool? pkgResults = null;
         
        //Create a Delegate that matches 
        //the Signature of the ProgressBar's SetValue method
        private delegate void UpdateProgressBarDelegate(
                System.Windows.DependencyProperty dp, Object value);

        private delegate void UpdateProgressBarExecutePackageDelegate(
                System.Windows.DependencyProperty dp, Object value);

        private void ProcessBar()
        {
            //Configure the ProgressBar
            ProgressBar1.Value = 0;

            //Stores the value of the ProgressBar
            double value = 0;

            //Create a new instance of our ProgressBar Delegate that points
            // to the ProgressBar's SetValue method.
            UpdateProgressBarDelegate updatePbDelegate =
                new UpdateProgressBarDelegate(ProgressBar1.SetValue);

            //Tight Loop: Loop until the ProgressBar.Value reaches the max
            do
            {
                value += 1;
                Dispatcher.Invoke(updatePbDelegate,
                    System.Windows.Threading.DispatcherPriority.Background,
                    new object[] { ProgressBar.ValueProperty, value });
            }
            while (ProgressBar1.Value != ProgressBar1.Maximum);
        }

        private void ProcessExecutePackageBar()
        {
            ProgressBarExecutePackage.Value = 0;
            double value = 0;
            UpdateProgressBarExecutePackageDelegate updatePbDelegate =
            new UpdateProgressBarExecutePackageDelegate(ProgressBarExecutePackage.SetValue);
            do
            {
                value += 1;
                Dispatcher.Invoke(updatePbDelegate,
                    System.Windows.Threading.DispatcherPriority.Background,
                    new object[] { ProgressBar.ValueProperty, value });
            }
            while (ProgressBarExecutePackage.Value != ProgressBarExecutePackage.Maximum);
        }

        public MainWindow()
        {
            InitializeComponent();

            //packagePath = ConfigurationManager.AppSettings["PackagesPath"].ToString();
            connectionString = ConfigurationManager.ConnectionStrings["CHEF"].ToString();  
           
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxPackages.SelectedIndex != -1)
            {

                KeyValuePair maxPages = (KeyValuePair)comboBoxPackages.SelectedItem;
                string skey = maxPages.Key.ToString();
                string svalue = maxPages.Value.ToString();

                string pkgLocation = string.Empty;
                string pkgFullName = string.Empty;
                int requestStatus = 0;
                //Package pkg;
                //Microsoft.SqlServer.Dts.Runtime.Application app;
                //DTSExecResult pkgResults;
                Microsoft.SqlServer.Management.IntegrationServices.PackageInfo packageInfo = null;

                try
                {
                    ProgressBarExecutePackage.Visibility = Visibility.Visible;
                    Mouse.OverrideCursor = Cursors.Wait;
                    ProcessExecutePackageBar();
                   
                    // Insert an entry in to Request Queue Table with Request Status 1

                    SqlConnection sqlConn = new SqlConnection(connectionString);
                    SqlCommand sqlCommand = new SqlCommand("CHEF.InsertRequestQueue", sqlConn);
                    sqlCommand .CommandType = CommandType.StoredProcedure;
                    SqlParameter ProcessID = sqlCommand .Parameters.Add("@ProcessID", SqlDbType.Int, 5);
                    ProcessID.Value = skey;
                    SqlParameter RequestStatus = sqlCommand .Parameters.Add("@RequestStatus", SqlDbType.TinyInt, 1);
                    RequestStatus.Value = 1;
                    sqlConn.Open();
                    int result = sqlCommand .ExecuteNonQuery();
                    
                    sqlConn.Close();
                    
                    string selecteSql = "select max(QueueID) as QueueID from chef.RequestQueue" + " where ProcessID= @processId and RequestStatus=1";
                    SqlCommand selectSqlCommand = new SqlCommand(selecteSql, sqlConn);
                    selectSqlCommand.Parameters.Add("@processId", SqlDbType.Int, 6, "ProcessID");
                    selectSqlCommand.Parameters["@processId"].Value = skey;

                    sqlConn.Open();
                    SqlDataReader selectDataReader = selectSqlCommand.ExecuteReader();

                    while (selectDataReader.Read())
                    {
                        QueueId = Convert.ToInt32(selectDataReader["QueueID"]);
                    }
                    sqlConn.Close();
                    sqlCommand = new SqlCommand("CHEF.ExecutePackageFromCatalog", sqlConn);
                    sqlConn.Open();
                    result = sqlCommand.ExecuteNonQuery();
                    sqlConn.Close();
              
                    if (result!=1)
                    {
                        pkgResults = false;
                        requestStatus = 4;
                    }
                    else
                    {
                        pkgResults = true;
                        requestStatus = 2;
                    }
                    // Update Request Queue Table with Request Status with result of package execution
                    SqlConnection updateSqlConnection = new SqlConnection(connectionString);
                    string updateSql = "UPDATE CHEF.RequestQueue " + "SET RequestStatus = @requestStatus " + "WHERE QueueID = @QueueID";
                    SqlCommand UpdateSqlCommand = new SqlCommand(updateSql, updateSqlConnection);
                    UpdateSqlCommand.Parameters.Add("@requestStatus", SqlDbType.Int, 5, "RequestStatus");
                    UpdateSqlCommand.Parameters.Add("@QueueID", SqlDbType.Int, 6, "QueueID");
                    UpdateSqlCommand.Parameters["@requestStatus"].Value = requestStatus;
                    UpdateSqlCommand.Parameters["@QueueID"].Value = QueueId;
                    updateSqlConnection.Open();
                    UpdateSqlCommand.ExecuteNonQuery();

                    if (pkgResults==true)
                    {
                        MessageBox.Show("Package in Project "+ skey + "_" + svalue + ".ispac Successfully Executed. ", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Package Execution Failed for Package in Project " + skey + "_" + svalue, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    buttonViewLog.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    ProgressBarExecutePackage.Visibility = Visibility.Hidden;
                    Mouse.OverrideCursor = Cursors.Arrow;
                }
            }
            else
            {
                MessageBox.Show("Please select the Package", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void buttonBrowseMetaDataFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "XML|*.xml";

            if ((bool)openFileDialog.ShowDialog())
            {
                textBoxMetaDataFile.Text = openFileDialog.FileName;
            }
        }
       
        const int SW_HIDE = 0;
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private void buttonCreatePackage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxMetaDataFile.Text.Trim()))
            {
                MessageBox.Show("Please select the Metadata file", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            //Calling the CHEF.exe
            try
            {
                string packagePath = ReadMetaDataFromFile(textBoxMetaDataFile.Text.Trim());
                string chefExe = @"CHEF.exe";
                
                ProgressBar1.Visibility = Visibility.Visible;
                Mouse.OverrideCursor = Cursors.Wait;

                ProcessStartInfo ProcessInfo;
                Process Process;
                ProcessInfo = new ProcessStartInfo(chefExe);
                ProcessInfo.Arguments ="\""+ packagePath+"\"";
                ProcessInfo.CreateNoWindow = true;
                ProcessInfo.UseShellExecute = false;
                Process = Process.Start(ProcessInfo);
                ProcessBar();
                Process.WaitForExit();
                Process.Close();
                buttonCreationLog.Visibility = Visibility.Visible;
                SqlConnection updateSqlConnection = new SqlConnection(connectionString);
                string updateSql = "UPDATE CHEF.RequestQueue " + "SET RequestStatus = @requestStatus " + "WHERE QueueID = @QueueID";
                SqlCommand UpdateSqlCommand = new SqlCommand(updateSql, updateSqlConnection);
                UpdateSqlCommand.Parameters.Add("@requestStatus", SqlDbType.Int, 5, "RequestStatus");
                UpdateSqlCommand.Parameters.Add("@QueueID", SqlDbType.Int, 6, "QueueID");
                UpdateSqlCommand.Parameters["@requestStatus"].Value = 2;
                UpdateSqlCommand.Parameters["@QueueID"].Value = QueueId;
                updateSqlConnection.Open();
                UpdateSqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Package Creation Failed." + ex.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SqlConnection updateSqlConnection = new SqlConnection(connectionString);
                string updateSql = "UPDATE CHEF.RequestQueue " + "SET RequestStatus = @requestStatus " + "WHERE QueueID = @QueueID";
                SqlCommand UpdateSqlCommand = new SqlCommand(updateSql, updateSqlConnection);
                UpdateSqlCommand.Parameters.Add("@requestStatus", SqlDbType.Int, 5, "RequestStatus");
                UpdateSqlCommand.Parameters.Add("@QueueID", SqlDbType.Int, 6, "QueueID");
                UpdateSqlCommand.Parameters["@requestStatus"].Value = 4;
                UpdateSqlCommand.Parameters["@QueueID"].Value = QueueId;
                updateSqlConnection.Open();
                UpdateSqlCommand.ExecuteNonQuery();
                
            }
            finally
            {
                ProgressBar1.Visibility = Visibility.Hidden;
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }
        private string ReadMetaDataFromFile(string metaDataPath)
        {
            string xmlMetaDataPath = string.Empty;
            string strMetaDataXML = string.Empty;
            int processID = 0;
            XElement root;
            string processName = string.Empty;
            string strSQLQuery = string.Empty;
            string strDeleteMetaData = string.Empty;
            SqlConnection cn =null;

            SqlCommand cm=null;
            //try
            //{
            //    //Set the current directory.
            //    if (!System.IO.Directory.Exists(packagePath))
            //        System.IO.Directory.CreateDirectory(packagePath);
            //    Directory.SetCurrentDirectory(packagePath);
            //}
            //catch (DirectoryNotFoundException ex)
            //{
            //    throw new Exception(string.Format("The specified Packages Path {0} does not exist. Please check the Admin Unitility config file. {1}", packagePath, ex));
            //}

            try
            {
                cn = new SqlConnection(connectionString);
                cm = cn.CreateCommand();
                xmlMetaDataPath = metaDataPath;
                if (cn.State == ConnectionState.Closed)
                    cn.Open();
                //Reading Metadata XML form file
                root = XElement.Load(xmlMetaDataPath);
                strMetaDataXML = root.ToString();
                processID = Convert.ToInt32(root.Element("Process").Attribute("ID").Value);
                processName = root.Element("Process").Attribute("Name").Value;
               
                //Delete metadata if process already exist!
                strDeleteMetaData = "Delete from CHEF.MetaData where ProcessID=" + processID;
                cm.CommandText = strDeleteMetaData;
                cm.CommandType = CommandType.Text;
                cm.ExecuteNonQuery();

                //Insert new Metadata
                strSQLQuery = @"INSERT INTO CHEF.Metadata ([ProcessID] ,[ProcessName] ,[Type] ,[Metadata]) VALUES (" + processID + ",'" + processName + "',0,@MetaData)";

                using (SqlCommand cmd = new SqlCommand(strSQLQuery, cn))
                {
                    cmd.Parameters.Add("@MetaData", SqlDbType.Xml);
                    cmd.Parameters[0].Value = strMetaDataXML;
                    int i = cmd.ExecuteNonQuery();
                }
                //Q the Request.
                cm.CommandText = "CHEF.InsertRequestQueue";
                cm.CommandType = CommandType.StoredProcedure;
                cm.Parameters.AddWithValue("@ProcessID", processID);
                cm.Parameters.AddWithValue("@RequestStatus", 1);
                cm.ExecuteNonQuery();

                SqlConnection selectSqlConnection = new SqlConnection(connectionString);
                string selecteSql = "select max(QueueID) as QueueID from chef.RequestQueue" + " where ProcessID= @processId";
                SqlCommand selectSqlCommand = new SqlCommand(selecteSql, selectSqlConnection);
                selectSqlCommand.Parameters.Add("@processId", SqlDbType.Int, 6, "ProcessID");
                selectSqlCommand.Parameters["@processId"].Value = processID;

                selectSqlConnection.Open();
                SqlDataReader selectDataReader = selectSqlCommand.ExecuteReader();

                while (selectDataReader.Read())
                {
                    QueueId = Convert.ToInt32(selectDataReader["QueueID"]);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                cn.Close();
                cm.Dispose();
            }

            return (processID + "_" + processName.Trim());
        }
        private void comboBoxPackages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            buttonViewLog.Visibility = Visibility.Hidden;
            e.Handled = true;
        }

        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
            tabControl1.SelectedIndex = 2;
        }

        private void TabItem_GotFocus(object sender, RoutedEventArgs e)
        {
            if (QueueId > 0)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("Year", typeof(System.Int32));
                dt.Columns.Add("Month", typeof(System.Int32));
                dt.Columns.Add("ETL Task", typeof(System.String));
                dt.Columns.Add("Process Start Date", typeof(System.String));
                dt.Columns.Add("Process End Date", typeof(System.String));
                dt.Columns.Add("Process Status", typeof(System.String));
                dt.Columns.Add("Source Rows", typeof(System.Int32));
                dt.Columns.Add("Target Rows", typeof(System.Int32));
                dt.Columns.Add("Description", typeof(System.String));
                dt.Columns.Add("User Name", typeof(System.String));

                string connectionString = ConfigurationManager.ConnectionStrings["CHEF"].ToString();
                SqlConnection selectSqlConnection = new SqlConnection(connectionString);
                string selecteSql = "SELECT * FROM CHEF.DataLoadStatus(null,null,(SELECT MAX(QueueID) FROM CHEF.Log)) ORDER BY StartLogID";
                SqlCommand selectSqlCommand = new SqlCommand(selecteSql, selectSqlConnection);
                selectSqlConnection.Open();
                SqlDataReader dr = selectSqlCommand.ExecuteReader();

                //SqlDataAdapter selectDataAdapter = new SqlDataAdapter(selecteSql, selectSqlConnection);
                if (dr != null)
                {
                    while (dr.Read())
                    {
                        DataRow row = dt.NewRow();
                        row["Year"] = dr["CalendarYear"];
                        row["Month"] = dr["CalendarMonth"];
                        row["ETL Task"] = dr["ETLTask"];

                        row["Process Start Date"] = dr["ProcessStartDate"];
                        row["Process End Date"] = dr["ProcessEndDate"];
                        row["Process Status"] = dr["ProcessStatus"];
                        row["Source Rows"] = dr["SourceRows"];
                        row["Target Rows"] = dr["TargetRows"];
                        row["Description"] = dr["Description"];
                        row["User Name"] = dr["UserName"];
                        dt.Rows.Add(row);
                    }
                }
                dataGridLogDetails.ItemsSource = dt.DefaultView;
                
            }
        }

        private void buttonCreationLog_Click(object sender, RoutedEventArgs e)
        {
            tabControl1.SelectedIndex = 2;
        }

        private void buttonEncrypt_Click(object sender, RoutedEventArgs e)
        {
            tabControl1.SelectedIndex = 3;
            ConnectionEncryption conEncrypt = new ConnectionEncryption();
            textBoxDPassword.Text= conEncrypt.EncryptString(textBoxPassword.Text);
        }
        
        
        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            TabItem tabitem = (TabItem)tabControl1.SelectedItem;
            if (tabitem.Header.ToString() == "Package")
            {
                //packagePath = ConfigurationManager.AppSettings["PackagesPath"].ToString();

            //    //try
            //    //{
            //    //    //Set the current directory.
            //    //    if (!System.IO.Directory.Exists(packagePath))
            //    //        System.IO.Directory.CreateDirectory(packagePath);
            //    //    Directory.SetCurrentDirectory(packagePath);
            //    //}
            //    //catch (DirectoryNotFoundException ex)
            //    //{
            //    //    throw new Exception(string.Format("The specified Packages Path {0} does not exist. Please check the Admin Unitility config file. {1}", packagePath, ex));
            //    //}

            //    //int i = System.IO.Directory.GetFiles(packagePath).Count();
            //    comboBoxPackages.Items.Clear();
            //    if (comboBoxPackages.Items.Count != i)
            //    {
            //        foreach (string s in System.IO.Directory.GetFiles(packagePath))
            //        {
            //            if (s.ToUpper().IndexOf(".DTSX") > 0)
            //            {
            //                string selectedPackage = System.IO.Path.GetFileName(s);
            //                string processId = string.Empty;
            //                string processName = string.Empty;
            //                int startIndex = 0;
            //                int length = 0;

            //                selectedPackage = selectedPackage.Replace(".dtsx", "");
            //                startIndex = selectedPackage.LastIndexOf("_");

            //                if (startIndex > 0)
            //                {
            //                    length = (selectedPackage.Length - 1) - (startIndex);
            //                    processId = selectedPackage.Substring(startIndex + 1, length);
            //                    processName = selectedPackage.Substring(0, startIndex);
            //                    comboBoxPackages.Items.Add(new KeyValuePair(processId, processName));
            //                }
            //            }
            //        }
            //    }

                string serverName = ConfigurationManager.ConnectionStrings["CHEF"].ConnectionString.Split(';')[1];
                int index = serverName.IndexOf('=');
                serverName = serverName.Substring(index + 1);
                Server server = new Server(serverName);
                IntegrationServices integrationServices = new IntegrationServices(server);
                Catalog catalog = integrationServices.Catalogs["SSISDB"];
                CatalogFolder catalogFolder = null;
                if (catalog == null)
                {
                    //Throw exception if catalog doesn't exist
                    throw new Exception("Catalog not found. Please create the SSISDB catalog and try again.");
                }
                else
                {
                    catalogFolder = catalog.Folders["CHEFFolder"];
                    if (catalogFolder == null)
                    {
                        //Create catalog folder if it doesn't exist
                        catalogFolder = new CatalogFolder(catalog, "CHEFFolder", "This is the folder which contains all projects generated through CHEF");
                        catalogFolder.Create();
                    }
                }

                foreach (ProjectInfo projectInfo in catalogFolder.Projects)
                {
                    comboBoxPackages.Items.Add(new KeyValuePair(projectInfo.Name.Substring(0, projectInfo.Name.IndexOf('_')), projectInfo.Name.Substring(projectInfo.Name.IndexOf('_')+1)));
                }         
            }
            else
            {
                comboBoxPackages.Items.Clear();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //themes.ItemsSource = ThemeManager.GetThemes();
        }
        private void themes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                string theme = e.AddedItems[0].ToString();

                // Window Level
                // this.ApplyTheme(theme);
                // Application Level
                // Application.Current.ApplyTheme(theme);
            }
        }

        private void textBoxMetaDataFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            buttonCreationLog.Visibility = Visibility.Hidden;
        }

        private void textBoxPassword_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void buttonDecrypt_Click(object sender, RoutedEventArgs e)
        {
            ConnectionEncryption conDecrypt = new ConnectionEncryption();
            labelEnPassword.Content = conDecrypt.DecryptString(textBoxDPassword.Text);
        }
    }

    public class KeyValuePair
    {
        public object Key;
        public string Value;

        public KeyValuePair(object NewValue, string NewDescription)
        {
            Key = NewValue;
            Value = NewDescription;
        }

        public override string ToString()
        {
            return Value;
        }
    }

}
