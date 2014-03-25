using BookSleeve;
using StackExchange.Profiling;
using SteamShared;
using SteamStatus.Models;
using System;
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

            var numServersTask = redis.Strings.GetInt64( 10, "steamstatus:num_servers" );

            var model = new HomeIndexViewModel();

            using ( MiniProfiler.Current.Step( "Get hashes for servers" ) )
            {
                Dictionary<string, byte[]> serverInfoDictionary = redis.Wait( redis.Hashes.GetAll( 10, "steamstatus:servers" ) );

                var serverInfos = serverInfoDictionary
                    .Select( kvp => ServerInfo.DeserializeFromBytes( kvp.Value ) )
                    .OrderBy( s => s.Server );

                model.Servers.AddRange( serverInfos );
            }

            using ( MiniProfiler.Current.Step( "Get num servers" ) )
            {
                model.NumServers = (int)redis.Wait( numServersTask );
            }

            return View( model );
        }

        RedisConnection GetRedis()
        {
            return HttpContext.Application[ "RedisConnection" ] as RedisConnection;
        }

    }
}
