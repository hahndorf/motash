using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Hacon.Lib;

namespace Hacon.Motash.Tray
{

	public class SystemTray : System.Windows.Forms.Form
	{
		private System.Windows.Forms.NotifyIcon WSNotifyIcon;
        private System.ComponentModel.IContainer components;

		private Icon mDirIcon = 
			new Icon(typeof(SystemTray).Assembly.GetManifestResourceStream
            ("Hacon.Motash.Tray.app.ico"));

        private System.Threading.Timer _checkTimer;

        /// <summary>
        /// Remember the last time we checked
        /// </summary>
        private DateTime _lastCheck;

        /// <summary>
        /// The wait time between checks, could come from app.config in the future
        /// </summary>
        private int WaitTime
        {
            get
            {
                // ten minutes
                return 1000 * 60 * 10;
            }
        }

		public SystemTray()
		{
			//constructor for the form
			InitializeComponent();

			//keep the form hidden
			this.Hide();
            WSNotifyIcon.Icon = mDirIcon;
            WSNotifyIcon.Text = "Monitoring scheduled tasks";
            WSNotifyIcon.Visible = true;

            //Create the MenuItem objects and add them to
            //the context menu of the NotifyIcon.
            MenuItem[] mnuItems = new MenuItem[1];

            //create the menu items array
            mnuItems[0] = new MenuItem("Quit monitoring", new EventHandler(this.ExitControlForm));
            mnuItems[0].DefaultItem = true;

            //add the menu items to the context menu of the NotifyIcon
            ContextMenu notifyIconMenu = new ContextMenu(mnuItems);
            WSNotifyIcon.ContextMenu = notifyIconMenu;

            // creates a timer call back to be called whenever the timer is ready to go
            System.Threading.TimerCallback tc = new System.Threading.TimerCallback(OnCheckTimerEvent);

            // set the last checked time into the past to catch tasks that ran recently
            // say 60 minutes
            _lastCheck = DateTime.Now.AddMinutes(-60);
            // the timer itself
            _checkTimer = new System.Threading.Timer(tc, null, 0, WaitTime);

		}

        private void OnCheckTimerEvent(object state)
        {
            CheckTasks();
        }

        private void CheckTasks()
        {
            // create a new Checker object
            Checker chk = new Checker();
            // set the last checked time
            chk.LastCheck = _lastCheck;
            // remember the new last checked time, which is now
            _lastCheck = DateTime.Now;
            // check for failed tasks
            int problems = chk.Check();
            // if we have any email the report          

            if (problems > 0)
            {
                string title = problems.ToString() + " tasks failed";

                PlaySound(Config.GetApplicationSettingValue("RingTone", ""), false);
                if (problems == 1)
                {
                    title = "One task failed";
                }

                string body = string.Empty;

                foreach (var failure in chk.Failures)
                {
                    body += failure.Path + System.Environment.NewLine;
                }

                if (chk.SetupProblem)
                {
                    // somethings wrong 
                    title = "Somethings not right!";
                    body = chk.ProblemText;
                }

                WSNotifyIcon.BalloonTipTitle = title;
                WSNotifyIcon.BalloonTipText = body;
                WSNotifyIcon.ShowBalloonTip(1000);
            }
        }

        public void ExitControlForm(object sender, EventArgs e)
        {
            //Hide the NotifyIcon.
            WSNotifyIcon.Visible = false;

            if (_checkTimer != null)
            {
                _checkTimer.Dispose();
            }

            this.Close();
        }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

        internal static void PlaySound(string ringToneFile, bool showMessageBoxForProblems)
        {

            // convert things like %windir% into the real value
            ringToneFile = System.Environment.ExpandEnvironmentVariables(ringToneFile);

            if (ringToneFile != "")
            {
                // fix for Windows NT6 (Vista)
                // It's a hack for sure.
                if (!System.IO.File.Exists(ringToneFile))
                {
                    // they renamed some builtin sound files, if we are
                    // in the Windows Media Directory, try again adding
                    // Windows infront of the name
                    string windowsDir = System.Environment.ExpandEnvironmentVariables("%windir%");
                    if (ringToneFile.Contains(windowsDir))
                    {
                        string altRingToneFile = Path.GetDirectoryName(ringToneFile)
                            + Path.DirectorySeparatorChar
                            + "Windows " + Path.GetFileName(ringToneFile);
                        if (System.IO.File.Exists(altRingToneFile))
                        {
                            // if the alternate file exists, use it
                            ringToneFile = altRingToneFile;
                        }
                    }
                }

                if (System.IO.File.Exists(ringToneFile))
                {
                    try
                    {
                        System.Media.SoundPlayer simpleSound = new System.Media.SoundPlayer(ringToneFile);
                        simpleSound.Play();

                    }
                    catch (Exception ex)
                    {
                        Exceptions.Log(ex);
                    }
                }
                else
                {
                    Exceptions.Log("Ring tone file not found");
                }
            }
        }

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SystemTray));
            this.WSNotifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.SuspendLayout();
            // 
            // WSNotifyIcon
            // 
            this.WSNotifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("WSNotifyIcon.Icon")));
            this.WSNotifyIcon.Visible = true;
            // 
            // SystemTray
            // 
            this.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.ControlBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SystemTray";
            this.Opacity = 0D;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
            Application.Run(new SystemTray());
		}	
	}
}