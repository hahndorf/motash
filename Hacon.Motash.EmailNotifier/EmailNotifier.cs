using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Hacon.Lib;

namespace Hacon.Motash
{
    [Export(typeof(Motash.INotifier))]
    public class EmailNotifier : Motash.INotifier
    {
        /// <summary>
        /// The email entry, may be configurable in the future.
        /// </summary>
        private string EmailIntro
        {
            get
            {
                return "The following task(s) executed with an unexpected return value" + Environment.NewLine
                     + "--------------------------------------------------------------" + Environment.NewLine;
            }
        }

        /// <summary>
        /// Send a problem report by email
        /// </summary>
        public void Send(List<Failure> failures)
        {
            if (failures.Count > 0)
            {
                MailMessage mm = new MailMessage();
                mm.Subject = "Failed tasks on " + RuntimeEnvironment.ServerName;
                mm.Body = EmailIntro + Checker.FailuresAsText(failures);
                mm.UseHtml = false;
                mm.AddRecipient(Config.GetApplicationSettingValue("AlertRecipient", ""), "");
                mm.Send();

                if (mm.Result != "")
                {
                    Console.WriteLine(mm.Result);
                    Lib.Exceptions.Log(mm.Result);
                }
            }
        }
    }
}