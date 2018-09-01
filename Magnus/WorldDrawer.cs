using Magnus.MagnusGL;
using Mathematics.Math3D;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BitmapData = System.Drawing.Imaging.BitmapData;
using ImageLockMode = System.Drawing.Imaging.ImageLockMode;
using SystemPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Magnus
{
    class WorldDrawer
    {
        private Font font;
        private int width = 1, height = 1;
        private double screenCoeff;
        private int programId;
        private int textTexture;
        private int mvpId;
        private int lightPositionId;
        private int cameraPositionId;
        private int ballPositionId, ballVectorId;
        private int enableLightId;
        private int shadowLengthId;
        private GlArrayBuffer coords, normals, colors;
        private Bitmap textTextureBmp = null;
        private Graphics textTextureGraphics = null;
        private List<GlMesh> meshes = new List<GlMesh>();

        public WorldDrawer(Font font)
        {
            this.font = font;

            screenCoeff = Constants.ScreenZoom * 2 / Constants.TableLength;

            programId = GL.CreateProgram();
            var vertexShaderId = loadShader(ShaderType.VertexShader, "VertexShader");
            var fragmentShaderId = loadShader(ShaderType.FragmentShader, "FragmentShader");
            GL.LinkProgram(programId);
            GL.GetProgram(programId, GetProgramParameterName.LinkStatus, out int status);
            if (status == 0)
            {
                throw new Exception("Failed to link program: " + GL.GetProgramInfoLog(programId));
            }
            GL.DetachShader(programId, vertexShaderId);
            GL.DetachShader(programId, fragmentShaderId);
            GL.DeleteShader(vertexShaderId);
            GL.DeleteShader(fragmentShaderId);
            GL.UseProgram(programId);

            mvpId = GL.GetUniformLocation(programId, "mvp");
            cameraPositionId = GL.GetUniformLocation(programId, "cameraPosition");
            lightPositionId = GL.GetUniformLocation(programId, "lightPosition");
            GL.Uniform3(lightPositionId, new Vector3((float)Constants.TableLength, (float)Constants.TableLength, (float)Constants.TableWidth) * 2);
            ballPositionId = GL.GetUniformLocation(programId, "ballPosition");
            ballVectorId = GL.GetUniformLocation(programId, "ballVector");
            enableLightId = GL.GetUniformLocation(programId, "enableLight");
            shadowLengthId = GL.GetUniformLocation(programId, "shadowLength");

            coords = new GlArrayBuffer(programId, "a_Position", 3);
            normals = new GlArrayBuffer(programId, "a_Normal", 3);
            colors = new GlArrayBuffer(programId, "a_Color", 4);

            GL.ClearColor(Color.CornflowerBlue);
            GL.BlendFunc(0, BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);

            textTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }

        private int loadShader(ShaderType type, string fileName)
        {
            var shaderId = GL.CreateShader(type);
            GL.ShaderSource(shaderId, File.ReadAllText(@"MagnusGL\Shaders\" + fileName + ".wlsl"));
            GL.CompileShader(shaderId);
            GL.GetShader(shaderId, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                throw new Exception("Failed to compile " + fileName + " shader: " + GL.GetShaderInfoLog(shaderId));
            }
            GL.AttachShader(programId, shaderId);
            return shaderId;
        }

        public void Start(int width, int height)
        {
            width = Math.Max(width, 1);
            height = Math.Max(height, 1);

            if (this.width != width || this.height != height)
            {
                this.width = width;
                this.height = height;

                if (textTextureBmp != null)
                {
                    textTextureBmp.Dispose();
                    textTextureGraphics.Dispose();
                }

                textTextureBmp = new Bitmap(width, height);
                textTextureGraphics = Graphics.FromImage(textTextureBmp);
            }

            textTextureGraphics.Clear(Color.Transparent);

            GL.UseProgram(programId);
            var projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4 / 2, (float)width / height, 0.01f, 4);
            var mouse = OpenTK.Input.Mouse.GetCursorState();
            var camera = Matrix4.CreateRotationY((float)Math.PI * mouse.X / 360)
                       * Matrix4.CreateRotationX((float)Math.PI / 2 * Math.Max(0, Math.Min(1, ((float)mouse.Y / Screen.PrimaryScreen.Bounds.Height - 0.5f) * 5)))
                       * Matrix4.CreateTranslation(0, 0, -2);
            var mv = Matrix4.CreateScale((float)screenCoeff) * camera;
            var mvp = mv * projection;
            GL.UniformMatrix4(mvpId, false, ref mvp);
            var cameraPosition = new Vector4(0, 0, 0, 1) * Matrix4.Invert(mv);
            GL.Uniform3(cameraPositionId, cameraPosition.Xyz);

            GL.Viewport(0, 0, width, height);
        }

        public void End()
        {
            GL.ClearStencil(1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.Disable(EnableCap.StencilTest);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.DepthMask(true);
            GL.DrawBuffer(DrawBufferMode.Back);
            renderMeshes(true, false);
            Profiler.Instance.LogEvent("drawer.End: depth");

            GL.Enable(EnableCap.StencilTest);
            GL.DepthMask(false);
            GL.Enable(EnableCap.DepthClamp);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.StencilFunc(StencilFunction.Always, 0, 0xFF);
            GL.StencilOpSeparate(StencilFace.Back, StencilOp.Keep, StencilOp.Keep, StencilOp.IncrWrap);
            GL.StencilOpSeparate(StencilFace.Front, StencilOp.Keep, StencilOp.Keep, StencilOp.DecrWrap);
            GL.DrawBuffer(DrawBufferMode.None);
            renderMeshes(false, false);
            GL.Disable(EnableCap.DepthClamp);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.Enable(EnableCap.Blend);

            GL.DrawBuffer(DrawBufferMode.Back);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(true);
            GL.StencilFunc(StencilFunction.Equal, 1, 0xFF);
            GL.StencilOpSeparate(StencilFace.Back, StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            renderMeshes(true, true);
            Profiler.Instance.LogEvent("drawer.End: final");

            GL.Disable(EnableCap.StencilTest);

            renderTexts();
            Profiler.Instance.LogEvent("drawer.End: texts");

            GL.Flush();
            Profiler.Instance.LogEvent("drawer.End: flush");
        }

        private void renderMeshes(bool real, bool light)
        {
            coords.Clear();
            normals.Clear();
            colors.Clear();
            if (!real) Profiler.Instance.LogEvent("drawer.End: shadow: clear");

            foreach (var mesh in meshes)
            {
                foreach (var triangle in real ? mesh.Triangles : mesh.ShadowTriangles)
                {
                    coords.Write(triangle.V0.Vertex.Position);
                    normals.Write(triangle.V0.Normal);
                    colors.Write(triangle.Color);
                    coords.Write(triangle.V1.Vertex.Position);
                    normals.Write(triangle.V1.Normal);
                    colors.Write(triangle.Color);
                    coords.Write(triangle.V2.Vertex.Position);
                    normals.Write(triangle.V2.Normal);
                    colors.Write(triangle.Color);
                }
            }
            if (!real) Profiler.Instance.LogEvent("drawer.End: shadow: write");

            coords.SetData();
            normals.SetData();
            colors.SetData();
            coords.Apply();
            normals.Apply();
            colors.Apply();
            if (!real) Profiler.Instance.LogEvent("drawer.End: shadow: send");

            GL.Uniform1(shadowLengthId, real ? 0f : 1000f);
            GL.Uniform1(enableLightId, light ? 1f : 0f);
            GL.DrawArrays(PrimitiveType.Triangles, 0, coords.Length);
            if (!real) Profiler.Instance.LogEvent("drawer.End: shadow: draw");
        }

        private void renderTexts()
        {
            textTextureGraphics.Flush();

            GL.UseProgram(0);

            GL.Enable(EnableCap.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, textTexture);
            BitmapData bd = new BitmapData();
            textTextureBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, SystemPixelFormat.Format32bppArgb, bd);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bd.Scan0);
            textTextureBmp.UnlockBits(bd);

            GL.Color4(Color.White);
            GL.Begin(PrimitiveType.Polygon);

            GL.TexCoord2(0, 1);
            GL.Vertex2(-1, -1);

            GL.TexCoord2(1, 1);
            GL.Vertex2(1, -1);

            GL.TexCoord2(1, 0);
            GL.Vertex2(1, 1);

            GL.TexCoord2(0, 0);
            GL.Vertex2(-1, 1);

            GL.End();

            GL.Disable(EnableCap.Texture2D);
        }

        private void drawCube(Color color, double x1, double y1, double z1, double x2, double y2, double z2, bool withShadow = true)
        {
            meshes.Add(new GlCube(color, x1, y1, z1, x2, y2, z2, withShadow));
        }

        private void drawCircle(Color color, Point3D center, double radius)
        {
            meshes.Add(new GlBall(color, center, radius));
        }

        private void drawPlane()
        {
            meshes.Add(new GlPlane(Color.Gray, -Constants.TableHeight, 10000));
        }

        private void drawTable()
        {
            var tableColor = Color.Green;
            drawCube(tableColor, -Constants.HalfTableLength, 0, -Constants.HalfTableWidth, Constants.HalfTableLength, -Constants.TableThickness, Constants.HalfTableWidth);
            for (int xSign = -1; xSign <= 1; xSign += 2)
            {
                for (int zSign = -1; zSign <= 1; zSign += 2)
                {
                    var x = zSign * (Constants.HalfTableLength - 10);
                    var z = xSign * (Constants.HalfTableWidth - 10);
                    drawCube(tableColor, x - 3, -Constants.TableThickness, z - 3, x + 3, -Constants.TableHeight, z + 3);
                }
            }
            drawCube(Color.FromArgb(254, 254, 254), -Constants.HalfTableLength, 0.5, -0.5, Constants.HalfTableLength, 0, 0.5, false);
            drawCube(Color.FromArgb(128, Color.Black), -0.5, Constants.NetHeight, -Constants.HalfNetWidth, 0.5, 0, Constants.HalfNetWidth);
        }

        private void drawPlayer(Player player)
        {
            meshes.Add(new GlPlayerBody(player));
            meshes.Add(new GlPlayerHandle(player));
        }

        private Vector3 toV3(Point3D v)
        {
            return new Vector3((float)v.X, (float)v.Y, (float)v.Z);
        }

        private void drawBall(Ball ball)
        {
            GL.Uniform3(ballPositionId, toV3(ball.Position));
            GL.Uniform3(ballVectorId, toV3(ball.MarkPoint.Normal));
            drawCircle(Color.White, ball.Position, Constants.BallRadius);
        }

        public void DrawWorld(State state)
        {
            meshes.Clear();
            drawPlane();
            foreach (var player in state.Players)
            {
                drawPlayer(player);
            }
            drawBall(state.Ball);
            drawTable();
        }

        public void DrawString(string text, int line, float alignment)
        {
            textTextureGraphics.DrawString(text, font, Brushes.Black, (width - textTextureGraphics.MeasureString(text, font).Width) * alignment, line * font.Height);
        }
    }
}
