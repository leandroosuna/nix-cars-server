using Microsoft.Xna.Framework;
using Riptide.Utils;
using Riptide;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace nix_cars_server
{

    class Program
    {
        public static Server Server;
        static NetworkManager networkManager;

        public static uint ServerTPS;
        public static JObject CFG;


        private delegate void TimerCallback(uint id, uint msg, IntPtr user, IntPtr param1, IntPtr param2);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern uint timeSetEvent(uint msDelay, uint msResolution, TimerCallback callback, IntPtr user, uint eventType);

        private static uint timerId;
        static uint TargetMS;
        static void Main(string[] args)
        {
            CFG = JObject.Parse(File.ReadAllText("app-settings.json"));

            //RiptideLogger.Initialize(Console.WriteLine, true);
            RiptideLogger.Initialize(LogDebug, LogInfo, LogWarning, LogError, false);
            networkManager = new NetworkManager();
            networkManager.Init();
            Server = new Server();
            Server.Start(7777, 30);
            Server.ClientConnected += (s, e) => NetworkManager.HandleConnect(e.Client.Id);
            Server.ClientDisconnected += (s, e) => NetworkManager.HandleDisconnect(e.Client.Id);

            if (!CFG.ContainsKey("ServerTPS"))
            {
                CFG["ServerTPS"] = (uint)200;

                File.WriteAllText("app-settings.json", CFG.ToString());
            }
            ServerTPS = CFG["ServerTPS"].Value<uint>();
            TargetMS = (1000 / ServerTPS);

            TimerCallback callback = TimerElapsed;

            timerId = timeSetEvent(TargetMS, 0, callback, IntPtr.Zero, 1);
            Console.WriteLine("Server started. Press Enter to exit.");
            Console.ReadLine();



        }
        private static void TimerElapsed(uint id, uint msg, IntPtr user, IntPtr param1, IntPtr param2)
        {
            ServerUpdate();
        }

        static int t = 0;
        static DateTime lastDt = DateTime.UtcNow;
        private static void ServerUpdate()
        {
            Server.Update();

            networkManager.Update();

            t++;
            if (t == ServerTPS) //1 sec 
            {
                t = 0;
                var dt = DateTime.UtcNow.AddHours(-3);
                var diff = dt - lastDt;
                var tps = 1000 / diff.TotalMilliseconds * ServerTPS;
                var perc = 100000 / diff.TotalMilliseconds;

                //Console.WriteLine($"{dt:HH:mm:ss.fff} [TPS = {tps:###.##} : {perc:###.##}%]" );
                Console.WriteLine($"{dt:HH:mm:ss} [Server TPS = {Math.Round(tps)} : {Math.Round(perc)}%]");

                lastDt = dt;

                //Console.Clear();
                //networkManager.ShowStatus();
                networkManager.ShowPacketCount();
                networkManager.ClearPacketCount();

            }
        }
        static void LogWarning(string msg)
        {
            Console.WriteLine("WARN " + msg);
        }
        static void LogDebug(string msg)
        {
            Console.WriteLine("DEBUG " + msg);
        }
        static void LogInfo(string msg)
        {
            Console.WriteLine("INFO " + msg);
        }
        static void LogError(string msg)
        {
            Console.WriteLine("ERROR " + msg);
        }
    }
}