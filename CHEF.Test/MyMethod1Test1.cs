using CHEFWrapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.SqlServer.Management.IntegrationServices;
using CHEFEngine;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Dts.Runtime;
using System.Data.SqlClient;
using System.Data;
using System.Xml;
using System.IO;
using System.Data.SqlTypes;

namespace CHEF2._0Alpha_Test
{


    /// <summary>
    ///This is a test class for ProgramTest and is intended
    ///to contain all ProgramTest Unit Tests
    ///</summary>
    [TestClass()]
    public class MyMethod1Test1
    {


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
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for CheckCatalogFolder
        ///</summary>
        [TestMethod()]
        [DeploymentItem("CHEF.exe")]
        public void CheckCatalogFolderTest1()
        {
            string folderName = "CHEFFolder"; // TODO: Initialize to an appropriate value
            Server server = new Server("localhost");
            IntegrationServices integrationServices = new IntegrationServices(server);
            Catalog catalog = integrationServices.Catalogs["SSISDB"];
            CatalogFolder expected = catalog.Folders[folderName]; // TODO: Initialize to an appropriate value
            CatalogFolder actual;
            actual = Program_Accessor.CheckCatalogFolder(folderName);
            Assert.AreEqual(expected.FolderId, actual.FolderId);
        }        
    }
}
