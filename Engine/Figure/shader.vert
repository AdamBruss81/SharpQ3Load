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
out float alphaGenSpecular;

uniform mat4 modelview;
uniform mat4 proj;
uniform vec3 camPosition; 

void CalculateTcGen(in vec3 campos, in vec3 position, in vec3 vertexnormal, out vec2 tcgen)
{
    vec3 viewer = campos - position;
    viewer = normalize(viewer);

    float d = dot(vertexNormal, viewer);

    vec3 reflected = vertexnormal * 2.0 * d - viewer;

    tcgen[0] = 0.5 + reflected[0] * 0.5;
    tcgen[1] = 0.5 - reflected[1] * 0.5;
}

void CalculateAlphaGenSpec(in vec3 campos, in vec3 position, in vec3 vertexnormal, out float alpha)
{
    vec3 lightorigin = vec3(-960, 1980, 96);

    vec3 lightdir = lightorigin - position;
    lightdir = normalize(lightdir);
    float d = dot(vertexnormal, lightdir);
    vec3 reflected = vertexnormal * 2 * d - lightdir;
    vec3 viewer = campos - position;
    float ilen = sqrt(dot(viewer, viewer));
    float l = dot(reflected, viewer);
    l *= ilen;

    if (l < 0) {
		alpha = 0;
	} 
    else {
		l = l*l;
		l = l*l;
		alpha = l * 255;
		if (alpha > 255) 
        {
			alpha = 255;
		}
	}
    alpha = alpha / 255;
}

void main(void)
{
    mainTexCoord = aTexCoord;
    lightmapTexCoord = aTexCoord2;

    vec3 viewer = camPosition - aPosition;
    viewer = normalize(viewer);

    CalculateTcGen(camPosition, aPosition, vertexNormal, tcgenEnvTexCoord);
    CalculateAlphaGenSpec(camPosition, aPosition, vertexNormal, alphaGenSpecular);

    color = aColor;
    vertice = aPosition;

    gl_Position = proj * modelview * vec4(aPosition, 1.0);
}
