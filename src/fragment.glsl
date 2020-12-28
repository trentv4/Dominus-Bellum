#version 330 core
out vec4 gl_FragColor;

in vec2 uv;
in vec3 normal;
in vec4 color;

void main()
{
	gl_FragColor = color;
}