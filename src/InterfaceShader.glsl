#version 330 core
layout(location=0) in vec2 _position;
layout(location=1) in vec2 _uv;

uniform mat4 model;

out vec2 uv;

void main() {
	vec4 newPosition = vec4(_position.x, _position.y, 1, 1) * model;
	newPosition.z = -1;
	// -1: foreground
	gl_Position = newPosition;
	uv = _uv;
}

<split>

#version 330 core
in vec2 uv;

uniform sampler2D elementTexture;
uniform float depth;

out vec4 FragColor;

void main() {
	vec4 color = texture(elementTexture, uv);
	if(color.w < 0.1)
		discard;
	FragColor = color;
	gl_FragDepth = depth;
}
