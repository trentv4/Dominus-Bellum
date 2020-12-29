#version 330 core
layout(location=0) in vec3 _position;
layout(location=1) in vec2 _uv;
layout(location=2) in vec4 _color;
layout(location=3) in vec3 _normal;

uniform mat4 mvp;

out vec2 uv;
out vec3 normal;
out vec4 color;

void main()
{
	gl_Position = vec4(_position, 1.0) * mvp;

	// Translates required data to fragment shader
	uv = _uv;
	normal = _normal;
	color = _color;
}