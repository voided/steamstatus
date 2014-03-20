using BookSleeve;
using StackExchange.Profiling;
using SteamStatus.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SteamStatus.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            var redis = GetRedis();

            string[] cmServers = MiniProfiler.Current.Inline( () => redis.Wait( redis.Sets.GetAllString( 10, "steamstatus:servers" ) ), "Get server list" );

            var serverBag = new ConcurrentBag<HomeIndexViewModel.Server>();

            using ( MiniProfiler.Current.Step( "Get hashes for servers" ) )
            {
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 10,
                };

                Parallel.ForEach( cmServers, parallelOptions, cmHost =>
                {
                    string keyName = string.Format( "steamstatus:{0}", cmHost );

                    Dictionary<string, string> serverInfo = redis.Wait( redis.Hashes.GetAll( 10, keyName ) )
                        .ToDictionary( kvp => kvp.Key, kvp => Encoding.UTF8.GetString( kvp.Value ) );

                    if ( serverInfo.Count == 0 )
                    {
                        // if we have no info entries, the key expired and we should remove it from the servers set
                        redis.Sets.Remove( 10, "steamstatus:servers", cmHost );

                        return;
                    }

                    IPEndPoint addr = AddressToEndPoint( cmHost );

                    var server = new HomeIndexViewModel.Server
                    {
                        Address = addr,
                        Status = serverInfo[ "status" ],
                    };

                    string host;
                    if ( serverInfo.TryGetValue( "host", out host ) )
                    {
                        server.Host = host;
                    }

                    string result;
                    if ( serverInfo.TryGetValue( "result", out result ) )
                    {
                        server.Result = result;
                    }

                    serverBag.Add( server );
                } );
            }

            var model = new HomeIndexViewModel();

            model.Servers.AddRange( serverBag.OrderBy( s => s.Address.ToString() ) );

            return View( model );
        }

        IPEndPoint AddressToEndPoint( string address )
        {
            string[] addressParts = address.Split( ':' );

            IPAddress ipAddr = IPAddress.Parse( addressParts[ 0 ] );
            int port = int.Parse( addressParts[ 1 ] );

            return new IPEndPoint( ipAddr, port );
        }

        RedisConnection GetRedis()
        {
            return HttpContext.Application[ "RedisConnection" ] as RedisConnection;
        }

    }
}
