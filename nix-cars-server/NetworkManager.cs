using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Riptide;
using Riptide.Utils;
using System.Collections.Generic;

namespace nix_cars_server
{
    public class NetworkManager
    {
        static List<Player> players = new List<Player>();
        
        static List<Player> playersJustJoined = new List<Player>();
        public void Init()
        {
            
        }
        public void Update()
        {
            CheckRTT();
            CheckPlayersJustJoined();
            BroadcastPlayerData();
        }
        
        void CheckRTT()
        {
            foreach(var c in Program.Server.Clients)
            {
                GetPlayerFromNetId(c.Id).RTT = c.RTT;
            }
        }
        
        void CheckPlayersJustJoined()
        {
            var othersCount = (uint)players.Count - 1;

            foreach(var p in playersJustJoined)
            {
                Message newName = Message.Create(MessageSendMode.Reliable, ServerToClient.PlayerName);
                newName.AddUInt(1);
                newName.AddUInt(p.id);
                newName.AddString(p.name);

                Program.Server.SendToAll(newName, p.netId);

                if(othersCount > 0)
                {
                    Message names = Message.Create(MessageSendMode.Reliable, ServerToClient.PlayerName);
                    names.AddUInt(othersCount);
                
                    foreach(var op in players)
                    {                    
                        if (op.id != p.id)
                        {
                            names.AddUInt(op.id);
                            names.AddString(op.name);
                        }
                    }
                    Program.Server.Send(names, p.netId);
                }
            }

            playersJustJoined.Clear();
        }
        
        [MessageHandler((ushort)ClientToServer.PlayerIdentity)]
        static void HandlePlayerIdentity(ushort fromClientId, Message message)
        {
            //Console.WriteLine("identity received");
            var playerId = message.GetUInt();
            var playerName = message.GetString();
            var version = message.GetInt();

            var current = Program.CFG["Version"].Value<int>();
            if (version != current)
            {
                Program.Server.DisconnectClient(fromClientId);
                //Console.WriteLine($"id {playerId} wrong version: {version} -> (current {current})");
                return;
            }

            Player p = GetPlayerFromId(playerId, true);
            p.name = playerName;
            p.netId = fromClientId;
            p.connected = true;

            playersJustJoined.Add(p);
        }

        bool packetloss = false;
        public void BroadcastPlayerData()
        {
            Message message = Message.Create(MessageSendMode.Unreliable, ServerToClient.AllPlayerData);
            
            message.AddUInt((uint)players.Count);
            foreach (Player p in players)
            {
                message.AddUInt(p.id);
                message.AddBool(p.connected);
                if (p.connected)
                { 
                    //message.AddUInt(p.lastProcessedMesage);
                    //message.AddBool(p.lastMovementValid);
                    message.AddVector3(p.position);
                    message.AddFloat(p.yaw);
                    message.AddFloat(p.pitch);
                    

                }
                p.outboundPackets++;

            }
            Program.Server.SendToAll(message);
        }

        
        [MessageHandler((ushort)ClientToServer.PlayerData)]
        private static void HandlePlayerData(ushort fromClientId, Message message)
        {
            var id = message.GetUInt();
            Player p = GetPlayerFromId(id);
            p.position = message.GetVector3();
            p.horizontalVelocity = message.GetVector2();
            p.yaw = message.GetFloat();
            p.pitch = message.GetFloat();

        }
        public static ClientInputState ValidateInput(ClientInputState state)
        {
            //float validDelta = 0.005f; //expected delta time
            //validDelta *= (state.Sprint ? 18f : 9.5f); //speed modifier
            //validDelta += 0.015f; //error margin

            //state.valid = true;
            //var len = state.positionDelta.Length();
            //if (len < validDelta)
            //    return state;

            //var diff = len - validDelta;
            //state.valid = false;

            //state.positionDelta.Normalize();
            //state.positionDelta *= diff;

            //state.position -= state.positionDelta;
            //return state;


            state.valid = true;
            return state;
        }
        

        

        public void ShowStatus()
        {
            var str = "Server status";
            if (Program.Server.IsRunning)
            {
                str += " ONLINE    TPS "+Program.ServerTPS + "    players online "+Program.Server.ClientCount + "/"+players.Count+" max "+Program.Server.MaxClientCount;
            }
            else
                str += " OFFLINE";
            
            Console.SetCursorPosition(0, 0);
            Console.Write(str);

            
            str = "Mode -    Map -    Time -    State - ";

            Console.SetCursorPosition(0, 1);
            Console.Write(str);
        }
        public void ShowPacketCount()
        {
            var line = 2;
            //Console.SetCursorPosition(0, 2);

            int countIn = 0;
            int countOut = 0;
            foreach(var c in Program.Server.Clients)
            {
                countIn += c.Metrics.UnreliableIn;
                countOut += c.Metrics.UnreliableOut;
            }
            //Console.Write("total IO " + countIn + " - " + countOut);
            line++;
            foreach (var c in Program.Server.Clients)
            {
                var p = GetPlayerFromNetId(c.Id);
                //Console.SetCursorPosition(0, line);
                //Console.WriteLine(player.name + " IO " + c.Metrics.UnreliableIn + " - " + player.outboundPackets + " - " + c.Metrics.UnreliableOut + " RTT " + c.RTT + " ms   lv " + player.lastMovementValid);
                Console.WriteLine($"{p.name}[{p.id}] IO {c.Metrics.UnreliableIn}-{c.Metrics.UnreliableOut}  RTT " +
                    $" {c.RTT}");
                    //+ (int)player.position.X + " " + (int)player.position.Y + " " + (int)player.position.Z);

                line++;
            }


             
        }
        public void ClearPacketCount()
        {
            foreach (var c in Program.Server.Clients)
            {
                c.Metrics.Reset();
            }
            foreach(var p in players)
            {
                p.outboundPackets = 0;
            }
        }
        public static void HandleConnect(ushort id)
        {
            Message m = Message.Create(MessageSendMode.Reliable, ServerToClient.Version);
            var v = Program.CFG["Version"].Value<int>();
            m.AddInt(v);
            Program.Server.Send(m, id);
        }
        public static void HandleDisconnect(ushort id)
        {
            var player = GetPlayerFromNetId(id);
            player.connected = false;
            //Console.WriteLine("handle disconnect");
        }

        public static Player GetPlayerFromNetId(ushort id)
        {
            foreach (var player in players)
            {
                if (player.netId == id)
                {
                    return player;
                }
            }
            return new Player(uint.MaxValue);
        }

        public static Player GetPlayerFromId(uint id, bool createIfNull = false)
        {
            foreach (var player in players)
            {
                if (player.id == id)
                {
                    return player;
                }
            }
            if (createIfNull)
            {
                Player p = new Player(id);
                players.Add(p);
                return p;
            }

            return new Player(uint.MaxValue);
        }

    }
}