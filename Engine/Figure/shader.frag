#version 330

out vec4 outputColor;

in vec2 texCoord;
in vec2 texCoord2;
in vec4 color;

uniform sampler2D texture1;
uniform sampler2D texture2;

void main()
{ 
    if (texCoord2.x != -1.0) {        
        vec4 texel0 = texture(texture1, texCoord);
        vec4 texel1 = texture(texture2, texCoord2);
        outputColor = clamp(texel0 * texel1 * 3.0, 0.0, 1.0);

        //outputColor = texel0;
        //outputColor = texel0 * texel1;

        // just base texture
        //outputColor = texture(texture1, texCoord);

        // add vert color to base texture and multiply by base (no lightmap)
        /*vec4 texel0 = texture(texture1, texCoord);
        vec4 tempColor = clamp(texel0 + color, 0.0, 1.0);
        tempColor *= texture(texture1, texCoord);
        outputColor = tempColor;*/

        // just vertice color
        //outputColor = vec4(color, 1.0);

        // just lightmap
        //outputColor = texture(texture2, texCoord2);
    }
    else {
        vec4 texel0 = texture(texture1, texCoord);
        outputColor = texel0 * color * 2.0;

        //vec4 texColor = texture(texture1, TexCoords);
        //if (texel0.a < 0.08) 
            //discard;
        //FragColor = texColor;

        //texel0.a = 0.3;
        //outputColor = vec4(texel0 * color * 2.0);
        //outputColor = texel0;
    }
}