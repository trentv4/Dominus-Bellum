#version 330 core
in vec2 uv;
in vec4 color;
in vec3 position;

layout (location = 0) out vec3 gPosition; // xyz
layout (location = 1) out vec3 gNormal; // qrs
layout (location = 2) out vec4 gAlbedoSpec; //rgbS
layout (location = 3) out vec2 gHeightAO; //rgbS

uniform sampler2D map_diffuse;
uniform sampler2D map_gloss;
uniform sampler2D map_ao;
uniform sampler2D map_normal;
uniform sampler2D map_height;

void main()
{
	gPosition = position;
	gNormal = texture(map_normal, uv).rgb;
	gAlbedoSpec.rgb = texture(map_diffuse, uv).rgb;
	gAlbedoSpec.a = texture(map_gloss, uv).r;
	gHeightAO.r = texture(map_ao, uv).r;
	gHeightAO.g = texture(map_height, uv).g;
}