using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Riptide;
using Riptide.Utils;
using System.Collections.Generic;
using System.Diagnostics;

namespace nix_cars_server
{
    public class NetworkManager
    {
        static List<Player> players = new List<Player>();
        
        static List<Player> playersJustJoined = new List<Player>();

        public static bool startingRace = false;
        public static string currentMode = "free";
        public void Init()
        {
            
        }
        public void Update()
        {
            CheckRTT();
            CheckPlayersJustJoined();
            BroadcastPlayerData();
        }
        static ushort countdown = 4;
        static ushort lapsMax = 4;
        static bool ignoreLaps = true;
        public void OneSecUpdate()
        {
            ShowPacketCount();
            ClearPacketCount();

            if (startingRace)
            {
                ushort sc = 0;
                if (countdown > 0)
                {
                    sc = (ushort)(countdown - 1);

                    countdown--;

                    if (countdown == 1)
                    {
                        Console.WriteLine("race started");

                        //currentMode = "race";
                        ignoreLaps = false;
                    }
                }
                else 
                {
                    sc = 1000;
                    startingRace = false;
                }

                var msg = Message.Create(MessageSendMode.Reliable, ServerToClient.RaceStartCountdown);
                msg.AddUShort(sc);

                Vector3[] array = [new Vector3(287,9, -18.7f), new Vector3(298, 9, -20), new Vector3(310, 9, -23),
                    new Vector3(287, 9, -33), new Vector3(298, 9, -36), new Vector3(310, 9, -38)];

                for (int i = array.Length - 1; i > 0; i--)
                {
                    Random r = new Random();
                    int j = (int)r.NextInt64(0,i + 1);

                    Vector3 temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                }

                if (sc == 3)
                {
                    for(int i = 0; i< players.Count; i++)
                    {
                        msg.AddUInt(players[i].id);                        
                        msg.AddVector3(array[i]);
                    }
                }

                Program.Server.SendToAll(msg);


            }

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
                    message.AddVector2(p.horizontalVelocity);

                    message.AddBool(p.f);
                    message.AddBool(p.b);
                    message.AddBool(p.l);
                    message.AddBool(p.r);
                    message.AddBool(p.boost);

                    message.AddFloat(p.progress);
                    message.AddUShort(p.currentLap);
                }
                p.outboundPackets++;

            }
            Program.Server.SendToAll(message);
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
        [MessageHandler((ushort)ClientToServer.PlayerData)]
        private static void HandlePlayerData(ushort fromClientId, Message message)
        {
            var id = message.GetUInt();
            Player p = GetPlayerFromId(id);
            p.position = message.GetVector3();
            p.yaw = message.GetFloat();
            p.pitch = message.GetFloat();
            p.horizontalVelocity = message.GetVector2();

            p.f = message.GetBool();
            p.b = message.GetBool();
            p.l = message.GetBool();
            p.r = message.GetBool();
            p.boost = message.GetBool();

            p.progress = message.GetFloat();
        }

        [MessageHandler((ushort)ClientToServer.Command)]
        private static void HandleCommand(ushort fromClientId, Message message)
        {
            var id = message.GetUInt();
            Player p = GetPlayerFromId(id);
            var command = message.GetString();
            if(p.id == 111111)
            {
                var splitted = command.Split("/");
                
                if(splitted.Length == 2)
                {
                    if (splitted[0] != "")
                    {
                        //error
                        return;
                    }
                    else
                    {
                        
                        HandleAdminCommand(fromClientId, splitted[1]);
                        //Console.WriteLine($"ADMIN: {splitted[1]}");
                    }
                }
                else
                {
                    Console.WriteLine($"{p.name}: {command}");
                    
                }
                
            }
            else
            {
                Console.WriteLine($"{p.name}: {command}");
                
            }
        }

        static void HandleAdminCommand(ushort id, string input)
        {
            var args = input.Split(" ");

            for (int i = 0; i < args.Length; i++)
                Console.Write(args[i]);

            Console.Write("\n");

            var msg = Message.Create(MessageSendMode.Reliable, ServerToClient.CommandResponse);
            msg.AddString("ok " + input);
            Program.Server.Send(msg, id);

            if (args[0] == "start")
            {
                currentMode = "race";

                players.ForEach(p => p.currentLap = 0);

                msg = Message.Create(MessageSendMode.Reliable, ServerToClient.GameModeChange);
                msg.AddString(currentMode);
                msg.AddUShort(lapsMax);
                Program.Server.SendToAll(msg);

                startingRace = true;
                countdown = 4;
                ignoreLaps = true;
            }

            if (args[0] == "stop")
            {
                players.ForEach(p => p.currentLap = 0);
                currentMode = "free";
                msg = Message.Create(MessageSendMode.Reliable, ServerToClient.GameModeChange);
                msg.AddString(currentMode);
                Program.Server.SendToAll(msg);
                ignoreLaps = true;
            }
        }

        [MessageHandler((ushort)ClientToServer.CarChange)]
        static void HandleCarChange(ushort fromClientId, Message message)
        {
            var id = message.GetUInt();
            Player p = GetPlayerFromId(id);
            var carId = message.GetUShort();
            var l = message.GetUShort();
            Vector3[] colors = new Vector3[l];
            for (int i = 0; i < l; i++)
            {
                colors[i] = message.GetVector3();
            }

            var msg = Message.Create(MessageSendMode.Reliable, ServerToClient.GameModeChange);
            msg.AddString(currentMode);
            Program.Server.Send(msg, fromClientId);

            msg = Message.Create(MessageSendMode.Reliable, ServerToClient.CarChange);
            msg.AddUInt(id);
            msg.AddUShort(carId);
            msg.AddUShort(l);
            for (int i = 0; i<l; i++)
            {
                msg.AddVector3(colors[i]);
            }

            Program.Server.SendToAll(msg, fromClientId);

        }


        [MessageHandler((ushort)ClientToServer.Lap)]
        static void HandleLap(ushort fromClientId, Message message)
        {
            var id = message.GetUInt();
            Player p = GetPlayerFromId(id);
            var forward = message.GetBool();
            
            if(currentMode == "race" && !ignoreLaps) 
            {
                if (forward)
                {
                    p.currentLap++;
                    if (p.currentLap == lapsMax + 1)
                    {
                        Message msgWin = Message.Create(MessageSendMode.Unreliable, ServerToClient.GameModeChange);
                        msgWin.AddString("free");

                        Console.WriteLine(p.name + " finished");
                        Program.Server.Send(msgWin, fromClientId);
                        return;
                    }
                    Console.WriteLine( $"{p.name} LAP {p.currentLap}");
                }
                else
                {
                    if (p.currentLap > 0)
                        p.currentLap --;

                    Console.WriteLine($"{p.name} LAP {p.currentLap}");
                }

                Message msg = Message.Create(MessageSendMode.Unreliable, ServerToClient.Lap);
                msg.AddUShort(p.currentLap);

                Program.Server.Send(msg, fromClientId);
            }

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