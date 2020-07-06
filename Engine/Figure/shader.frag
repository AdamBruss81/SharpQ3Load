#version 430

out vec4 outputColor;

in vec2 mainTexCoord;
in vec2 lightmapTexCoord;
in vec4 color;
in vec3 vertice;

// q3 shader variables
uniform int custom_shader_controller;
uniform vec3 rgbgen;
uniform vec2 tcscroll; // s, t
uniform vec2 tcscale; // s, t
uniform vec3 tcturb; // amp, phase, freq

// helper variables
uniform float timeS;

uniform sampler2D texture0;
uniform sampler2D texture1;
uniform sampler2D texture2;

void main()
{ 
    // lightmap
    if (lightmapTexCoord.x != -1.0) {        
        vec4 main_tex_texel = texture(texture0, mainTexCoord); // main texture
        vec4 lightmap_texel = texture(texture1, lightmapTexCoord); // lightmap

        if(custom_shader_controller == 2) {
            // do some shader effects like the eyes on fatal instinct and the cross on fatal instinct
            vec4 texel2 = texture(texture2, mainTexCoord); // experimental third texture for example for killblock_i4b
            vec4 pulser = vec4(rgbgen, 1.0);
            outputColor = clamp(main_tex_texel * lightmap_texel * 3.0 + (texel2 * pulser), 0.0, 1.0);
        }
        else if(custom_shader_controller == 3) { // fire under floor from fatal instinct
            vec2 mainTexCoord_Mod = mainTexCoord;

            // the order of these tc mods is hardcoded here for now. it should follow what's in the shader stages
            // scroll
            mainTexCoord_Mod.x += tcscroll[0] * timeS;
            mainTexCoord_Mod.y += tcscroll[1] * timeS;    

            // turb - this does not look quite right for example for the flames in the floor in fatal instinct.
            // in quake 3, the flames appear to also swirl around in a circle making it look really turbulent.
            // I couldn't reproduce that despite looking at the source. Revisit at some point. Move on for now.
            // It still looks pretty close and good. Maybe I need to use the vertices in the vertex shader before
            // they are interpolated into the frag shader? I'm not sure. I'm stretching my glsl shader knowledge with this.
            // going to move on for now.
            float turbVal = tcturb[1] + timeS * tcturb[2];
            mainTexCoord_Mod.x += sin( ( (vertice.x + vertice.z) * 1.0/128.0 * 0.125 + turbVal ) * 6.238) * tcturb[0];
            mainTexCoord_Mod.y += sin( (vertice.y * 1.0/128.0 * 0.125 + turbVal ) * 6.238) * tcturb[0];

            // scale       
            mainTexCoord_Mod.x *= tcscale[0];
            mainTexCoord_Mod.y *= tcscale[1];

            vec4 firegore_texel = texture(texture2, mainTexCoord_Mod); // stage 1         
            
            vec4 holy_floor_texel = texture(texture0, mainTexCoord);            
            vec4 mixed = mix(firegore_texel, holy_floor_texel, holy_floor_texel.w); // stage 2

            // GL_DST_COLOR GL_ONE_MINUS_DST_ALPHA
            outputColor = (lightmap_texel * mixed + mixed * (1 - lightmap_texel.w)) * 3.0; // stage 3
        }
        else {
            // just lightmapping
            outputColor = clamp(main_tex_texel * lightmap_texel * 3.0, 0.0, 1.0);
        }
    }
    // vertice colors
    else {        
        vec4 texel0 = texture(texture0, mainTexCoord);
        outputColor = texel0 * color * 2.0;
    }
}