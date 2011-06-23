using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Principal;
using System.ServiceProcess;
using System.Collections.Generic;
using ts = TaskScheduler;
using Hacon.Lib;

namespace Hacon.Motash
{
    public class Checker
    {
        #region Properties and members
        StringBuilder _tasks = new StringBuilder();
        int _problems = 0;

        public Checker()
        {
            LastCheck = new DateTime(1970, 1, 1);
        }

        public DateTime LastCheck;

        public int ProblemCount
        {
            get
            {
                return _problems;
            }
        }

        public string ProblemText
        {
            get
            {
                return _tasks.ToString();
            }
        }

        public string EmailIntro
        {
            get
            {
                return "The following task(s) executed with a return value not 0" + Environment.NewLine
                     + "---------------------------------------------------------" + Environment.NewLine;
            }
        }

        string _rootFolderPattern = string.Empty;
        public string RootFolderPattern
        {
            get
            {
                if (_rootFolderPattern == "")
                {
                    _rootFolderPattern = Config.GetApplicationSettingValue("RootFolderPattern", "");
                }
                return _rootFolderPattern;
            }
            set
            {
                _rootFolderPattern = value;
            }
        } 
        #endregion

        public int Check()
        {
            _problems = 0;
            _tasks.Remove(0, _tasks.Length);

      //      if (!IsAdmin()) return SetProblem("No administrator");
            if (Environment.OSVersion.Version.Major < 6) return SetProblem("Windows Vista or newer is required");
            if (!IsServiceRunning()) return SetProblem("Task Scheduler service is not running");
            if (RootFolderPattern == "") return SetProblem("No RootFolderPattern set");

            try
            {
                ts.TaskScheduler ts = new ts.TaskScheduler();
                //ts.Connect(RuntimeEnvironment.ServerName);
                // for local machine, we don't need to be an admin
                ts.Connect();
                ts.ITaskFolder root = ts.GetFolder("\\");

                foreach (ts.ITaskFolder level1 in root.GetFolders(0))
                {
                    if (Regex.IsMatch(level1.Name, RootFolderPattern, RegexOptions.IgnoreCase))
                    {
                        string path1 = "\\" + level1.Name;
                        CheckTasks(ts, path1);

                        ts.ITaskFolder folder1 = ts.GetFolder(path1);
                        foreach (ts.ITaskFolder level2 in folder1.GetFolders(0))
                        {
                            string path2 = "\\" + level2.Name;
                            CheckTasks(ts, path1 + path2);

                            ts.ITaskFolder folder2 = ts.GetFolder(path1 + path2);
                            foreach (ts.ITaskFolder level3 in folder2.GetFolders(0))
                            {
                                CheckTasks(ts, path1 + path2 + "\\" + level3.Name);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Exceptions.Log(ex);
                _tasks.AppendLine(ex.Message);
                _problems++;
            }
            return _problems;
        }

        public void EmailReport()
        {
            if (_problems > 0)
            {
                MailMessage mm = new MailMessage();
                mm.Subject = "Failed tasks on " + RuntimeEnvironment.ServerName;
                mm.Body = EmailIntro + _tasks.ToString();
                mm.UseHtml = false;
                mm.AddRecipient(Config.GetApplicationSettingValue("AlertRecipient",""),"");
                mm.Send();
            }
        }

        #region Private Helper
        private int SetProblem(string problem)
        {
            _tasks.AppendLine(problem);
            _problems = 1;
            return 1;
        }

        private bool IsAdmin()
        {
            WindowsPrincipal user = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return user.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private bool IsServiceRunning()
        {
            ServiceController sc = new ServiceController("Schedule");
            if (sc.Status == ServiceControllerStatus.Running)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void CheckTasks(ts.TaskScheduler ts, string path)
        {
            TaskScheduler.IRegisteredTaskCollection tasksInFolder = ts.GetFolder(path).GetTasks(0);

            foreach (ts.IRegisteredTask task in tasksInFolder)
            {
                try
                {
                    if (task.State != TaskScheduler._TASK_STATE.TASK_STATE_DISABLED)
                    {
                        if (task.State != TaskScheduler._TASK_STATE.TASK_STATE_RUNNING)
                        {
                        //    Console.WriteLine(task.Name);
                         //   Console.WriteLine(task.Definition.RegistrationInfo.Description + "");

                           List<int> allowedResultCodes = GetAllowedResults(task.Definition.RegistrationInfo.Description + "");

                            if (task.LastRunTime >= LastCheck)
                            {
                                if (!allowedResultCodes.Contains(task.LastTaskResult))
                            //    if (task.LastTaskResult != 0)
                                {
                                    _tasks.AppendLine(path + @"\" + task.Name + " (" + task.LastTaskResult.ToString() + ") " + task.LastRunTime.ToString("dd MMM yyyy HH:mm:ss"));
                                    _problems++;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Exceptions.Log(ex);
                    _tasks.AppendLine(ex.Message);
                    _problems++;
                }
            }
        } 

        private List<int> GetAllowedResults(string description)
        {
            List<int> results = new List<int>();

            RegexOptions options = RegexOptions.None;
            Regex rxFind = new Regex(@"{[0-9,]+}", options);
            Regex rxSplit = new Regex(@",", options);

            MatchCollection matches = rxFind.Matches(description);
            if (matches.Count == 1)
            {
                string IDs = matches[0].Value.Replace("{", "").Replace("}", "");
                string[] ids = rxSplit.Split(IDs);
                foreach (string id in ids)
                {
                    int rc = Lib.UserInput.ToInt32(id);
                    if (!results.Contains(rc))
                    {
                        results.Add(rc);
                    }
                }
            }
            else
            {
                results.Add(0);
            }

            return results;
        }

        #endregion
    }
}