#version 330 core
in vec3 vertexColor;
in vec3 vNormal;
in vec3 fragmentWorld;
out vec4 outColor;

uniform vec4 lightPosWorld;
uniform vec3 lightColor;
uniform float lightIntensity;
uniform vec3 cameraposWorld;

void main()
{
    vec3 normal = normalize(vNormal);
    vec3 lightDir = normalize(lightPosWorld.xyz - fragmentWorld);

    float NdotL = max(0.0, dot(normal, lightDir));

    vec3 specular = vec3(0.0);

    if (NdotL > 0.0)
    {
        vec3 reflectDir = reflect(-lightDir, normal);
        vec3 viewDir = normalize(cameraposWorld - fragmentWorld);
        float spec = pow(max(0.0, dot(reflectDir, viewDir)), 5.0);
        specular = lightColor * lightIntensity * spec;
    }

    vec3 diffuse = lightColor * lightIntensity * NdotL;
    vec3 lighting = diffuse + specular;

    outColor = vec4(vertexColor * lighting, 1.0);
}
