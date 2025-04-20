using Microsoft.Xna.Framework;
using Riptide;

namespace nix_cars_server
{
    public class Player
    {
        public uint id;
        public string name;
        public ushort netId;
        public short RTT;

        public Vector3 position = Vector3.Zero;
        public Vector2 horizontalVelocity = Vector2.Zero;

        public float yaw;
        public float pitch;
        
        public bool connected;
        public bool connectedMessageSent;
        public bool disconnectedMessageSent;
        
        public uint lastProcessedMesage;

        public uint outboundPackets = 0;
        public bool lastMovementValid = false;

        
        public Player(uint id)
        {
            this.id = id;
            name = "noname";
            connectedMessageSent = false;
            disconnectedMessageSent = false;

        }
    }
}
