using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Listochek.Common
{
    // A simple class meant to help create shaders.
    public class Shader
    {
        public readonly int Handle;

        private readonly Dictionary<string, int> _uniformLocations;

        public Shader(string vertPath, string fragPath)
        {
            //Читаем вершинный шейдер
            var shaderSource = File.ReadAllText(vertPath);
            //Генерируем вершинный шейдер
            var vertexShader = GL.CreateShader(ShaderType.VertexShader);

            GL.ShaderSource(vertexShader, shaderSource);
            
            //Компилируем шейдер
            CompileShader(vertexShader);

            // тоже самое но с фрагментарным шейдером
            shaderSource = File.ReadAllText(fragPath);
            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, shaderSource);
            CompileShader(fragmentShader);

            //Создание шейдерной программы 
            Handle = GL.CreateProgram();

            //Прикрепление шейдеров
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);

            //Линкуем их
            LinkProgram(Handle);

            //Уничтожение шейдеров
            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

            //Создание словаря для шейдеров
            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);
            _uniformLocations = new Dictionary<string, int>();
            // Loop over all the uniforms,
            for (var i = 0; i < numberOfUniforms; i++)
            {
                //Получаем имя перменных
                var key = GL.GetActiveUniform(Handle, i, out _, out _);

                //Получаем их положение
                var location = GL.GetUniformLocation(Handle, key);

                //Добавляем в словарь
                _uniformLocations.Add(key, location);
            }
        }

        private static void CompileShader(int shader)
        {
            //Компилируем шейдер
            GL.CompileShader(shader);

            //Для отлова исключений при компиляции
            GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                var infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
            }
        }

        private static void LinkProgram(int program)
        {
            //Линкуем программу
            GL.LinkProgram(program);

            //Для отлова исключений линковки
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
            if (code != (int)All.True)
            {
                throw new Exception($"Error occurred whilst linking Program({program})");
            }
        }

        //Подключаем шейдер 
        public void Use()
        {
            GL.UseProgram(Handle);
        }
        //Функции для взаимодействия с шейдерами в основной программе
        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }

        public void SetInt(string name, int data)
        {
            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data);
        }

        public void SetFloat(string name, float data)
        {
            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data);
        }

        public void SetMatrix4(string name, Matrix4 data)
        {
            GL.UseProgram(Handle);
            GL.UniformMatrix4(_uniformLocations[name], true, ref data);
        }

        public void SetVector3(string name, Vector3 data)
        {
            GL.UseProgram(Handle);
            GL.Uniform3(_uniformLocations[name], data);
        }
    }
}
