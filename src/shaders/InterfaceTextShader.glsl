#version 330 core
layout(location=0) in vec2 _position;
layout(location=1) in vec2 _uv;

uniform mat4 model;

out vec2 uv;

void main() {
	gl_Position = vec4(_position.x, _position.y, 1, 1) * model;
	uv = _uv;
}

<split>

#version 330 core
in vec2 uv;

uniform sampler2D elementTexture;

out vec4 FragColor;

void main() {
	vec4 color = texture(elementTexture, uv);

	FragColor = vec4(0,1,0,1);
}
