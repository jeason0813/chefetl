using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.IntegrationServices;
using Microsoft.SqlServer.Dts.Runtime;
using CHEFWrapper;

namespace CHEF2._0Alpha_Test
{
    /// <summary>
    /// Summary description for MyMethod2Test2
    /// </summary>
    [TestClass]
    public class MyMethod2Test2
    {
        public MyMethod2Test2()
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
        public void CreateProjectAndDeployTest2()
        {
            string serverName = ConfigurationManager.ConnectionStrings["CHEF"].ConnectionString.Split(';')[1];
            int index = serverName.IndexOf('=');
            serverName = serverName.Substring(index + 1);
            Server server = new Server(serverName);
            IntegrationServices integrationServices = new IntegrationServices(server);
            Catalog catalog = integrationServices.Catalogs["SSISDB"];
            CatalogFolder catalogFolder = catalog.Folders["NotTheCHEFFolder"];
            if (catalogFolder==null)
            {
                catalogFolder = new CatalogFolder(catalog, "NotTheCHEFFolder", "Not the CHEF folder.");
                catalogFolder.Create();
            }
            string strProjectLocation = @"C:\Program Files\Microsoft\CHEF\Temp";
            Package package = null;
            Application app = new Application();
            package = app.LoadPackage(@"C:\Users\t-satsen\Documents\Visual Studio 2010\Projects\Package_DatabaseToDatabase\BasicFeaturesPackage\bin\Debug\TableToTableDataTransferPkg.dtsx", null);
            Program_Accessor.processID = "100";
            Program_Accessor.processName = "TestTask";
            if (catalogFolder.Projects[Program_Accessor.processID + "_" + Program_Accessor.processName] != null)
                catalogFolder.Projects[Program_Accessor.processID + "_" + Program_Accessor.processName].Drop();
            Program_Accessor.CreateProjectAndDeploy(catalogFolder, strProjectLocation, package);
            Assert.IsNull(catalogFolder.Projects[Program_Accessor.processID + "_" + Program_Accessor.processName]);
        }
    }
}
