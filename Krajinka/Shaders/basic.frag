#version 330 core
in vec3 vertexColor;
in vec3 vNormal;
in vec3 fragmentWorld;
out vec4 outColor;

uniform vec4 lightPosWorld;
uniform vec3 lightColor;
uniform float lightIntensity;
uniform int isSun;

void main()
{
    if (isSun == 1)
    {
        outColor = vec4(vertexColor, 1.0);
        return;
    }

    vec3 normal = normalize(vNormal);
    vec3 lightDir = normalize(lightPosWorld.xyz - fragmentWorld);

    float NdotL = max(0.0, dot(normal, lightDir));

    vec3 ambient = vec3(0.2) * vertexColor;
    vec3 diffuse = lightColor * lightIntensity * NdotL * vertexColor;

    outColor = vec4(ambient + diffuse, 1.0);
}
