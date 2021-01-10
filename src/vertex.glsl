#version 330 core
layout(location=0) in vec3 _position;
layout(location=1) in vec2 _uv;
layout(location=2) in vec4 _color;

uniform mat4 modelView;
uniform mat4 perspective;

out vec2 uv;
out vec4 color;
out vec3 position;

void main()
{
	vec4 pos = vec4(_position, 1.0) * modelView;
	gl_Position = pos * perspective;

	position = vec3(pos);
	uv = _uv;
	color = _color;
}