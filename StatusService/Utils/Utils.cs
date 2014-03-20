using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatusService
{
    static class Utils
    {
        public static Dictionary<string, byte[]> ToRedisHash( this Dictionary<string, object> input )
        {
            return input.ToDictionary( kvp => kvp.Key, kvp =>
            {
                if ( kvp.Value == null )
                    return new byte[ 0 ];

                return Encoding.UTF8.GetBytes( kvp.Value.ToString() );
            } );
        }
    }
}
