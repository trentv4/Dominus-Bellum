#version 330 core
in vec2 uv;
in vec3 normal;
in vec4 color;

uniform sampler2D texture0;

out vec4 gl_FragColor;

void main()
{
	gl_FragColor = texture(texture0, uv);
}