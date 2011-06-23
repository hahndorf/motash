using System;
using System.ServiceProcess;

namespace Hacon.Motash.Service
{
    public partial class MotashService : ServiceBase
    {
        private System.Threading.Timer _checkTimer;
        private DateTime _lastCheck;

        public MotashService()
        {
            InitializeComponent();
        }

        private int WaitTime
        {
            get
            {
                // one hour
                return 1000 * 60 * 60;
            }
        }

        protected override void OnStart(string[] args)
        {
            System.Threading.TimerCallback tc = new System.Threading.TimerCallback(OnCheckTimerEvent);
            _checkTimer = new System.Threading.Timer(tc, null, 3000, WaitTime);
            _lastCheck = DateTime.Now.AddSeconds(-WaitTime); 
        }

        protected override void OnStop()
        {
            if (_checkTimer != null)
            {
                _checkTimer.Dispose();
            }
        }

        private void OnCheckTimerEvent(object state)
        {
            Checker chk = new Checker();
            chk.LastCheck = _lastCheck;
            _lastCheck = DateTime.Now;
            int problems = chk.Check();
            if (problems > 0)
            {
                chk.EmailReport();
            }            
        }

    }
}
