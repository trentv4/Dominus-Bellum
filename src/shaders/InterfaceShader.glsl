#version 330 core
layout(location=0) in vec2 _position;
layout(location=1) in vec2 _uv;

uniform mat4 model;
uniform mat4 perspective;

out vec2 uv;

void main() {
	vec4 newPosition = vec4(_position.x, _position.y, 1, 1) * model * perspective;
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
uniform bool isFont;

out vec4 FragColor;

void drawTextPath() {
	vec3 sdf = texture(elementTexture, uv).rgb;
	// This is a shortened expression for "median"
	float signedDistance = max(min(sdf.r, sdf.g), min(max(sdf.r, sdf.g), sdf.b));
	// Remaps to [0, 1]
	float screenPxDistance = 2 * (signedDistance - 0.5);
	float glyphOpacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);
	FragColor = mix(vec4(0,0,0,0), vec4(1,1,1,1), glyphOpacity * 1);
	if(glyphOpacity < 0.1)
		discard;
}

void drawTexturedQuadPath() {
	vec4 color = texture(elementTexture, uv);
	if(color.w < 0.1)
		discard;
	FragColor = color;
	gl_FragDepth = depth;
}

void main() {
	if(isFont) {
		drawTextPath();
	} else {
		drawTexturedQuadPath();
	}
}
