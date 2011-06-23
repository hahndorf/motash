using System;
using System.Web;
using System.Net.Mail;
using System.Configuration;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Hacon.Lib
{
    /// <summary>
    /// Used to send SMTP messages
    /// </summary>
    /// <remarks>If the MasterEmail address is set in *.config the message will be
    /// redirected to that address and some properties are changed in the Send() method
    /// to reflect the real values used.</remarks>
    /// <example>The most basic example:
    /// <code>
    /// MailMessage message = new MailMessage();
    /// message.AddRecipient("user@foo.bar", "Test User");
    /// message.Subject = "My Subject";
    /// message.Body =  "My Body";
    /// message.Send();
    /// 
    /// if (message.Result != "")
    /// {
    ///     // handle the problem
    /// }
    /// </code>
    /// </example>
    public class MailMessage
    {
        System.Net.Mail.MailMessage _mailMessage = new System.Net.Mail.MailMessage();
        SmtpClient _smtpClient = new SmtpClient();
        // string _problems = string.Empty;
        List<string> _problems = new List<string>();
        private string _noEmailHost = "SendNoEmails.testing.test";

        /// <summary>
        /// Create an instance of this class to send an SMTP message
        /// </summary>
        public MailMessage()
        {
            Host = _smtpClient.Host;

            // set defaults
            LogExceptions = true;
            SenderAddress = Config.SenderEmail;
            SenderName = "";
            Subject = "";
            Body = "";
        }


        #region Private Helpers

        private void AddProblem(string problem)
        {
            if (!_problems.Contains(problem))
            {
                _problems.Add(problem);
            }
        }

        private string CleanAddress(string address)
        {
            return Regex.Replace(address, @"\s", "");
        }
        #endregion

        #region Properties
        /// <summary>
        /// The email subject line, required.
        /// </summary>
        public string Subject { get; set; }
        /// <summary>
        /// The body of the email, required
        /// </summary>
        public string Body { get; set; }
        /// <summary>
        /// The display of the sender, not required.
        /// </summary>
        public string SenderName { get; set; }
        /// <summary>
        /// The email address of the send, required, if not set the value inc(tn)SenderEmail from *.config is used.
        /// </summary>
        public string SenderAddress { get; set; }

        /// <summary>
        /// The SMTP server, taken from System.Net.Mail in *.config
        /// </summary>
        public string Host { get; private set; }

        bool _useHtml = false;
        /// <summary>
        /// Set to true if the body is html, default is false
        /// </summary>
        public bool UseHtml
        {
            get
            {
                return _useHtml;
            }
            set
            {
                _useHtml = value;
                _mailMessage.IsBodyHtml = value;
            }
        }
        /// <summary>
        /// If false any exceptions are not logged, should only be used by the exceptions class.
        /// </summary>
        public bool LogExceptions { get; set; }

        /// <summary>
        /// Empty string if everything worked, otherwise a message listing the problems.
        /// </summary>
        public string Result
        {
            get
            {
                string result = string.Empty;

                foreach (string problem in _problems)
                {
                    result += problem + " ";
                }

                return result;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a recipient
        /// </summary>
        /// <param name="mailAddress">The email address</param>
        /// <param name="displayName">The display name of the recipient, can be empty</param>
        public void AddRecipient(string mailAddress, string displayName)
        {
            mailAddress = CleanAddress(mailAddress);


                if (displayName == "")
                {
                    _mailMessage.To.Add(new MailAddress(mailAddress));
                }
                else
                {
                    _mailMessage.To.Add(new MailAddress(mailAddress, displayName));
                }

        }


        /// <summary>
        /// Sends the message, check Result property for success
        /// </summary>
        public void Send()
        {
            if (!IsValid()) return;

            if (SenderName == "")
            {
                _mailMessage.From = new MailAddress(SenderAddress);
            }
            else
            {
                _mailMessage.From = new MailAddress(SenderAddress, SenderName);
            }

            string body = Body;

            if (UseHtml)
            {
                if (Body.IndexOf("<html") == -1)
                {
                    // add some HTML around the body
                    body = HTMLWrapper(Body);
                }

                System.Net.Mime.ContentType htmlType = new System.Net.Mime.ContentType("text/html");
                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(body, htmlType);
                _mailMessage.AlternateViews.Add(htmlView);
            }

            string problem = string.Empty;

 
            _mailMessage.Body = body;
            _mailMessage.Subject = Subject;
            _mailMessage.Priority = MailPriority.Normal;

            if (Config.CurrentDebugLevel == Config.DebugLevels.Debug)
            {
                string debugFileName = "email";
                if (UseHtml)
                {
                    debugFileName += ".html";
                }
                else
                {
                    debugFileName += ".txt";
                }
                IOHelper.SaveDebugTextFile(debugFileName, this.ToString());
            }

            if (_smtpClient.Host == _noEmailHost)
            {
                // don't send any email
                return;
            }
            // we have a special host name, specified in
            // the config of unit testing
            // only if that's not set do we actually want to send the message
            try
            {
                _smtpClient.Send(_mailMessage);
            }
            catch (System.Net.Mail.SmtpException eSmtp)
            {
                if (LogExceptions)
                {
                    string innerExceptionMessage = string.Empty;
                    if (eSmtp.InnerException != null)
                    {
                        innerExceptionMessage = eSmtp.InnerException.Message;
                    }
                    Exceptions.Log(eSmtp, innerExceptionMessage + " " + this.ToString(), (int)Exceptions.LogTargets.File);
                }
                AddProblem("SmtpException: " + eSmtp.Message);
            }

            catch (Exception ex)
            {
                if (LogExceptions)
                {
                    Exceptions.Log(ex, this.ToString(), (int)Exceptions.LogTargets.File);
                }
                if (Config.CurrentDebugLevel > Config.DebugLevels.Stage)
                {
                    AddProblem("System Exception while sending email: " + ex.Message + " " + this.ToString());
                }
                else
                {
                    AddProblem("System Exception while sending email: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Returns all meta and body data for the message
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine("Recipients: " + _mailMessage.To.Count + " " + _mailMessage.To.ToString() + "#");
            info.AppendLine("Subject: " + Subject + "#");
            info.AppendLine("SenderName: " + SenderName + "#");
            info.AppendLine("SenderAddress: " + SenderAddress + "#");
            info.AppendLine("UseHTML: " + UseHtml.ToString() + "#");
            info.AppendLine("CC Recipients: " + _mailMessage.CC.ToString() + "#");
            info.AppendLine("Bcc Recipients: " + _mailMessage.Bcc.ToString() + "#");
            info.AppendLine("SMTPHost: " + Host + "#");
            info.AppendLine("Body: " + Body);

            return info.ToString();
        }

        #endregion

        /// <summary>
        /// Checks the data of the instance and returns true if valid for sending a message
        /// If returning false, the result property has the details.
        /// </summary>
        /// <returns>True if all data is valid and we are ready for sending the message</returns>
        public bool IsValid()
        {
            string problem = string.Empty;

            if (SenderAddress == "")
            {
                AddProblem("Sender not specified.");
            }
            if (Subject == "")
            {
                AddProblem("The subject is empty.");
            }
            if (Body == "")
            {
                AddProblem("The body is empty. ");
            }
            if (Host == "")
            {
                AddProblem("No host specified.");
            }

            if (_mailMessage.To.Count == 0)
            {
                AddProblem("No recipients were specified.");
            }

            if (!IsValidAddress(SenderAddress, true))
            {
                AddProblem("The sender address is not valid.");
            }


            if (_problems.Count == 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public static bool IsValidAddress(string address, bool allowNameSyntax)
        {

            // test for a valid e-mail format, see below for criteria
            // optional parameter allows to check for mutiple addresses separated 
            // by the specified character.

            bool isValid = false;
            try
            {
                if (address == null)
                {
                    return false;
                }
                else
                {
                    string fullAddressPattern;
                    string singleAddressPattern;

                    // this matches a single address
                    singleAddressPattern = "[-a-z0-9\\._&]+@[-a-z0-9\\._]{2,}\\.[a-z]{2,10}";

                    // a single address only
                    // ^ marks the beginning and $ the end of the string
                    if (allowNameSyntax)
                    {
                        // allow the 'John Doe <john@doe.com>' syntax
                        //                          fullAddressPattern = "(?:^" + singleAddressPattern + "$)|(?:^[-a-z0-9\\._ ',&]+<" + singleAddressPattern + ">$)";
                        // allow all kind of stuff in the name, but no more than 100 characters
                        fullAddressPattern = "(?:^" + singleAddressPattern + "$)|(?:^.{1,100}<" + singleAddressPattern + ">$)";
                    }
                    else
                    {
                        fullAddressPattern = "^" + singleAddressPattern + "$";
                    }

                    if (Regex.IsMatch(address, fullAddressPattern, RegexOptions.IgnoreCase))
                    {
                        isValid = true;
                    }
                    else
                    {
                        isValid = false;
                    }
                }
            }
            catch (Exception ex)
            {
                // in case of error we log it an return false
                Exceptions.Log(ex);
                isValid = false;
            }

            return isValid;
        }

        #region Html Wrapper
        /// <summary>
        /// Puts the basic page html tags around some text
        /// </summary>
        /// <param name="BodyText">The text to be wrapped</param>
        /// <param name="StyleTag">A style tag to be used in the header tag</param>
        /// <returns>A basic html string</returns>
        private static string HTMLWrapper(string BodyText, string StyleTag)
        {
            string HTML = "<html><head>";
            HTML += StyleTag + Environment.NewLine;
            HTML += "</head><body>" + Environment.NewLine;
            HTML += BodyText + Environment.NewLine;
            HTML += "</body></html>" + Environment.NewLine;

            return HTML;
        }

        /// <summary>
        /// Adds a basic style tag to a string
        /// </summary>
        /// <param name="BodyText">the text</param>
        /// <returns>The text now with a style tag</returns>
        private static string HTMLWrapper(string BodyText)
        {
            string Style = "<style>*{font-family:sans-serif;}</style>";

            return HTMLWrapper(BodyText, Style);

        }
        #endregion
    }
}
