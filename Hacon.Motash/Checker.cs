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
    /// <summary>
    /// Class to check the tasks
    /// </summary>
    public class Checker
    {
        #region Properties and members
        // store tasks problems in the string
        StringBuilder _tasks = new StringBuilder();
        // cound our problem
        int _problems = 0;

        /// <summary>
        /// Use this instead of a hardcoded string
        /// </summary>
        public string AppName
        {
            get
            {
                return "Motash";
            }
        }

        /// <summary>
        /// Constructor, sets the LastCheck time to 1970
        /// </summary>
        public Checker()
        {
            LastCheck = new DateTime(1970, 1, 1);
        }

        public DateTime LastCheck;

        /// <summary>
        /// Number of problems found
        /// </summary>
        public int ProblemCount
        {
            get
            {
                return _problems;
            }
        }

        /// <summary>
        /// The problems as text
        /// </summary>
        public string ProblemText
        {
            get
            {
                return _tasks.ToString();
            }
        }

        /// <summary>
        /// The email entry, may be configerable in the future.
        /// </summary>
        public string EmailIntro
        {
            get
            {
                return "The following task(s) executed with an unexpected return value" + Environment.NewLine
                     + "--------------------------------------------------------------" + Environment.NewLine;
            }
        }

        string _rootFolderPattern = string.Empty;
        /// <summary>
        /// The regular expression to match the top level folder against
        /// </summary>
        /// <remarks>By default this comes from the app.config</remarks>
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

        /// <summary>
        /// Runs the actual checks, sets the Problem count and string
        /// </summary>
        /// <returns></returns>
        public int Check()
        {
            // reset our two global counters
            _problems = 0;
            _tasks.Remove(0, _tasks.Length);

            // check for the correct setup, if one of the checks fails, exit right away.
            if (!IsAdmin()) return SetProblem(AppName + " does not run as an administrator. This is required");
            if (Environment.OSVersion.Version.Major < 6) return SetProblem("Windows Vista or newer is required");
            if (!IsServiceRunning()) return SetProblem("The Task Scheduler service is not running");
            if (RootFolderPattern == "") return SetProblem("No RootFolderPattern set, check your config file.");

            try
            {
                // get an instance on the COM based Task Scheduler object
                ts.TaskScheduler ts = new ts.TaskScheduler();
                ts.Connect();
                ts.ITaskFolder root = ts.GetFolder("\\");

                // loop through the root
                foreach (ts.ITaskFolder level1 in root.GetFolders(0))
                {
                    // any folder must match our pattern, if the pattern is '.' it matches everything
                    if (Regex.IsMatch(level1.Name, RootFolderPattern, RegexOptions.IgnoreCase))
                    {
                        // go two levels down, this could be done recursively, but who nests
                        // their tasks that deep.
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

            // return the number of problems found
            return _problems;
        }

        /// <summary>
        /// Send a problem report by email
        /// </summary>
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

                if (mm.Result != "")
                {
                    Lib.Exceptions.Log(mm.Result);
                }
            }
        }

        #region Private Helper
        private int SetProblem(string problem)
        {
            _tasks.AppendLine("Setup Problem detected:");
            _tasks.AppendLine("=======================");
            _tasks.AppendLine("");
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

        /// <summary>
        /// Checks tasks in one folder
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="path"></param>
        private void CheckTasks(ts.TaskScheduler ts, string path)
        {
            TaskScheduler.IRegisteredTaskCollection tasksInFolder = ts.GetFolder(path).GetTasks(0);
            foreach (ts.IRegisteredTask task in tasksInFolder)
            {
                try
                {
                    // if task is disabled, ignore it.
                    if (task.State == TaskScheduler._TASK_STATE.TASK_STATE_DISABLED)
                    {
                        continue;
                    }
                    // if tasks is currently running, ignore it.
                    if (task.State == TaskScheduler._TASK_STATE.TASK_STATE_RUNNING)
                    {
                        continue;
                    }

                    // if the task ran last before our last check, we already checked it
                    // so ignore it this time
                    if (task.LastRunTime < LastCheck)
                    {
                        continue;
                    }

                    // get a list of allowed exit codes for this tasks, if no custom ones found
                    // only 0 is allowed
                    List<int> allowedResultCodes = GetAllowedResults(task.Definition.RegistrationInfo.Description + "");

                    // check whether we have an exit code that is not allowed
                    if (!allowedResultCodes.Contains(task.LastTaskResult))
                    {
                        // add the string and problem count
                        _tasks.AppendLine(path + @"\" + task.Name + " (" + task.LastTaskResult.ToString() + ") " + task.LastRunTime.ToString("dd MMM yyyy HH:mm:ss"));
                        _problems++;
                    }
                    
                }
                catch (Exception ex)
                {
                    // in any error case, also add a problem
                    Exceptions.Log(ex);
                    _tasks.AppendLine(ex.Message);
                    _problems++;
                }
            }
        } 

        /// <summary>
        /// Gets a list of allowed exit codes
        /// </summary>
        /// <param name="description">The string to parse</param>
        /// <returns>We are looking a curly brackets with just comma separated integers in between.</returns>
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