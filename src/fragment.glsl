#version 330 core
out vec4 outColor;

in vec2 uv;
in vec3 normal;
in vec4 color;

void main()
{
	outColor = color;
}