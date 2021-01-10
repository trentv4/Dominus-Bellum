#version 330 core
in vec2 uv;
in vec4 color;
in vec3 position;

uniform sampler2D map_diffuse;
uniform sampler2D map_gloss;
uniform sampler2D map_ao;
uniform sampler2D map_normal;
uniform sampler2D map_height;

layout (location = 0) out vec3 gPosition;
layout (location = 1) out vec3 gNormal;
layout (location = 2) out vec4 gAlbedoSpec;
layout (location = 3) out vec2 gHeightAO;

void main()
{
	vec4 diffuse = texture(map_diffuse, uv);

	gPosition = position;
	gNormal = texture(map_normal, uv).rgb;
	gAlbedoSpec.rgb = (diffuse.xyz * diffuse.w) + vec3(color * (1-diffuse.w));
	gAlbedoSpec.a = texture(map_gloss, uv).r;
	gHeightAO.r = texture(map_ao, uv).r;
	gHeightAO.g = texture(map_height, uv).g;
}