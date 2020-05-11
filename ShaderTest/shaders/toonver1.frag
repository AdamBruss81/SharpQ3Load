varying float intensity;

void main()
{
	vec4 color;
	
	if (intensity > 0.95)
		color = vec4(1.0,0.5,0.5,1.0); // reddish
	else if (intensity > 0.5)
		color = vec4(0.6,0.3,0.3,1.0); // darker red
	else if (intensity > 0.25)
		color = vec4(0.4,0.2,0.2,1.0); // even darker red
	else
		color = vec4(0.2,0.1,0.1,1.0); // darkest red

	gl_FragColor = color;
}