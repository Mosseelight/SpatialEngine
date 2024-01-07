using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Riptide;

//engine stuff
using static SpatialEngine.Globals;
using static SpatialEngine.Rendering.MeshUtils;
using System.Numerics;
using JoltPhysicsSharp;
using Riptide.Transports;

namespace SpatialEngine.Networking
{
    //client and server
    //updates get sent here and sent out here
    //Client Server arch:
    /*
        client sends input updates to server and server will apply that update
        server can also be host
        host computer will auto update its self and send that update to all clients
        
        every frame:
        check for stuff from clients (input)
        if there is input
            run the update based on that input (enum with switch?)
        run host update
        send all update to client (position of things, rotation of things)
    
    */
    
    public class SpatialServer
    {
        public static Server server;
        public static Thread serverThread;

        public ushort port;
        public string ip;
        public int maxConnections { get; protected set; }

        bool stopping;


        public SpatialServer(string ip, ushort port = 58301, int maxConnections = 10)
        {
            this.ip = ip;
            this.port = port;
            this.maxConnections = maxConnections;
            Message.InstancesPerPeer = 100;
        }

        public void Start()
        {
            server = new Server();
            serverThread = new Thread(Update);
            server.ClientConnected += ClientConnected;
            server.MessageReceived += handleMessage;
            server.Start(port, (ushort)maxConnections, 0, false);
            serverThread.Start();
        }

        public void Update()
        {
            while(true)
            {
                if(stopping)
                    return;
                server.Update();
            }
        }

        public void ClientConnected(object sender, EventArgs e)
        {

        }

        public Connection[] GetServerConnections()
        {
            return server.Clients;
        }

        public void SendUnrelib(Packet packet, ushort clientId)
        {
            Message msgUnrelib = Message.Create(MessageSendMode.Unreliable, packet.GetPacketType());
            msgUnrelib.AddBytes(packet.ConvertToByte());
            server.Send(msgUnrelib, clientId);
        }

        public void SendRelib(Packet packet, ushort clientId)
        {
            Message msgRelib = Message.Create(MessageSendMode.Reliable, packet.GetPacketType());
            msgRelib.AddBytes(packet.ConvertToByte());
            server.Send(msgRelib, clientId);
        }

        public void SendUnrelibAll(Packet packet)
        {
            Message msgUnrelib = Message.Create(MessageSendMode.Unreliable, packet.GetPacketType());
            msgUnrelib.AddBytes(packet.ConvertToByte());
            server.SendToAll(msgUnrelib);
        }

        public void SendRelibAll(Packet packet)
        {
            Message msgRelib = Message.Create(MessageSendMode.Reliable, packet.GetPacketType());
            msgRelib.AddBytes(packet.ConvertToByte());
            server.SendToAll(msgRelib);
        }

        public void handleMessage(object sender, MessageReceivedEventArgs e)
        {
            HandlePacketServer(e.Message.GetBytes(), e.FromConnection);
        }

        public void Close()
        {
            stopping = true;
            serverThread.Interrupt();
            server.Stop();
            server = null;
            serverThread = null;
        }

        //Handles packets that come from the client

        void HandlePacketServer(byte[] data, Connection client)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(stream);
            //packet type
            ushort type = reader.ReadUInt16();

            switch (type)
            {
                case (ushort)PacketType.Ping:
                    {

                        break;
                    }
                case (ushort)PacketType.Connect:
                    {
                        ConnectReturnPacket packet = new ConnectReturnPacket();
                        SendRelib(packet, client.Id);
                        break;
                    }
                case (ushort)PacketType.SpatialObject:
                    {
                        SpatialObjectPacket packet = new SpatialObjectPacket();
                        packet.ByteToPacket(data);
                        if (packet.id >= scene.SpatialObjects.Count)
                            break;
                        scene.SpatialObjects[packet.id].SO_rigidbody.SetPosition((Double3)packet.Position);
                        scene.SpatialObjects[packet.id].SO_rigidbody.SetRotation(packet.Rotation);
                        stream.Close();
                        reader.Close();
                        break;
                    }
                case (ushort)PacketType.SpawnSpatialObject:
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