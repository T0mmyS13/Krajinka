#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aColor;
layout (location = 2) in vec3 aNormal;

out vec3 vertexColor;
out vec3 vNormal;
out vec3 fragmentWorld;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    vec4 worldPosition = model * vec4(aPos, 1.0);
    gl_Position = projection * view * worldPosition;

    fragmentWorld = worldPosition.xyz;
    vertexColor = aColor;
    vNormal = normalize(mat3(transpose(inverse(model))) * aNormal);
}
