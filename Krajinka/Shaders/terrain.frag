#version 330 core
in vec3 vertexColor;
in vec3 vertexNormal;
out vec4 FragColor;

uniform vec3 lightDir;

void main()
{
    vec3 normal = normalize(vertexNormal);
    vec3 lightDirection = normalize(-lightDir);

    float diffuse = max(dot(normal, lightDirection), 0.0);
    float ambient = 0.3;
    float light = ambient + diffuse * 0.7;

    FragColor = vec4(vertexColor * light, 1.0);
}
