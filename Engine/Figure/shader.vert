#version 430 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec2 aTexCoord2;
layout(location = 3) in vec4 aColor;

out vec2 mainTexCoord;
out vec2 lightmapTexCoord;
out vec4 color;
out vec3 vertice;

uniform mat4 modelview;
uniform mat4 proj;

void main(void)
{
    mainTexCoord = aTexCoord;
    lightmapTexCoord = aTexCoord2;
    color = aColor;
    vertice = aPosition;

    gl_Position = proj * modelview * vec4(aPosition, 1.0);
}