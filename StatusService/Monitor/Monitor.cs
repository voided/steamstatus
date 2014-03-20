using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StatusService
{
    class Monitor : BaseMonitor
    {
        DateTime nextNotify = DateTime.MaxValue;


        public Monitor( IPEndPoint server )
            : base( server )
        {
        }


        protected override void OnConnected( SteamClient.ConnectedCallback callback )
        {
            if ( callback.Result != EResult.OK )
            {
                SteamMonitor.Instance.NotifyCMOffline( this, callback.Result );
                return;
            }

            base.OnConnected( callback );
        }

        protected override void OnDisconnected( SteamClient.DisconnectedCallback callback )
        {
            base.OnDisconnected( callback );

            if ( !IsDisconnecting )
            {
                // if we're not forcibly disconnecting this instance, notify that the CM is offline
                SteamMonitor.Instance.NotifyCMOffline( this );
            }
        }

        protected override void OnLoggedOn( SteamUser.LoggedOnCallback callback )
        {
            base.OnLoggedOn( callback );

            if ( callback.Result != EResult.OK )
            {
                SteamMonitor.Instance.NotifyCMOffline( this, callback.Result );
                return;
            }

            SteamMonitor.Instance.NotifyCMOnline( this );

            nextNotify = DateTime.Now + TimeSpan.FromMinutes( 1 );
        }

        protected override void OnLoggedOff( SteamUser.LoggedOffCallback callback )
        {
            base.OnLoggedOff( callback );

            SteamMonitor.Instance.NotifyCMOffline( this, callback.Result );
        }


        protected override void Tick()
        {
            // run callbacks
            base.Tick();

            if ( DateTime.Now >= nextNotify )
            {
                if ( Client.IsConnected )
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
    }

    class MasterMonitor : BaseMonitor
    {
        public MasterMonitor()
            : base( null )
        {
        }

        protected override void Tick()
        {
            base.Tick();
        }
    }

    class BaseMonitor
    {
        public IPEndPoint Server { get; private set; }

        public bool IsDisconnecting { get; private set; }


        protected SteamClient Client { get; private set; }


        SteamMonitorUser steamUser;
        CallbackManager callbackMgr;

        DateTime nextConnect = DateTime.MaxValue;


        public BaseMonitor( IPEndPoint server )
        {
            Server = server;

            Client = new SteamClient();

            steamUser = new SteamMonitorUser();
            Client.AddHandler( steamUser );

            callbackMgr = new CallbackManager( Client );

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
            IsDisconnecting = true;
            Client.Disconnect();
        }


        public void DoTick()
        {
            this.Tick();
        }

        protected virtual void Tick()
        {
            // we'll check for callbacks every 10ms
            // thread quantum granularity might hose us,
            // but it should wake often enough to handle callbacks within a single thread

            callbackMgr.RunWaitCallbacks( TimeSpan.FromMilliseconds( 10 ) );

            if ( DateTime.Now >= nextConnect )
            {
                nextConnect = DateTime.MaxValue;

                Client.Connect( Server );
            }
        }


        protected virtual void OnConnected( SteamClient.ConnectedCallback callback )
        {
            steamUser.LogOn();
        }

        protected virtual void OnDisconnected( SteamClient.DisconnectedCallback callback )
        {
            if ( IsDisconnecting )
            {
                // we're disconnecting this monitor, so we don't want to try to reconnect
                // or notify that the CM we're connecting to is down
                return;
            }

            // schedule a reconnect in 10 seconds
            Connect( DateTime.Now + TimeSpan.FromSeconds( 10 ) );
        }

        protected virtual void OnLoggedOn( SteamUser.LoggedOnCallback callback )
        {
        }

        protected virtual void OnLoggedOff( SteamUser.LoggedOffCallback callback )
        {
        }

        protected virtual void OnCMList( SteamClient.CMListCallback callback )
        {
            SteamMonitor.Instance.UpdateCMList( callback.Servers );
        }
    }
}
