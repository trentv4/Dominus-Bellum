#version 330 core
layout(location=0) in vec3 _position;
layout(location=1) in vec2 _uv;
layout(location=2) in vec4 _color;

uniform mat4 mvp;

out vec2 uv;
out vec4 color;
out vec3 position;

void main()
{
	gl_Position = vec4(_position, 1.0) * mvp;


	// Translates required data to fragment shader
	position = gl_Position.rgb;
	uv = _uv;
	color = _color;
}