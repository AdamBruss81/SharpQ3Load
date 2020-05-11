varying vec3 normal;

void main()
{
	normal = gl_Normal; // set varying variable for use by fragment shader

	gl_Position = ftransform();
}