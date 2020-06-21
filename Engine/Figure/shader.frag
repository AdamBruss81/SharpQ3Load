#version 430

out vec4 outputColor;

in vec2 texCoord;
in vec2 texCoord2;
in vec4 color;

// q3 shader variables
flat in int test;
uniform vec3 rgbgen;

uniform sampler2D texture1;
uniform sampler2D texture2;
uniform sampler2D texture3;

void main()
{ 
    // lightmap
    if (texCoord2.x != -1.0) {        
        vec4 texel0 = texture(texture1, texCoord); // main texture
        vec4 texel1 = texture(texture2, texCoord2); // lightmap

        if(test == 2) {
            vec4 texel2 = texture(texture3, texCoord); // experimental third texture for example for killblock_i4b
            vec4 pulser = vec4(rgbgen, 1.0);
            outputColor = clamp(texel0 * texel1 * 3.0 + (texel2 * pulser), 0.0, 1.0);
            //outputColor = pulser;
            //outputColor = vec4(rgbgen, 1.0);
        }
        else {
            outputColor = clamp(texel0 * texel1 * 3.0, 0.0, 1.0);
        }
    }
    // vertice colors
    else {        
        vec4 texel0 = texture(texture1, texCoord);
        outputColor = texel0 * color * 2.0;
    }
}