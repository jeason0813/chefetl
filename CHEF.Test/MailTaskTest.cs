using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;
using CHEFWrapper;
using System.Configuration;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.IntegrationServices;
using System.Data;
using System.Threading;

namespace CHEF2._0Alpha_Test
{
    /// <summary>
    /// Summary description for MailTaskTest
    /// </summary>
    [TestClass]
    public class MailTaskTest
    {
        public MailTaskTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestMailTask()
        {
            string[] args = { };
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["CHEF"].ConnectionString);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandText = "DELETE FROM [Chef].[Log]"
                                + " WHERE [Log].[QueueID] IN"
                                + " (SELECT QueueID FROM [CHEF].[RequestQueue]"
                                + " WHERE ProcessID = 9200)";
            cmd.ExecuteNonQuery();
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandText = "DELETE FROM [Chef].[RequestQueue]"
                                + " WHERE ProcessID = 9200";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "DELETE FROM [Chef].[MetaData]"
                                + " WHERE ProcessID = 9200";
            cmd.ExecuteNonQuery();
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandText = "INSERT INTO [CHEF].[Metadata] VALUES "
                                                + "(9200"
                                                + ",'MailTaskTest'"
                                                + ",'<CHEFMetaData ApplicationName=\"TestApplication\">"
                                                + "     <Process ID=\"9200\" Name=\"DemoMailTask\" DefaultAllowTruncate=\"False\" VerboseLogging=\"False\" ExecuteExistingPackage=\"False\" >"
                                                + "         <ConnectionSet>"
                                                + "             <SMTPConnection key=\"SMTPConnection\" SmtpServer=\"mail.messaging.microsoft.com\" UseWindowsAuthentication=\"True\" EnableSsl=\"False\" />"
                                                + "         </ConnectionSet>"
                                                + "         <Step ID=\"9210\" Name=\"SendMailTask Test\" TypeID=\"1\" TypeName=\"Staging\">"
                                                + "             <SendMailTask Name=\"SendMail\" SMTPServer=\"SMTPConnection\" From=\"t-satsen@microsoft.com\" To=\"t-satsen@microsoft.com\" CC=\"t-divsar@microsoft.com\" BCC=\"t-kishke@microsoft.com\" Subject=\"Test Mail: Send Mail Task\" Priority=\"Normal\" MessageSourceType=\"DirectInput\" MessageSource=\"Hi, this is mail task test. Please ignore.\">"
                                                + "                 <DataFlow Name=\"Populate Sales Currency\" SourceName=\"[Sales].[Currency]\" TargetName=\"[Sales].[Currency_Copy]\" />"
                                                + "                 <Attachments FileName=\"C:\\Users\\t-satsen\\Desktop\\Note.txt\" />"
                                                + "             </SendMailTask>"
                                                + "         </Step>"
                                                + "     </Process>"
                                                + "</CHEFMetaData>'"
                                                + ",0"
                                                + ",SUSER_SNAME()"
                                                + ",SYSDATETIME()"
                                                + ",SUSER_SNAME()"
                                                + ",SYSDATETIME())";
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ProcessID", 9200);
            cmd.Parameters.AddWithValue("@StartStepID", 9210);
            cmd.Parameters.AddWithValue("@RequestStatus", 1);
            cmd.CommandText = "CHEF.InsertRequestQueue";
            cmd.ExecuteNonQuery();

            Program_Accessor.WraperCreateSSISPkg(args);

            cmd.Parameters.Clear();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "DELETE FROM [CHEF].[RequestQueue] WHERE [QueueID] = (SELECT MAX(QueueID) FROM [CHEF].[RequestQueue] WHERE [ProcessID] = 9200)";
            cmd.ExecuteNonQuery();
            
            string serverName = ConfigurationManager.ConnectionStrings["CHEF"].ConnectionString.Split(';')[1];
            int index = serverName.IndexOf('=');
            int finalStatusID = 4;
            bool testPass = true;
            serverName = serverName.Substring(index + 1);
            Server server = new Server(serverName);
            IntegrationServices integrationServices = new IntegrationServices(server);
            Microsoft.SqlServer.Management.IntegrationServices.PackageInfo packageInfo = null;
            ProjectInfo projectInfo = integrationServices.Catalogs["SSISDB"].Folders["CHEFFolder"].Projects["9200_MailTaskTest"];
            Assert.IsNotNull(projectInfo);
            packageInfo = projectInfo.Packages["Package.dtsx"];
            Assert.IsNotNull(packageInfo);
            packageInfo.Execute(false, null);
            Thread.Sleep(1000);
            cmd.Parameters.Clear();
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandText = "SELECT MAX([StatusID])"
                            + " FROM [CHEF].[Log]"
                            + " WHERE [QueueID] ="
                            + " (SELECT [QueueID]"
                            + " FROM [CHEF].[RequestQueue]"
                            + " WHERE [ProcessID] = 9200)";
            SqlDataReader sqlDataReader = cmd.ExecuteReader();
            if (sqlDataReader.HasRows)
            {
                sqlDataReader.Read();
                finalStatusID = Convert.ToInt32(sqlDataReader[0].ToString());
            }
            if (finalStatusID == 4)
            {
                testPass = false;
            }
            sqlDataReader.Close();
            if (testPass)
            {
                cmd.Parameters.Clear();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "UPDATE [CHEF].[RequestQueue]"
                                + " SET [RequestStatus] = 2"
                                + " WHERE [ProcessID] = 9200";
                cmd.ExecuteNonQuery();
            }
            else
            {
                cmd.Parameters.Clear();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "UPDATE [CHEF].[RequestQueue]"
                                + " SET [RequestStatus] = 4"
                                + " WHERE [ProcessID] = 9200";
                cmd.ExecuteNonQuery();
            }
            Assert.IsTrue(testPass); 
        }
    }
}
