﻿using System;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using ImGuiNET;
using Silk.NET.Core;
using Silk.NET.GLFW;


namespace SpatialEngine
{
    public class Game
    {

        static bool showWireFrame = false;
        static uint vertCount;
        static uint indCount;
        static float totalTime = 0.0f;
        public const int SCR_WIDTH = 1920;
        public const int SCR_HEIGHT = 1080;

        static ImGuiController controller;
        static IInputContext input;
        private static Vector2 LastMousePosition;
        static IWindow window;
        static GL gl;
        static Shader shader;
        static Scene scene = new Scene();
        static Camera camera;
        static readonly string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        static readonly string resourcePath = appPath + @"\res";
        static readonly string ShaderPath = resourcePath + @"/Shaders";
        static readonly string ImagePath = resourcePath + @"/Images";


        public static void Main(string[] args)
        {
            WindowOptions options = WindowOptions.Default with
            {
                Size = new Vector2D<int>(SCR_WIDTH, SCR_HEIGHT),
                Title = "GameTesting",
                VSync = false
            };
            window = Window.Create(options);
            window.Load += OnLoad;
            window.Update += OnUpdate;
            window.Render += OnRender;
            window.Run();
        }

        static unsafe void OnLoad() 
        {
            controller = new ImGuiController(gl = window.CreateOpenGL(), window, input = window.CreateInput());
            gl.ClearColor(Color.DarkCyan);

            Vertex[] vertexes =
            {
                new Vertex(new Vector3(-1.0f, -1.0f, 1.0f),new Vector3(0), new Vector2(0)),
                new Vertex(new Vector3(-1.0f, 1.0f, 1.0f),new Vector3(0), new Vector2(0)),
                new Vertex(new Vector3(-1.0f, -1.0f, -1.0f),new Vector3(0), new Vector2(0)),
                new Vertex(new Vector3(-1.0f, 1.0f, -1.0f),new Vector3(0), new Vector2(0)),
                new Vertex(new Vector3( 1.0f,-1.0f, 1.0f),new Vector3(0), new Vector2(0)),
                new Vertex(new Vector3(1.0f,1.0f, 1.0f),new Vector3(0), new Vector2(0)),
                new Vertex(new Vector3(1.0f,-1.0f, -1.0f),new Vector3(0), new Vector2(0)),
                new Vertex(new Vector3(1.0f,1.0f, -1.0f),new Vector3(0), new Vector2(0))
            };

            uint[] indices =
            {
                1, 2, 0,
                3, 6, 2,
                7, 4, 6,
                5, 0, 4,
                6, 0, 2,
                3, 5, 7,
                1, 3, 2,
                3, 7, 6,
                7, 5, 4,
                5, 1, 0,
                6, 4, 0,
                3, 1, 5
            };
            
            scene.AddSpatialObject(new Mesh(gl, vertexes, indices));
            for (int i = 0; i < scene.SpatialObjects.Count; i++)
            {
                vertCount += (uint)scene.SpatialObjects[i].SO_mesh.vertexes.Length;
                indCount += (uint)scene.SpatialObjects[i].SO_mesh.indices.Length;
            }
            camera = new Camera(new Vector3(0,0,-2), Quaternion.Identity, Vector3.Zero, 45f);
            shader = new Shader(gl, ShaderPath + @"\Default.vert", ShaderPath + @"\Default.frag");

            ImGui.SetWindowSize(new Vector2(400,600));

            //input stuffs
            for (int i = 0; i < input.Keyboards.Count; i++)
                input.Keyboards[i].KeyDown += KeyDown;
            for (int i = 0; i < input.Mice.Count; i++)
            {
                input.Mice[i].Cursor.CursorMode = CursorMode.Normal;
                input.Mice[i].MouseMove += OnMouseMove;
            }
        }

        static void KeyDown(IKeyboard keyboard, Key key, int keyCode)
        {
            
        }

        static unsafe void OnMouseMove(IMouse mouse, Vector2 position)
        {
            if (LastMousePosition == default) { LastMousePosition = position; }
            else
            {
                
            }
        }

        static void OnUpdate(double dt) 
        {
            totalTime += (float)dt;
            for (int i = 0; i < scene.SpatialObjects.Count; i++)
            {
                scene.SpatialObjects[i].SO_mesh.SetModelMatrix();
            }
        }

        static unsafe void OnRender(double dt)
        {   
            controller.Update((float)dt);

            ImGuiMenu(dt);

            gl.Enable(EnableCap.DepthTest);
            gl.Clear((uint) (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
            gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
            if(showWireFrame)
                gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Line);

            gl.UseProgram(shader.shader);
            shader.SetUniform("uView", camera.GetViewMat());
            shader.SetUniform("uProj", camera.GetProjMat());
            scene.DrawSingle(ref shader, camera.GetViewMat(), camera.GetProjMat());

            controller.Render();
        }

        static void ImGuiMenu(double deltaTime)
        {
            ImGuiWindowFlags window_flags = 0;
            window_flags |= ImGuiWindowFlags.NoTitleBar;
            window_flags |= ImGuiWindowFlags.MenuBar;

            ImGui.Begin("SpatialEngine", window_flags);

            ImGui.Text(string.Format("App avg {0:N3} ms/frame ({1:N1} FPS)", deltaTime * 1000, Math.Round(1.0f / deltaTime)));
            ImGui.Text(string.Format("{0} verts, {1} indices ({2} tris)", vertCount, indCount, indCount / 3));
            ImGui.Text(string.Format("Amount of Spatials: ({0})", scene.SpatialObjects.Count));
            //ImGui.Text(string.Format("Ram Usage: {0:N2}mb", process.PrivateMemorySize64 / 1024.0f / 1024.0f));
            ImGui.Text(string.Format("Time Open {0:N1} minutes", (totalTime / 60.0f)));

            ImGui.Spacing();
            ImGui.Checkbox("Wire Frame", ref showWireFrame);

            ImGui.Text("Camera");
            ImGui.SliderFloat3("Camera Position", ref camera.position, -10, 10);
        }
    }
}