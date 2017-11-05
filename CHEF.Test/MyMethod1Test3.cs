using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.IntegrationServices;
using CHEFWrapper;

namespace CHEF2._0Alpha_Test
{
    /// <summary>
    /// Summary description for MyMethod1Test3
    /// </summary>
    [TestClass]
    public class MyMethod1Test3
    {
        public MyMethod1Test3()
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
        public void CheckCatalogFolderTest3()
        {
            string folderName = "NotTheCHEFFolder"; // TODO: Initialize to an appropriate value
            Server server = new Server("localhost");
            IntegrationServices integrationServices = new IntegrationServices(server);
            Catalog catalog = integrationServices.Catalogs["SSISDB"];
            CatalogFolder expected; // TODO: Initialize to an appropriate value
            CatalogFolder actual;
            if (catalog.Folders[folderName] != null)
                catalog.Folders[folderName].Drop();
            actual = Program_Accessor.CheckCatalogFolder(folderName);
            expected = catalog.Folders[folderName];
            Assert.AreEqual(expected.FolderId, actual.FolderId);
        }
    }
}
