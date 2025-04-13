using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System.Numerics;
namespace Szeminarium1_24_02_17_2
//Vegleges
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();
        private static CubeArrangementModel cubeArrangementModel = new();
        private static IWindow window;
        private static IInputContext inputContext;
        private static GL Gl;
        private static ImGuiController controller;
        private static uint program;
        private static GlCube glCubeCentered;
        private static GlCube glCubeRotating;
        private static float Shininess = 50;
        private static Vector3 ambientStrength = new(0.2f, 0.2f, 0.2f);
        private static Vector3 diffuseStrength = new(0.3f, 0.3f, 0.3f);
        private static Vector3 specularStrength = new(0.5f, 0.5f, 0.5f);
        private static Vector3 backgroundColor = new(1.0f, 1.0f, 1.0f);
        private static int selectedColorIndex = 0;
        private static readonly string[] colorNames = { "Red", "Green", "Blue", "Magenta", "Cyan", "Yellow" };
        private static readonly Vector4[] predefinedColors =
        {
            new(1,0,0,1), new(0,1,0,1), new(0,0,1,1), new(1,0,1,1), new(0,1,1,1), new(1,1,0,1)
        };

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNorm;

        uniform mat4 uModel;
        uniform mat3 uNormal;
        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPosition;

        void main()
        {
            outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos, 1.0);
            outNormal = uNormal*vNorm;
            outWorldPosition = vec3(uModel * vec4(vPos, 1.0));
        }
        ";

        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";

        private static readonly string FragmentShaderSource = @"
        #version 330 core
        
        uniform vec3 lightColor;
        uniform vec3 lightPos;
        uniform vec3 viewPos;
        uniform float shininess;
        uniform vec3 ambientStrength;
        uniform vec3 diffuseStrength;
        uniform vec3 specularStrength;

        out vec4 FragColor;

        in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;

        void main()
        {
            vec3 ambient = ambientStrength * lightColor;

            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(lightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * lightColor * diffuseStrength;

            vec3 viewDir = normalize(viewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininess) / max(dot(norm, viewDir), -dot(norm, lightDir));
            vec3 specular = specularStrength * spec * lightColor;

            vec3 result = (ambient + diffuse + specular) * outCol.xyz;
            FragColor = vec4(result, outCol.w);
        }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "2 szeminárium";
            windowOptions.Size = new Vector2D<int>(500, 500);
            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static void Window_Load()
        {
            inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();
            controller = new ImGuiController(Gl, window, inputContext);

            window.FramebufferResize += s => Gl.Viewport(s);
            Gl.ClearColor(System.Drawing.Color.White);
            SetUpObjects();
            LinkProgram();
            Gl.Enable(EnableCap.CullFace);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left:
                    cameraDescriptor.DecreaseZYAngle();
                    break;
                case Key.Right:
                    cameraDescriptor.IncreaseZYAngle();
                    break;
                case Key.Down:
                    cameraDescriptor.IncreaseDistance();
                    break;
                case Key.Up:
                    cameraDescriptor.DecreaseDistance();
                    break;
                case Key.U:
                    cameraDescriptor.IncreaseZXAngle();
                    break;
                case Key.D:
                    cameraDescriptor.DecreaseZXAngle();
                    break;
                case Key.Space:
                    cubeArrangementModel.AnimationEnabeld = !cubeArrangementModel.AnimationEnabeld;
                    break;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            cubeArrangementModel.AdvanceTime(deltaTime);
            controller.Update((float)deltaTime);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.ClearColor(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();
            SetLightColor();
            SetLightPosition();
            SetViewerPosition();
            SetShininess();
            SetLightingStrengths();

            DrawPulsingCenterCube();
            DrawRevolvingCube();
//csuszkak
            ImGuiNET.ImGui.Begin("Lighting properties", ImGuiWindowFlags.AlwaysAutoResize);
            ImGuiNET.ImGui.SliderFloat("Shininess", ref Shininess, 1, 200);
            ImGuiNET.ImGui.SliderFloat3("Ambient", ref ambientStrength, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat3("Diffuse", ref diffuseStrength, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat3("Specular", ref specularStrength, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat3("Background", ref backgroundColor, 0f, 1f);
            if (ImGuiNET.ImGui.Combo("Cube face 1 color", ref selectedColorIndex, colorNames, colorNames.Length))
            {
                glCubeCentered.UpdateFaceColor(0, predefinedColors[selectedColorIndex]);
            }
            ImGuiNET.ImGui.End();

            controller.Render();
        }

        private static unsafe void SetLightingStrengths()
        {
            int loc = Gl.GetUniformLocation(program, "ambientStrength");
            Gl.Uniform3(loc, ambientStrength.X, ambientStrength.Y, ambientStrength.Z);

            loc = Gl.GetUniformLocation(program, "diffuseStrength");
            Gl.Uniform3(loc, diffuseStrength.X, diffuseStrength.Y, diffuseStrength.Z);

            loc = Gl.GetUniformLocation(program, "specularStrength");
            Gl.Uniform3(loc, specularStrength.X, specularStrength.Y, specularStrength.Z);
        }

        private static unsafe void SetLightColor()
        {
            int location = Gl.GetUniformLocation(program, LightColorVariableName);
            Gl.Uniform3(location, 1f, 1f, 1f);
            CheckError();
        }

        private static unsafe void SetLightPosition()
        {
            int location = Gl.GetUniformLocation(program, LightPositionVariableName);
            Gl.Uniform3(location, 0f, 2f, 0f);
            CheckError();
        }

        private static unsafe void SetViewerPosition()
        {
            int location = Gl.GetUniformLocation(program, ViewPosVariableName);
            Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
            CheckError();
        }

        private static unsafe void SetShininess()
        {
            int location = Gl.GetUniformLocation(program, ShininessVariableName);
            Gl.Uniform1(location, Shininess);
            CheckError();
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 100);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);
            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        private static unsafe void DrawPulsingCenterCube()
        {
            var modelMatrix = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(glCubeCentered.Vao);
            Gl.DrawElements(GLEnum.Triangles, glCubeCentered.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void DrawRevolvingCube()
        {
            Matrix4X4<float> modelMatrix =
                Matrix4X4.CreateScale(0.25f) *
                Matrix4X4.CreateRotationX((float)Math.PI / 4f) *
                Matrix4X4.CreateRotationZ((float)Math.PI / 4f) *
                Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeAngleOwnRevolution) *
                Matrix4X4.CreateTranslation(1f, 1f, 0f) *
                Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeAngleRevolutionOnGlobalY);

            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(glCubeRotating.Vao);
            Gl.DrawElements(GLEnum.Triangles, glCubeRotating.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError();

            var noTranslate = modelMatrix;
            noTranslate.M41 = noTranslate.M42 = noTranslate.M43 = 0;
            noTranslate.M44 = 1;
            Matrix4X4.Invert(noTranslate, out var inverse);
            Matrix3X3<float> normalMatrix = new(Matrix4X4.Transpose(inverse));

            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUpObjects()
        {
            //elore definialt szinek
            float[] face1Color = [1.0f, 0.0f, 0.0f, 1.0f];
            float[] face2Color = [0.0f, 1.0f, 0.0f, 1.0f];
            float[] face3Color = [0.0f, 0.0f, 1.0f, 1.0f];
            float[] face4Color = [1.0f, 0.0f, 1.0f, 1.0f];
            float[] face5Color = [0.0f, 1.0f, 1.0f, 1.0f];
            float[] face6Color = [1.0f, 1.0f, 0.0f, 1.0f];

            glCubeCentered = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face2Color, face3Color, face4Color, face5Color, face6Color);
            glCubeRotating = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face1Color, face1Color, face1Color, face1Color, face1Color);
        }

        private static void Window_Closing()
        {
            glCubeCentered.ReleaseGlCube();
            glCubeRotating.ReleaseGlCube();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error);
        }
    }
} 