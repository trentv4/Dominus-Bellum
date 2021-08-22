#version 330 core
layout(location=0) in vec3 _position;
layout(location=1) in vec2 _uv;
layout(location=2) in vec4 _color;
layout(location=3) in vec3 _normal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 perspective;

out vec3 position;
out vec2 uv;
out vec4 color;
out vec3 normal;

void main() {
	vec4 pos = vec4(_position, 1.0) * model;
	gl_Position = pos * view * perspective;
	position = vec3(pos);

	uv = _uv;
	color = _color;
	normal = _normal;
}

<split>

#version 330 core
in vec2 uv;
in vec4 color;
in vec3 position;
in vec3 normal;

uniform sampler2D map_diffuse;
uniform sampler2D map_gloss;
uniform sampler2D map_ao;
uniform sampler2D map_normal;
uniform sampler2D map_height;

layout (location = 0) out vec4 gPosition; // x, y, z, ambient occlusion
layout (location = 1) out vec4 gNormal; // normal X, normal Y, normal Z, height
layout (location = 2) out vec4 gAlbedoSpec; // r, g, b, gloss

void main() {
	vec4 diffuse = texture(map_diffuse, uv);
	vec4 gloss = texture(map_gloss, uv);
	vec4 ao = texture(map_ao, uv);
	vec4 normalT = texture(map_normal, uv);
	vec4 height = texture(map_height, uv);

	gPosition.xyz = position;
	gPosition.w = ao.x;
	gNormal.xyz = normalT.xyz * normal;
	gNormal.w = height.x;
	gAlbedoSpec.rgb = (diffuse.xyz * diffuse.w) + vec3(color * (1-diffuse.w));
	gAlbedoSpec.a = gloss.x;
}