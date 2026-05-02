#version 330 core
out vec3 skyDirectionWorld;

uniform mat4 invProjection;
uniform mat4 invView;

void main()
{
    vec2 position;

    if (gl_VertexID == 0)
    {
        position = vec2(-1.0, -1.0);
    }
    else if (gl_VertexID == 1)
    {
        position = vec2(3.0, -1.0);
    }
    else
    {
        position = vec2(-1.0, 3.0);
    }

    vec4 clip = vec4(position, 1.0, 1.0);
    vec4 view = invProjection * clip;
    vec3 viewDirection = normalize(view.xyz / view.w);

    skyDirectionWorld = normalize(mat3(invView) * viewDirection);

    gl_Position = vec4(position, 1.0, 1.0);
}
