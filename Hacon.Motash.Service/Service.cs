using System;
using System.ServiceProcess;

namespace Hacon.Motash.Service
{
    /// <summary>
    /// The main service class, uses a timer to periodically call Check
    /// </summary>
    public partial class MotashService : ServiceBase
    {
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
                // one hour
                return 1000 * 60 * 60;
            }
        }

        /// <summary>
        /// Executes when the service starts, initializes the timer
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
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
            // create a new Checker object
            Checker chk = new Checker();
            // set the last checked time
            chk.LastCheck = _lastCheck;
            // remember the new last checked time, which is now
            _lastCheck = DateTime.Now;
            // check for failed tasks
            chk.Check();
            chk.Notify();
        }

        public MotashService()
        {
            InitializeComponent();
        }

        protected override void OnStop()
        {
            if (_checkTimer != null)
            {
                _checkTimer.Dispose();
            }
        }
    }
}
