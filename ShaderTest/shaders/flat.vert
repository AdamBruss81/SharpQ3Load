void main(void)
{
	vec4 v = vec4(gl_Vertex);	

	v.y = 0.0; // I don't understand why setting y to zero here produces the effect it does
	// back in the code I'm rotating about the y axis. so I'd expect to set z or x to zero here to have the desired effect
	// but instead setting y does it. yet another thing that doesn't make sense initially with opengl/glsl
	
	gl_Position = gl_ModelViewProjectionMatrix * v;
}