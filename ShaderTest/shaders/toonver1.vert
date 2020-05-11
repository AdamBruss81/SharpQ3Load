uniform vec3 lightdir;
	
varying float intensity;

void main()
{	
	// this is pretty cool and scarily powerful
	// this gets called for every vertex in the teapot. I didn't fully grasp this until I changed the colors dramatically in the frag shader.
	// so gl_Normal is the normal of a vertex

	vec3 tempNormal = vec3(gl_Normal.x, gl_Normal.y, gl_Normal.z);
	intensity = dot(lightdir, tempNormal);
	
	gl_Position = ftransform();
}