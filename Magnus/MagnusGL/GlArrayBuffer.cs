using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Drawing;

namespace Magnus.MagnusGL
{
    class GlArrayBuffer
    {
        private int bufferId, variableLocation, itemSize;
        private List<float> data = new List<float>();

        public int Length => data.Count / itemSize;

        public GlArrayBuffer(int programId, string variableName, int itemSize)
        {
            GL.GenBuffers(1, out bufferId);
            variableLocation = GL.GetAttribLocation(programId, variableName);
            this.itemSize = itemSize;
        }

        #region GL

        public void Bind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, bufferId);
        }

        public void SetData()
        {
            Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, data.Count * sizeof(float), data.ToArray(), BufferUsageHint.StaticDraw);
        }

        public void Apply()
        {
            Bind();
            GL.VertexAttribPointer(variableLocation, itemSize, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(variableLocation);
        }

        #endregion

        #region Array

        public void Clear()
        {
            data.Clear();
        }

        public void Write(DoublePoint3D v)
        {
            data.Add((float)v.X);
            data.Add((float)v.Y);
            data.Add((float)v.Z);
        }

        public void Write(Color v)
        {
            data.Add(v.R / 255f);
            data.Add(v.G / 255f);
            data.Add(v.B / 255f);
            data.Add(v.A / 255f);
        }

        #endregion
    }
}
