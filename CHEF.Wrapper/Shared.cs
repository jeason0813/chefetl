using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;


namespace CHEFWrapper
{
    
    class Shared
    {
    }
    public static class DatabaseConnection
    {

        public static string CHEF
        {
            get
            {
                return System.Configuration.ConfigurationManager.ConnectionStrings["CHEF"].ConnectionString;
            }
        }
      
    }
}