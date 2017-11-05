using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace CHEFWrapper
{

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class CHEFGlobalConfig
    {

        private CHEFGlobalConfigGlobalConfiguration globalConfigurationField;
        private string applicationNameField;
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("GlobalConfiguration", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public CHEFGlobalConfigGlobalConfiguration GlobalConfiguration
        {
            get
            {
                return this.globalConfigurationField;
            }
            set
            {
                this.globalConfigurationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ApplicationName
        {
            get
            {
                return this.applicationNameField;
            }
            set
            {
                this.applicationNameField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class CHEFGlobalConfigGlobalConfiguration
    {

        private string versionField;

        private string logLocationField;

        private string installationBitLocationField;

        private string maxBatchSizeField;

        private string maxLogTableSizeField;

        private string notificationAliasField;

        private string sendNotificationField;

        private string thresholdTimeInMinutesField;

        private string outputPackageLocationField;
        private string badRowFolderLocationField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string LogLocation
        {
            get
            {
                return this.logLocationField;
            }
            set
            {
                this.logLocationField = value;
            }
        }
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string InstallationBitLocation
        {
            get
            {
                return this.installationBitLocationField;
            }
            set
            {
                this.installationBitLocationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string MaxBatchSize
        {
            get
            {
                return this.maxBatchSizeField;
            }
            set
            {
                this.maxBatchSizeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string MaxLogTableSize
        {
            get
            {
                return this.maxLogTableSizeField;
            }
            set
            {
                this.maxLogTableSizeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string NotificationAlias
        {
            get
            {
                return this.notificationAliasField;
            }
            set
            {
                this.notificationAliasField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SendNotification
        {
            get
            {
                return this.sendNotificationField;
            }
            set
            {
                this.sendNotificationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ThresholdTimeInMinutes
        {
            get
            {
                return this.thresholdTimeInMinutesField;
            }
            set
            {
                this.thresholdTimeInMinutesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string OutputPackageLocation
        {
            get
            {
                return this.outputPackageLocationField;
            }
            set
            {
                this.outputPackageLocationField = value;
            }
        }
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string BadRowFolderLocation
        {
            get
            {
                return this.badRowFolderLocationField;
            }
            set
            {
                this.badRowFolderLocationField = value;
            }
        }
    }

    
}