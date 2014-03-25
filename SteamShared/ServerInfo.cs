using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SteamShared
{
    [ProtoContract]
    public class ServerInfo
    {
        [ProtoMember( 1 )]
        public string Server { get; set; }

        [ProtoMember( 2 )]
        public bool IsOnline { get; set; }

        [ProtoMember( 3 )]
        public string Result { get; set; }

        [ProtoMember( 4 )]
        public string Host { get; set; }


        public void Serialize( Stream stream )
        {
            Serializer.Serialize( stream, this );
        }
        public byte[] SerializeToBytes()
        {
            using ( var ms = new MemoryStream() )
            {
                Serialize( ms );
                return ms.ToArray();
            }
        }

        public static ServerInfo Deserialize( Stream stream )
        {
            return Serializer.Deserialize<ServerInfo>( stream );
        }

        public static ServerInfo DeserializeFromBytes( byte[] input )
        {
            using ( var ms = new MemoryStream( input ) )
            {
                return Deserialize( ms );
            }
        }
    }
}
