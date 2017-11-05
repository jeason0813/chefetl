using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Tasks.SendMailTask;

namespace CHEFEngine.Executables
{
    class SendEmailTaskWrapper
    {
        public string Name { get; set; }
        public string SmtpConnection { get; set; }
        public string ToLine { get; set; }
        public string CCLine { get; set; }
        public string BCCLine { get; set; }
        public string FromLine { get; set; }
        public MailPriority Priority { get; set; }
        public string FileAttachments { get; set; }
        public string Subject { get; set; }
        public bool BypassPrepare { get; set; }
        public SendMailMessageSourceType SendMailMessageSourceTypeField { get; set; }
        public string MessageSource { get; set; }
        public string SetExpression { get; set; }
        public static void AddSqlTask(Executable exe, SendEmailTaskWrapper emailTask)
        {
            TaskHost thSendMailTask = exe as TaskHost;
            thSendMailTask.Name = emailTask.Name;
            thSendMailTask.Properties["SmtpConnection"].SetValue(thSendMailTask, emailTask.SmtpConnection);
            thSendMailTask.Properties["ToLine"].SetValue(thSendMailTask, emailTask.ToLine);
            thSendMailTask.Properties["CCLine"].SetValue(thSendMailTask, emailTask.CCLine);
            thSendMailTask.Properties["BCCLine"].SetValue(thSendMailTask, emailTask.BCCLine);
            thSendMailTask.Properties["FromLine"].SetValue(thSendMailTask, emailTask.FromLine);
            thSendMailTask.Properties["Priority"].SetValue(thSendMailTask, emailTask.Priority);
            thSendMailTask.Properties["FileAttachments"].SetValue(thSendMailTask, emailTask.FileAttachments);
            thSendMailTask.Properties["Subject"].SetValue(thSendMailTask, emailTask.Subject);
            thSendMailTask.Properties["MessageSourceType"].SetValue(thSendMailTask, SendMailMessageSourceType.DirectInput);
            thSendMailTask.Properties["SmtpConnection"].SetValue(thSendMailTask, emailTask.SmtpConnection);
            thSendMailTask.Properties["MessageSourceType"].SetValue(thSendMailTask, emailTask.SendMailMessageSourceTypeField);
            thSendMailTask.Properties["MessageSource"].SetValue(thSendMailTask, emailTask.MessageSource);
            thSendMailTask.Properties["SqlStatementSource"].SetExpression(thSendMailTask, emailTask.SetExpression);
            thSendMailTask = null;
        }
    }
}
