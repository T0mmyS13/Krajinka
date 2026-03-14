using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

/// <summary>
/// Reprezentuje jednoduchý bodový terén vykreslovaný pomocí OpenGL shaderů.
/// </summary>
public class Terrain
{
    private readonly float[] vertices;
    private int VBO;
    private int VAO;
    private int shaderProgram;
    private int vertexCount;

    private readonly string vertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 aPos;
        layout (location = 1) in vec3 aColor;

        out vec3 vertexColor;

        uniform mat4 model;
        uniform mat4 view;
        uniform mat4 projection;

        void main()
        {
            gl_Position = vec4(aPos, 1.0) * model * view * projection;
            vertexColor = aColor;
        }
    ";

    private readonly string fragmentShaderSource = @"
        #version 330 core
        in vec3 vertexColor;
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(vertexColor, 1.0);
        }
    ";

    /// <summary>
    /// Vytvoří bodový terén o zadané šířce a hloubce a připraví data pro vykreslení.
    /// </summary>
    /// <param name="width">Počet bodů v ose X.</param>
    /// <param name="depth">Počet bodů v ose Z.</param>
    public Terrain(int width, int depth)
    {
        vertexCount = width * depth;
        vertices = new float[vertexCount * 6];

        int index = 0;
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                vertices[index++] = x * 0.5f;
                vertices[index++] = 0.0f;
                vertices[index++] = z * 0.5f;

                if ((x + z) % 2 == 0)
                {
                    vertices[index++] = 0.2f;
                    vertices[index++] = 0.8f;
                    vertices[index++] = 0.2f;
                }
                else
                {
                    vertices[index++] = 0.1f;
                    vertices[index++] = 0.6f;
                    vertices[index++] = 0.1f;
                }
            }
        }

        SetupShaders();
        SetupBuffers();
    }

    /// <summary>
    /// Zkompiluje shadery a vytvoří OpenGL shader program.
    /// </summary>
    private void SetupShaders()
    {
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);

        shaderProgram = GL.CreateProgram();
        GL.AttachShader(shaderProgram, vertexShader);
        GL.AttachShader(shaderProgram, fragmentShader);
        GL.LinkProgram(shaderProgram);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    /// <summary>
    /// Vytvoří a nastaví OpenGL buffery a atributy vrcholů pro pozici a barvu.
    /// </summary>
    private void SetupBuffers()
    {
        VAO = GL.GenVertexArray();
        GL.BindVertexArray(VAO);

        VBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
    }

    /// <summary>
    /// Vykreslí terén pomocí předaných transformačních matic.
    /// </summary>
    /// <param name="model">Modelová matice objektu.</param>
    /// <param name="view">Pohledová matice kamery.</param>
    /// <param name="projection">Projekční matice kamery.</param>
    public void Render(Matrix4 model, Matrix4 view, Matrix4 projection)
    {
        GL.UseProgram(shaderProgram);
        GL.BindVertexArray(VAO);

        int modelLocation = GL.GetUniformLocation(shaderProgram, "model");
        int viewLocation = GL.GetUniformLocation(shaderProgram, "view");
        int projLocation = GL.GetUniformLocation(shaderProgram, "projection");

        GL.UniformMatrix4(modelLocation, true, ref model);
        GL.UniformMatrix4(viewLocation, true, ref view);
        GL.UniformMatrix4(projLocation, true, ref projection);

        GL.DrawArrays(PrimitiveType.Points, 0, vertexCount);
    }
}