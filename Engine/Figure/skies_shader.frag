#version 430

out vec4 outputColor;

in vec2 mainTexCoord;
in vec2 lightmapTexCoord; // probably unused in here
in vec4 color; // probably unused in here
in vec3 position;

// q3 shader variables
uniform vec3 rgbgen;
uniform vec2 tcscroll; // s, t
uniform vec2 tcscale; // s, t
uniform vec2 tcscroll2; // s, t
uniform vec2 tcscale2; // s, t

// helper variables
uniform float timeS;

uniform sampler2D texture1;
uniform sampler2D texture2;
uniform sampler2D texture3; // probably unused in here

void main()
{
    vec2 texmod = mainTexCoord;
    // scroll
    texmod.x += tcscroll[0] * timeS * 10;
    texmod.y += tcscroll[1] * timeS * 10;
    // scale
    texmod.x /= tcscale[0];
    texmod.y /= tcscale[1];

    vec4 sky_texel1 = texture(texture1, texmod);

    texmod = mainTexCoord;
    // scroll
    texmod.x += tcscroll2[0] * timeS * 10;
    texmod.y += tcscroll2[1] * timeS * 10;
    // scale
    texmod.x /= tcscale2[0];
    texmod.y /= tcscale2[1];

    vec4 sky_texel2 = texture(texture2, texmod);

    outputColor = sky_texel1 + sky_texel2;
}