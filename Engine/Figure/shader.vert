#version 430 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec2 aTexCoord2;
layout(location = 3) in vec4 aColor;
layout(location = 4) in vec3 vertexNormal; // normalized when it comes in

out vec2 mainTexCoord;
out vec2 lightmapTexCoord;
out vec4 color;
out vec3 vertice;
out vec2 tcgenEnvTexCoord;

uniform mat4 modelview;
uniform mat4 proj;
uniform vec3 camPosition; 

void main(void)
{
    mainTexCoord = aTexCoord;
    lightmapTexCoord = aTexCoord2;

    vec3 viewer = camPosition - aPosition;
    viewer = normalize(viewer);

    float d = dot(vertexNormal, viewer);

    vec3 reflected;
    reflected[0] = vertexNormal[0] * 2.0 * d - viewer[0];
    reflected[1] = vertexNormal[1] * 2.0 * d - viewer[1];
    reflected[2] = vertexNormal[2] * 2.0 * d - viewer[2];

    tcgenEnvTexCoord[0] = 0.5 + reflected[0] * 0.5;
    tcgenEnvTexCoord[1] = 0.5 - reflected[1] * 0.5; // because of reflection of verts we do at read time?

    color = aColor;
    vertice = aPosition;

    gl_Position = proj * modelview * vec4(aPosition, 1.0);
}