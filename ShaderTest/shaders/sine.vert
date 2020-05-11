uniform float time;

void main(void)
{
	// get current vertices
	vec4 v = vec4(gl_Vertex);
	
	// set y to new value
	v.y = sin(time * v.x);

	// change position to model view times v
	gl_Position = gl_ModelViewProjectionMatrix * v;
}