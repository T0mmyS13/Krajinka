using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// OpenGL shader program s cache uniform proměnných.
/// </summary>
public class Shader : IDisposable
{
    public int ProgramId;

    private readonly Dictionary<string, int> uniforms = new();

    public Shader(string vertexPath, string fragmentPath)
    {
        int vertexShader = CompileShader(vertexPath, ShaderType.VertexShader);
        int fragmentShader = CompileShader(fragmentPath, ShaderType.FragmentShader);

        LinkShader(vertexShader, fragmentShader);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        LoadUniforms();
    }

    public void Use()
    {
        GL.UseProgram(ProgramId);
    }

    public void SetUniform<T>(string name, T value)
    {
        int location = GetUniformLocation(name);
        if (location == -1)
        {
            return;
        }

        switch (value)
        {
            case int uniformInt:
                GL.Uniform1(location, uniformInt);
                break;
            case float uniformFloat:
                GL.Uniform1(location, uniformFloat);
                break;
            case Vector3 uniformVector3:
                GL.Uniform3(location, uniformVector3);
                break;
            case Vector4 uniformVector4:
                GL.Uniform4(location, uniformVector4);
                break;
            case Matrix4 uniformMatrix4:
                GL.UniformMatrix4(location, false, ref uniformMatrix4);
                break;
            default:
                throw new NotSupportedException($"Uniform type {typeof(T)} is not supported.");
        }
    }

    private int CompileShader(string relativePath, ShaderType shaderType)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        string source = File.ReadAllText(fullPath);

        int shader = GL.CreateShader(shaderType);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int compileStatus);
        if (compileStatus == 0)
        {
            string shaderLog = GL.GetShaderInfoLog(shader);
            throw new InvalidOperationException($"Chyba kompilace shaderu '{relativePath}': {shaderLog}");
        }

        return shader;
    }

    private void LinkShader(int vertexShader, int fragmentShader)
    {
        ProgramId = GL.CreateProgram();
        GL.AttachShader(ProgramId, vertexShader);
        GL.AttachShader(ProgramId, fragmentShader);
        GL.LinkProgram(ProgramId);

        GL.GetProgram(ProgramId, GetProgramParameterName.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
        {
            string programLog = GL.GetProgramInfoLog(ProgramId);
            throw new InvalidOperationException($"Chyba linkování shader programu: {programLog}");
        }
    }

    private int GetUniformLocation(string name)
    {
        if (uniforms.TryGetValue(name, out int location))
        {
            return location;
        }
        
        Console.WriteLine($"Warning: Uniform '{name}' not found.");
        return -1;
    }

    private void LoadUniforms()
    {
        GL.GetProgram(ProgramId, GetProgramParameterName.ActiveUniforms, out int uniformCount);
        for (int i = 0; i < uniformCount; i++)
        {
            GL.GetActiveUniform(ProgramId, i, 256, out _, out _, out _, out string name);
            int location = GL.GetUniformLocation(ProgramId, name);
            if (location != -1)
            {
                uniforms[name] = location;
                Console.WriteLine($"Loaded uniform: {name} -> {location}");
            }
        }
    }

    private bool disposed;

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        GL.DeleteProgram(ProgramId);
        disposed = true;
    }
}
