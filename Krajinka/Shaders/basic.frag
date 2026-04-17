#version 330 core
in vec3 vNormal;
in vec3 fragmentWorld;
in vec2 vUV;
out vec4 outColor;

uniform vec4 lightPosWorld;
uniform vec3 lightColor;
uniform float lightIntensity;
uniform sampler2D texGrass;
uniform sampler2D texRock;
uniform sampler2D texMud;
uniform sampler2D surfaceTypeMap;
uniform int useTexture;
uniform int isTerrain;
uniform vec2 terrainMaxXZ;

float Transition(float value, float center, float halfWidth)
{
    return smoothstep(center - halfWidth, center + halfWidth, value);
}

void main()
{
    vec3 normal = normalize(vNormal);
    vec3 lightDir = normalize(lightPosWorld.xyz - fragmentWorld);

    float NdotL = max(0.0, dot(normal, lightDir));

    vec3 baseColor = vec3(1.0, 1.0, 1.0);
    if (useTexture == 1)
    {
        if (isTerrain == 1)
        {
            vec2 mapUv = fragmentWorld.xz / terrainMaxXZ;
            float typeValue = texture(surfaceTypeMap, mapUv).r * 255.0;
            vec3 grassColor = texture(texGrass, vUV).rgb;
            vec3 rockColor = texture(texRock, vUV).rgb;
            vec3 mudColor = texture(texMud, vUV).rgb;

            float blendHalfWidth = 0.25;

            if (typeValue < 1.5)
            {
                float t = Transition(typeValue, 1.5, blendHalfWidth);
                baseColor = mix(mudColor, grassColor, t);
            }
            else if (typeValue < 2.5)
            {
                float t = Transition(typeValue, 2.5, blendHalfWidth);
                baseColor = mix(grassColor, rockColor, t);
            }
            else
            {
                baseColor = rockColor;
            }
        }
        else
        {
            baseColor = texture(texGrass, vUV).rgb;
        }
    }

    vec3 ambient = vec3(0.2) * baseColor;
    vec3 diffuse = lightColor * lightIntensity * NdotL * baseColor;
    outColor = vec4(ambient + diffuse, 1.0);
}
