#version 430 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec2 aTexCoord2;
layout(location = 3) in vec4 aColor;
layout(location = 4) in vec3 vertexNormal;

out vec2 mainTexCoord;
out vec2 lightmapTexCoord;
out vec4 color;
out vec3 vertice;
out vec2 tcgenEnvTexCoord;

uniform mat4 modelview;
uniform mat4 proj;
uniform vec3 camPosition; // normalized when it comes in

void main(void)
{
    mainTexCoord = aTexCoord;
    lightmapTexCoord = aTexCoord2;

    // define tcgenEnvTexCoord
    vec3 viewer = normalize(-camPosition);
    float d = dot(vertexNormal, viewer);
    vec3 reflected = vertexNormal * 2.0 * d - viewer;
    tcgenEnvTexCoord = vec2(0.5, 0.5) + reflected.xy * 0.5;

    color = aColor;
    vertice = aPosition;

    gl_Position = proj * modelview * vec4(aPosition, 1.0);
}