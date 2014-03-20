using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StatusService
{
    class Monitor
    {
        public IPEndPoint Server { get; private set; }


        SteamClient steamClient;
        SteamMonitorUser steamUser;
        CallbackManager callbackMgr;

        DateTime nextConnect = DateTime.MaxValue;
        DateTime nextNotify = DateTime.MaxValue;

        bool disconnecting;


        public Monitor( IPEndPoint server )
        {
            Server = server;

            steamClient = new SteamClient();

            steamUser = new SteamMonitorUser();
            steamClient.AddHandler( steamUser );

            callbackMgr = new CallbackManager( steamClient );

            new Callback<SteamClient.ConnectedCallback>( OnConnected, callbackMgr );
            new Callback<SteamClient.DisconnectedCallback>( OnDisconnected, callbackMgr );

            new Callback<SteamUser.LoggedOnCallback>( OnLoggedOn, callbackMgr );
            new Callback<SteamUser.LoggedOffCallback>( OnLoggedOff, callbackMgr );

            new Callback<SteamClient.CMListCallback>( OnCMList, callbackMgr );
        }


        public void Connect( DateTime? when = null )
        {
            if ( when == null )
                when = DateTime.Now;

            nextConnect = when.Value;
        }
        public void Disconnect()
        {
            disconnecting = true;
            steamClient.Disconnect();
        }


        public void Tick()
        {
            // we'll check for callbacks every 10ms
            // thread quantum granularity might hose us,
            // but it should wake often enough to handle callbacks within a single thread

            callbackMgr.RunWaitCallbacks( TimeSpan.FromMilliseconds( 10 ) );

            if ( DateTime.Now >= nextConnect )
            {
                nextConnect = DateTime.MaxValue;

                steamClient.Connect( Server );
            }

            if ( DateTime.Now >= nextNotify )
            {
                if ( steamClient.IsConnected )
                {
                    SteamMonitor.Instance.NotifyCMOnline( this );
                }
                else
                {
                    SteamMonitor.Instance.NotifyCMOffline( this );
                }

                nextNotify = DateTime.Now + TimeSpan.FromMinutes( 1 );
            }
        }


        void OnConnected( SteamClient.ConnectedCallback callback )
        {
            if ( callback.Result != EResult.OK )
            {
                SteamMonitor.Instance.NotifyCMOffline( this, callback.Result );
                return;
            }

            steamUser.LogOn();
        }

        void OnDisconnected( SteamClient.DisconnectedCallback callback )
        {
            if ( disconnecting )
            {
                // we're disconnecting this monitor, so we don't want to try to reconnect
                // or notify that the CM we're connecting to is down
                return;
            }

            // schedule a reconnect in 10 seconds
            Connect( DateTime.Now + TimeSpan.FromSeconds( 10 ) );

            SteamMonitor.Instance.NotifyCMOffline( this );
        }

        void OnLoggedOn( SteamUser.LoggedOnCallback callback )
        {
            if ( callback.Result != EResult.OK )
            {
                SteamMonitor.Instance.NotifyCMOffline( this, callback.Result );
                return;
            }

            SteamMonitor.Instance.NotifyCMOnline( this );

            nextNotify = DateTime.Now + TimeSpan.FromMinutes( 1 );
        }

        void OnLoggedOff( SteamUser.LoggedOffCallback callback )
        {
            SteamMonitor.Instance.NotifyCMOffline( this, callback.Result );
        }

        void OnCMList( SteamClient.CMListCallback callback )
        {
            SteamMonitor.Instance.UpdateCMList( callback.Servers );
        }
    }
}
