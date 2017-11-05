using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;

namespace UpdateConfig
{
    [RunInstaller(true)]
    public partial class Installer : System.Configuration.Install.Installer
    {
        public Installer()
        {
            InitializeComponent();
        }
        string outputPath = string.Empty;
        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);
            try
            {
                string dbServer = Context.Parameters["dbServer"];
                string dbName = Context.Parameters["dbName"];
                outputPath = Context.Parameters["TargetDir"];
                outputPath = outputPath.Substring(0, outputPath.Length - 2);
                string login = Context.Parameters["Login"];
                string password = Context.Parameters["Password"];
                if (dbServer.Contains(@"\\"))
                {
                    dbServer = dbServer.Replace(@"\\", @"\");
                }
                UpadteConfig(dbServer, dbName, outputPath, login, password);
                //Creating Log folder

                if (!(Directory.Exists(outputPath + @"\Log")))
                {
                    Directory.CreateDirectory(outputPath + @"\Log");
                }
                string command = "SetupBuild.cmd";
                string commandDir = outputPath + @"\MSBuild";
                
                int exitCode = RunCommand(command, commandDir);
                CleanInstallFiles(outputPath);
                if (exitCode == 1)
                {
                    throw new InstallException();
                }
                
            }
            catch
            {
                throw new InstallException("CHEF Installation failed. Please find the installation Log in \n"+outputPath + @"\Log\MainBuild.log");
                
            }

        }

        private void CleanInstallFiles(string outputPath)
        {
            //try
            //{
                File.Copy(outputPath + @"\MSBuild\MainBuild.log", outputPath + @"\Log\MainBuild.log", true);
            //    //Directory.Delete(outputPath + @"\MSBuild", true);

            //    if (Directory.Exists(outputPath + @"\Database"))
            //    {
            //        Directory.Delete(outputPath + @"\Database", true);
            //    }
                
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("An Exception occured during installation. Details: " + ex.ToString());
            //}
        }

        private int RunCommand(string command, string commandDir)
        {
            Directory.SetCurrentDirectory(commandDir);
            int ExitCode;
            ProcessStartInfo ProcessInfo;
            Process Process;
            ProcessInfo = new ProcessStartInfo(command);
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = false;

            Process = Process.Start(ProcessInfo);
            Process.WaitForExit();
            ExitCode = Process.ExitCode;
            Process.Close();

            return ExitCode;

        }

        private void UpadteConfig(string dbServer, string dbName, string outputPath, string login, string password)
        {
            string pathToXml = System.IO.Path.GetFullPath(outputPath + @"\MSBuild\Scripts\Config.proj");
            XElement project = XElement.Load(pathToXml);

            foreach (XElement xE in project.Descendants())
            {
                if (xE.Name.LocalName.Contains("CHEFServer"))
                    xE.SetValue(dbServer);
                else if (xE.Name.LocalName.Contains("DBName"))
                    xE.SetValue(dbName);
                else if (xE.Name.LocalName.Contains("DBLogin"))
                    xE.SetValue(login);
                else if (xE.Name.LocalName.Contains("DBPassword"))
                    xE.SetValue(password);
                else if (xE.Name.LocalName.Contains("OutputPackagePath"))
                    xE.SetValue(outputPath);
                else if (xE.Name.LocalName.Contains("OutputLogPath"))
                    xE.SetValue(outputPath+"Log");
            }

            project.Save(pathToXml);
        }
       

    }
}
