#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec2 aTexCoord2;
layout(location = 3) in vec4 aColor;

out vec2 texCoord;
out vec2 texCoord2;
out vec4 color;

uniform mat4 modelview;
uniform mat4 proj;

void main(void)
{
    texCoord = aTexCoord;
    texCoord2 = aTexCoord2;
    color = aColor;

    gl_Position = proj * modelview * vec4(aPosition, 1.0);
}