using System;
using System.IO;
using System.Numerics;
using System.Reflection.Emit;
using JoltPhysicsSharp;
using Silk.NET.Input;

//engine stuff
using static SpatialEngine.Globals;
using static SpatialEngine.MeshUtils;

namespace SpatialEngine
{
    namespace Networking
    {
	
        public enum PacketType
        {
            ConnectPacket,
            ConnectReturn,
            SpatialObjectPacket,
            SpawnSpatialObject,      
        }
	
        public abstract class Packet
        {
            // may be better to have the handle packet here and it returns a record of the packet type i need
            // handles whatever data and returns the packet with the data

            public abstract byte[] ConvertToByte();
            public abstract void ByteToPacket(byte[] data);
        }

        public abstract class ServerPacket
        {

            public abstract byte[] ConvertToByte();
            public abstract void ByteToPacket(byte[] data);
        }

        public abstract class ClientPacket
        {

            public abstract byte[] ConvertToByte();
            public abstract void ByteToPacket(byte[] data);
        }


        //Connection packets

        public class ConnectPacket : ClientPacket
        {
            //connect is value 0
            public ConnectPacket() 
            {
                
            }

            public override byte[] ConvertToByte() 
            {
                return [0];
            }

            public override void ByteToPacket(byte[] data)
            {
                //does nothing
            }
        }

        public class ConnectReturnPacket : ServerPacket
        {
            public ConnectReturnPacket()
            {

            }

            public override byte[] ConvertToByte()
            {
                MemoryStream stream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(EngVer);
                throw new NotImplementedException();

            }

            public override void ByteToPacket(byte[] data)
            {
                //does nothing
            }
        }

        //will need more infomration then this like info about the rigidbody state and whatever
        public class SpatialObjectPacket : ServerPacket
        {
            public int id;
            public Vector3 Position;
            public Quaternion Rotation;

            public SpatialObjectPacket()
            {

            }

            public SpatialObjectPacket(int id, Vector3 position, Quaternion rotation)
            {
                this.id = id;
                this.Position = position;
                this.Rotation = rotation;
            }

            public override byte[] ConvertToByte()
            {
                MemoryStream stream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(stream);
                //type of packet
                writer.Write((ushort)PacketType.SpatialObjectPacket);
                //id
                writer.Write(id);
                //position
                writer.Write(Position.X);
                writer.Write(Position.Y);
                writer.Write(Position.Z);
                //rotation
                writer.Write(Rotation.X);
                writer.Write(Rotation.Y);
                writer.Write(Rotation.Z);
                writer.Write(Rotation.W);
                stream.Close();
                writer.Close();

                return stream.ToArray();
            }

            public override void ByteToPacket(byte[] data)
            {
                MemoryStream stream = new MemoryStream(data);
                BinaryReader reader = new BinaryReader(stream);
                int type = reader.ReadUInt16();
                id = reader.ReadInt32();
                //position
                float posX = reader.ReadSingle();
                float posY = reader.ReadSingle();
                float posZ = reader.ReadSingle();
                Position = new Vector3(posX, posY, posZ);
                //rotation
                float rotX = reader.ReadSingle();
                float rotY = reader.ReadSingle();
                float rotZ = reader.ReadSingle();
                float rotW = reader.ReadSingle();
                Rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                stream.Close();
                reader.Close();
            }
        }

        public class SpawnSpatialObjectPacket : ClientPacket
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public string ModelLocation;
            public int MotionType;
            public int ObjectLayer;
            public int Activation;

            public SpawnSpatialObjectPacket()
            {

            }

            public SpawnSpatialObjectPacket(int id, Vector3 position, Quaternion rotation, string modelLocation, MotionType motionType, ObjectLayer objectLayer, Activation activation)
            {
                this.Position = position;
                this.Rotation = rotation;
                this.ModelLocation = modelLocation;
                this.MotionType = (int)motionType;
                this.ObjectLayer = objectLayer;
                this.Activation = (int)activation;
            }

            public override byte[] ConvertToByte()
            {
                MemoryStream stream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(stream);
                //type of packet
                writer.Write((ushort)PacketType.SpawnSpatialObject);
                //id
                //writer.Write(id);
                //position
                writer.Write(Position.X);
                writer.Write(Position.Y);
                writer.Write(Position.Z);
                //rotation
                writer.Write(Rotation.X);
                writer.Write(Rotation.Y);
                writer.Write(Rotation.Z);
                writer.Write(Rotation.W);
                //modellocation
                writer.Write(ModelLocation);
                //motion type
                writer.Write(MotionType);
                //object layer
                writer.Write(ObjectLayer);
                //activation
                writer.Write(Activation);
                stream.Close();
                writer.Close();

                return stream.ToArray();
            }

            public override void ByteToPacket(byte[] data)
            {
                MemoryStream stream = new MemoryStream(data);
                BinaryReader reader = new BinaryReader(stream);
                int type = reader.ReadUInt16();
                //id = reader.ReadInt32();
                //position
                float posX = reader.ReadSingle();
                float posY = reader.ReadSingle();
                float posZ = reader.ReadSingle();
                Position = new Vector3(posX, posY, posZ);
                //rotation
                float rotX = reader.ReadSingle();
                float rotY = reader.ReadSingle();
                float rotZ = reader.ReadSingle();
                float rotW = reader.ReadSingle();
                Rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                ModelLocation = reader.ReadString();
                MotionType = reader.ReadInt32();
                ObjectLayer = reader.ReadInt32();
                Activation = reader.ReadInt32();
                stream.Close();
                reader.Close();
            }
        }


        public static class PacketHandler
        {
            public static void HandlePacket(byte[] data)
            {
                MemoryStream stream = new MemoryStream(data);
                BinaryReader reader = new BinaryReader(stream);
                //packet type
                int type = reader.ReadUInt16();

                switch (type)
                {
                    case (int)PacketType.SpatialObjectPacket:
                    {
                        SpatialObjectPacket packet = new SpatialObjectPacket();
                        packet.ByteToPacket(data);
                        scene.SpatialObjects[packet.id].SO_mesh.position = packet.Position;
                        scene.SpatialObjects[packet.id].SO_mesh.rotation = packet.Rotation;
                        stream.Close();
                        reader.Close();
                        break;
                    }
                    case (int)PacketType.SpawnSpatialObject:
                    {
                        SpawnSpatialObjectPacket packet = new SpawnSpatialObjectPacket();
                        packet.ByteToPacket(data);
                        scene.AddSpatialObject(LoadModel(packet.Position, packet.Rotation, packet.ModelLocation), (MotionType)packet.MotionType, (ObjectLayer)packet.ObjectLayer, (Activation)packet.Activation);
                        stream.Close();
                        reader.Close();
                        break;
                    }
                }
            }
        }
    }
}