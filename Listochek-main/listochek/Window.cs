using Listochek.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

namespace Listochek
{
    public class Window : GameWindow
    {
        // Моделька
        private readonly float[] _vertices =    
        {
            //Позиции точек       |Вектора нормалей    |Координаты текстур
            -0.5f,  0.0f, -0.5f,  0.0f,  1.0f,  0.0f,  0.0f, 1.0f,
             0.5f,  0.0f, -0.5f,  0.0f,  1.0f,  0.0f,  1.0f, 1.0f,
             0.5f,  0.0f,  0.5f,  0.0f,  1.0f,  0.0f,  1.0f, 0.0f,
             0.5f,  0.0f,  0.5f,  0.0f,  1.0f,  0.0f,  1.0f, 0.0f,
            -0.5f,  0.0f,  0.5f,  0.0f,  1.0f,  0.0f,  0.0f, 0.0f,
            -0.5f,  0.0f, -0.5f,  0.0f,  1.0f,  0.0f,  0.0f, 1.0f,
            //Вторая сторона
            -0.5f,  -0.001f, -0.5f,  0.0f,  -1.0f,  0.0f,  0.0f, 1.0f,
             0.5f,  -0.001f, -0.5f,  0.0f,  -1.0f,  0.0f,  1.0f, 1.0f,
             0.5f,  -0.001f,  0.5f,  0.0f,  -1.0f,  0.0f,  1.0f, 0.0f,
             0.5f,  -0.001f,  0.5f,  0.0f,  -1.0f,  0.0f,  1.0f, 0.0f,
            -0.5f,  -0.001f,  0.5f,  0.0f,  -1.0f,  0.0f,  0.0f, 0.0f,
            -0.5f,  -0.001f, -0.5f,  0.0f,  -1.0f,  0.0f,  0.0f, 1.0f
        };

        //Текстура листочка
        private Texture TexListochka;
        //Для выбора текстурки
        private string res = "Resources/listochek.png";

        private int _vertexBufferObject;
        private int _vaoModel;
        private int _vaoLamp;

        //Шейдер лампочки
        private Shader _lampShader;
        //Шейдер света
        private Shader _lightingShader;
        // Позиция источника света
        private readonly Vector3 _lightPos = new Vector3(0.0f, 1.0f, 0.0f);

        //Для камеры
        private Camera _camera;
        private Vector3 _startCameraPos = new Vector3(0.0f, 0.4f, 1.2f);

        private bool _firstMove = true;

        private Vector2 _lastPos;

        //Переменная для скалирования от времени
        private double _time;

        //Координаты для перемещения в пространстве
        private float xv, yv = 0.0f, zv;
        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            //Задний фон
            GL.ClearColor(0.6f, 0.6f, 0.8f, 1.0f);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            //Принимаем шейдеры из файлов
            _lightingShader = new Shader("Shaders/shader.vert", "Shaders/lighting.frag");
            _lampShader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");

            {
                _vaoModel = GL.GenVertexArray();
                GL.BindVertexArray(_vaoModel);

                //Принимаем координаты Точек модельки
                var positionLocation = _lightingShader.GetAttribLocation("aPos");
                GL.EnableVertexAttribArray(positionLocation);
                GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
                //Принимаем координаты векторов нормалей
                var normalLocation = _lightingShader.GetAttribLocation("aNormal");
                GL.EnableVertexAttribArray(normalLocation);
                GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
                //Принимаем Координаты для текстуры
                var texCoordLocation = _lightingShader.GetAttribLocation("aTexCoords");
                GL.EnableVertexAttribArray(texCoordLocation);
                GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
            }

            {
                _vaoLamp = GL.GenVertexArray();
                GL.BindVertexArray(_vaoLamp);
                
                //Принимаем координаты позиций для модельки источника света
                var positionLocation = _lampShader.GetAttribLocation("aPos");
                GL.EnableVertexAttribArray(positionLocation);
                GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            }

            //Для избежания просвета сквозь моделек
            GL.Enable(EnableCap.DepthTest);
            //Загружаем тектстру в пеменную
            TexListochka = Texture.LoadFromFile(res);
            //Создаём камеру
            _camera = new Camera(_startCameraPos, 0.0f);

            //Захват мыши
            CursorGrabbed = true;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            //Тут можно менять скалирование от времени
            _time += 60.0 * e.Time;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.BindVertexArray(_vaoModel);


            //Использование света
            _lightingShader.Use();

            /////////////////////////////////СОЗДАЁМ КУЧУ МОДЕЛЕК ЛИСТОЧКА\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
            
            //моделька      //поворот вокруг оси X                                              //Вращение вокруг оси Y
            Matrix4 model = Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(_time)) * Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(_time))
                * Matrix4.CreateTranslation(xv, yv, zv); //Здесь задаём координаты модельки

            _lightingShader.SetMatrix4("model", model); //Использовать на объекте шейдер
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36); //Рисование примитивов

            Matrix4 model1 = Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(-_time)) * Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(_time))
                * Matrix4.CreateTranslation(xv + 2.0f, yv + 1.0f, zv);

            _lightingShader.SetMatrix4("model", model1);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            Matrix4 model2 = Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(_time)) * Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(-_time))
                * Matrix4.CreateTranslation(xv - 2.0f, yv, zv);

            _lightingShader.SetMatrix4("model", model2);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            Matrix4 model3 = Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(-_time)) * Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(_time))
                * Matrix4.CreateTranslation(xv, yv - 1.0f, zv + 2.0f);

            _lightingShader.SetMatrix4("model", model3);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            Matrix4 model4 = Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(_time)) * Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(-_time))
                * Matrix4.CreateTranslation(xv, yv, zv - 2.0f);

            _lightingShader.SetMatrix4("model", model4);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            Matrix4 model5 =  Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(-_time)) * Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(_time))
                * Matrix4.CreateTranslation(xv + 2.0f, yv + 1.0f, zv - 2.0f);

            _lightingShader.SetMatrix4("model", model5);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            Matrix4 model6 = Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(_time)) * Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(-_time))
                * Matrix4.CreateTranslation(xv - 2.0f, yv, zv + 2.0f);

            _lightingShader.SetMatrix4("model", model6);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            Matrix4 model7 = Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(-_time)) * Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(_time))
                * Matrix4.CreateTranslation(xv - 2.0f, yv - 1.0f, zv - 2.0f);

            _lightingShader.SetMatrix4("model", model7);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            Matrix4 model8 = Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(_time)) * Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(-_time))
                * Matrix4.CreateTranslation(xv + 2.0f, yv, zv + 2.0f);

            _lightingShader.SetMatrix4("model", model8);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            //Движение
            yv -= 0.3f * (float)e.Time;
            //zv += 0.3f * (float)e.Time;
            
            _lightingShader.Use();

            /*
            Matrix4 modeln = Matrix4.Identity *  Matrix4.CreateTranslation(0.0f, 2.0f, 0.0f);
            _lightingShader.SetMatrix4("model", modeln);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            Matrix4 modeln2 = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, 0.0f);
            _lightingShader.SetMatrix4("model", modeln2);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            Matrix4 modeln3 = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 1.0f, -1.0f) * Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(90.0f));
            _lightingShader.SetMatrix4("model", modeln3);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            */

            //Настрока Шейдера 
            _lightingShader.SetMatrix4("view", _camera.GetViewMatrix());
            _lightingShader.SetMatrix4("projection", _camera.GetProjectionMatrix());

            _lightingShader.SetVector3("viewPos", _camera.Position);

            _lightingShader.SetVector3("material.specular", new Vector3(0.5f, 0.5f, 0.5f));
            _lightingShader.SetFloat("material.shininess", 32.0f);

            _lightingShader.SetVector3("light.position", _lightPos);
            _lightingShader.SetVector3("light.ambient", new Vector3(0.2f)); //Яркость
            _lightingShader.SetVector3("light.diffuse", new Vector3(0.5f)); //Рассеивание света
            _lightingShader.SetVector3("light.specular", new Vector3(0.5f)); //Затенение

            /////////////////////////   ТУТ ЛАМОЧКА    \\\\\\\\\\\\\\\\\\\\\\\\\\
            GL.BindVertexArray(_vaoLamp);

            _lampShader.Use();

            Matrix4 lampMatrix = Matrix4.Identity;
            lampMatrix *= Matrix4.CreateScale(0.2f);
            lampMatrix *= Matrix4.CreateTranslation(_lightPos);

            _lampShader.SetMatrix4("model", lampMatrix);
            _lampShader.SetMatrix4("view", _camera.GetViewMatrix());
            _lampShader.SetMatrix4("projection", _camera.GetProjectionMatrix());

            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (!IsFocused)
            {
                return;
            }
            //Читаем кнопки
            var input = KeyboardState;
            //Выход
            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }
            //Хар-ка камеры
            const float cameraSpeed = 1.5f;
            const float sensitivity = 0.2f;
            //Читаем инпуты
            if (input.IsKeyDown(Keys.W))
            {
                _camera.Position += _camera.Front * cameraSpeed * (float)e.Time; //Вперёд
            }
            if (input.IsKeyDown(Keys.S))
            {
                _camera.Position -= _camera.Front * cameraSpeed * (float)e.Time; // Назад
            }
            if (input.IsKeyDown(Keys.A))
            {
                _camera.Position -= _camera.Right * cameraSpeed * (float)e.Time; // Влево
            }
            if (input.IsKeyDown(Keys.D))
            {
                _camera.Position += _camera.Right * cameraSpeed * (float)e.Time; // Вправо
            }
            if (input.IsKeyDown(Keys.Space))
            {
                _camera.Position += _camera.Up * cameraSpeed * (float)e.Time; // Вверх
            }
            if (input.IsKeyDown(Keys.LeftShift))
            {
                _camera.Position -= _camera.Up * cameraSpeed * (float)e.Time; // Вниз
            }

            //Читаем движение мыши
            var mouse = MouseState;

            if (_firstMove)
            {
                _lastPos = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - _lastPos.X;
                var deltaY = mouse.Y - _lastPos.Y;
                _lastPos = new Vector2(mouse.X, mouse.Y);

                _camera.Yaw += deltaX * sensitivity;
                _camera.Pitch -= deltaY * sensitivity;
            }
        }

        //Меняем угол обзора
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _camera.Fov -= e.OffsetY;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
            _camera.AspectRatio = Size.X / (float)Size.Y;
        }
    }
}