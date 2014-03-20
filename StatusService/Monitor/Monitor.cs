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
        IPEndPoint server;

        SteamClient steamClient;
        SteamMonitorUser steamUser;
        CallbackManager callbackMgr;

        DateTime nextConnect;
        bool disconnecting;


        public Monitor( IPEndPoint server )
        {
            this.server = server;

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

                steamClient.Connect( server );
            }
        }


        void OnConnected( SteamClient.ConnectedCallback callback )
        {
            if ( callback.Result != EResult.OK )
            {
                Console.WriteLine( "{0} unable to connect to Steam: {1}", server, callback.Result );

                // todo: notify that this CM is unavailable

                return;
            }
            
            Console.WriteLine( "{0} connected to Steam!", server );

            steamUser.LogOn();
        }

        void OnDisconnected( SteamClient.DisconnectedCallback callback )
        {
            Console.WriteLine( "{0} disconnected from Steam!", server );

            if ( disconnecting )
            {
                // we're disconnecting this monitor, so we don't want to try to reconnect
                return;
            }

            Connect( DateTime.Now + TimeSpan.FromSeconds( 10 ) );
        }

        void OnLoggedOn( SteamUser.LoggedOnCallback callback )
        {
            Console.WriteLine( "{0} Logged on to Steam: {1}", server, callback.Result );

            if ( callback.Result != EResult.OK )
            {
                // todo: notify that this CM is unavailable

                return;
            }
        }

        void OnLoggedOff( SteamUser.LoggedOffCallback callback )
        {
            Console.WriteLine( "{0} logged off Steam: {0}", server, callback.Result );

            // notify that this CM is unavailable
        }

        void OnCMList( SteamClient.CMListCallback callback )
        {
            SteamMonitor.Instance.UpdateCMList( callback.Servers );
        }
    }
}
