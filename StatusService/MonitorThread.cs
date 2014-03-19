using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StatusService
{
    class MonitorThread
    {
        static MonitorThread _instance = new MonitorThread();
        public static MonitorThread Instance { get { return _instance; } }


        Thread monitorThread;
        bool monitorRunning;


        MonitorThread()
        {
            monitorThread = new Thread( MonitorLoop );
        }


        public void Start()
        {
            monitorRunning = true;
            monitorThread.Start();
        }


        public void Stop()
        {
            monitorRunning = false;
            monitorThread.Join();
        }


        void MonitorLoop()
        {
            SteamMonitor.Instance.Start();

            while ( true )
            {
                if ( !monitorRunning )
                    break;

                SteamMonitor.Instance.Tick();
            }
        }
    }
}
