using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nix_cars_server
{
    public class ClientInputState
    {
        public Vector3 position;
        public Vector3 positionDelta;
        public bool valid;
        public float yaw;
        public float pitch;
        public byte clipId;

        public bool Forward;
        public bool Backward;
        public bool Left;
        public bool Right;
        public bool Fire;
        public bool ADS;
        public bool Reload;

        public bool Jump;
        public bool Crouch;
        public bool Sprint;
        public bool Ability1;
        public bool Ability2;
        public bool Ability3;
        public bool Ability4;
        public float deltaTime = 0f;
        public float accDeltaTime = 0f;


        public uint messageId;
        public ClientInputState(bool forward, bool backward, bool left, bool right, bool fire, 
            bool ads, bool reload, bool jump, bool crouch, bool sprint, bool ability1, bool ability2, bool ability3, bool ability4)
        {
            Forward = forward;
            Backward = backward;
            Left = left;
            Right = right;
            Fire = fire;
            ADS = ads;
            Reload = reload;
            Jump = jump;
            Crouch = crouch;
            Sprint = sprint;
            Ability1 = ability1;
            Ability2 = ability2;
            Ability3 = ability3;
            Ability4 = ability4;
        }
        public ClientInputState() { }
        float speed;
        //public void ApplyInputTo(Player p)
        //{
        //    Vector3 tempFront;

        //    tempFront.X = MathF.Cos(MathHelper.ToRadians(yaw)) * MathF.Cos(MathHelper.ToRadians(pitch));
        //    tempFront.Y = MathF.Sin(MathHelper.ToRadians(pitch));
        //    tempFront.Z = MathF.Sin(MathHelper.ToRadians(yaw)) * MathF.Cos(MathHelper.ToRadians(pitch));

        //    p.frontDirection = Vector3.Normalize(tempFront);

        //    var frontFlat = Vector3.Normalize(new Vector3(p.frontDirection.X, 0, p.frontDirection.Z));
        //    var rightFlat = Vector3.Cross(Vector3.Up, frontFlat);

        //    Vector3 dir = Vector3.Zero;
        //    int dz = 0;
        //    int dx = 0;

        //    if (Forward)
        //        dz++;
        //    if (Backward)
        //        dz--;
        //    if (Left)
        //        dx++;
        //    if (Right)
        //        dx--;

        //    dir += (dz * frontFlat + dx * rightFlat);
        //    speed = 9.5f;
        //    if (dz > 0 && Sprint)
        //        speed = 18;

        //    if (dir != Vector3.Zero)
        //        dir = Vector3.Normalize(dir);

        //    p.position += dir * speed * accDeltaTime;
        //    p.clipId = 0;
        //    p.lastProcessedMesage = messageId;
        //}
    }

    public enum PlayerAnimation
    {
        idle,
        runForward,
        runForwardRight,
        runForwardLeft,
        runBackward,
        runBackwardRight,
        runBackwardLeft,
        runRight,
        runLeft,
        sprintForward,
        sprintForwardRight,
        sprintForwardLeft
    }
}
