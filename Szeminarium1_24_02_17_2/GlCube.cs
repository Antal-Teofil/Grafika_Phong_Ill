using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
//kicsit atrendeztuk, hogy modularisabb legyen
namespace Szeminarium1_24_02_17_2
{
    internal class GlCube
    {
        public uint Vao { get; }
        public uint Vertices { get; }
        public uint Colors { get; }
        public uint Indices { get; }
        public uint IndexArrayLength { get; }

        private GL Gl;
        private float[] colorArray;

        private GlCube(uint vao, uint vertices, uint colors, uint indices, uint indexArrayLength, GL gl, float[] colorArray)
        {
            Vao = vao;
            Vertices = vertices;
            Colors = colors;
            Indices = indices;
            IndexArrayLength = indexArrayLength;
            Gl = gl;
            this.colorArray = colorArray;
        }

        public static unsafe GlCube CreateCubeWithFaceColors(GL Gl, float[] face1Color, float[] face2Color, float[] face3Color, float[] face4Color, float[] face5Color, float[] face6Color)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            float[] vertexArray = new float[] {
                -0.5f, 0.5f, 0.5f, 0f, 1f, 0f,
                0.5f, 0.5f, 0.5f, 0f, 1f, 0f,
                0.5f, 0.5f, -0.5f, 0f, 1f, 0f,
                -0.5f, 0.5f, -0.5f, 0f, 1f, 0f,
                -0.5f, 0.5f, 0.5f, 0f, 0f, 1f,
                -0.5f, -0.5f, 0.5f, 0f, 0f, 1f,
                0.5f, -0.5f, 0.5f, 0f, 0f, 1f,
                0.5f, 0.5f, 0.5f, 0f, 0f, 1f,
                -0.5f, 0.5f, 0.5f, -1f, 0f, 0f,
                -0.5f, 0.5f, -0.5f, -1f, 0f, 0f,
                -0.5f, -0.5f, -0.5f, -1f, 0f, 0f,
                -0.5f, -0.5f, 0.5f, -1f, 0f, 0f,
                -0.5f, -0.5f, 0.5f, 0f, -1f, 0f,
                0.5f, -0.5f, 0.5f,0f, -1f, 0f,
                0.5f, -0.5f, -0.5f,0f, -1f, 0f,
                -0.5f, -0.5f, -0.5f,0f, -1f, 0f,
                0.5f, 0.5f, -0.5f, 0f, 0f, -1f,
                -0.5f, 0.5f, -0.5f,0f, 0f, -1f,
                -0.5f, -0.5f, -0.5f,0f, 0f, -1f,
                0.5f, -0.5f, -0.5f,0f, 0f, -1f,
                0.5f, 0.5f, 0.5f, 1f, 0f, 0f,
                0.5f, 0.5f, -0.5f,1f, 0f, 0f,
                0.5f, -0.5f, -0.5f,1f, 0f, 0f,
                0.5f, -0.5f, 0.5f,1f, 0f, 0f
            };

            List<float> colorsList = new();
            colorsList.AddRange(Enumerable.Repeat(face1Color, 4).SelectMany(c => c));
            colorsList.AddRange(Enumerable.Repeat(face2Color, 4).SelectMany(c => c));
            colorsList.AddRange(Enumerable.Repeat(face3Color, 4).SelectMany(c => c));
            colorsList.AddRange(Enumerable.Repeat(face4Color, 4).SelectMany(c => c));
            colorsList.AddRange(Enumerable.Repeat(face5Color, 4).SelectMany(c => c));
            colorsList.AddRange(Enumerable.Repeat(face6Color, 4).SelectMany(c => c));

            float[] colorArray = colorsList.ToArray();

            uint[] indexArray = new uint[] {
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7,
                8, 9, 10, 10, 11, 8,
                12, 14, 13, 12, 15, 14,
                17, 16, 19, 17, 19, 18,
                20, 22, 21, 20, 23, 22
            };

            uint offsetPos = 0;
            uint offsetNormal = offsetPos + (3 * sizeof(float));
            uint vertexSize = offsetNormal + (3 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData<float>(GLEnum.ArrayBuffer, vertexArray, GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData<float>(GLEnum.ArrayBuffer, colorArray, GLEnum.DynamicDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData<uint>(GLEnum.ElementArrayBuffer, indexArray, GLEnum.StaticDraw);

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexArrayLength = (uint)indexArray.Length;

            return new GlCube(vao, vertices, colors, indices, indexArrayLength, Gl, colorArray);
        }

        public unsafe void UpdateFaceColor(int faceIndex, Vector4 color) // itt lehet modositani egy oldallap szinet
        {
            if (faceIndex < 0 || faceIndex > 5) return;
            for (int i = 0; i < 4; i++)
            {
                int baseIndex = faceIndex * 16 + i * 4;
                colorArray[baseIndex + 0] = color.X;
                colorArray[baseIndex + 1] = color.Y;
                colorArray[baseIndex + 2] = color.Z;
                colorArray[baseIndex + 3] = color.W;
            }
            Gl.BindBuffer(GLEnum.ArrayBuffer, Colors);
            fixed (float* ptr = colorArray)
            {
                Gl.BufferSubData(GLEnum.ArrayBuffer, 0, (nuint)(sizeof(float) * colorArray.Length), ptr);
            }
        }

        internal void ReleaseGlCube()
        {
            Gl.DeleteBuffer(Vertices);
            Gl.DeleteBuffer(Colors);
            Gl.DeleteBuffer(Indices);
            Gl.DeleteVertexArray(Vao);
        }
    }
}