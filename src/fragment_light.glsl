#version 330 core
in vec2 uv;
in vec4 color;
in vec3 position;

out vec4 FragColor;

uniform sampler2D gPosition; 
uniform sampler2D gNormal; 
uniform sampler2D gAlbedoSpec; 
uniform sampler2D gHeightAO; 

void main()
{
	vec3 p = texture(gPosition, uv).rgb;
	vec3 n = texture(gNormal, uv).rgb;
	vec3 a = texture(gAlbedoSpec, uv).rgb;
	float g = texture(gAlbedoSpec, uv).a;
	vec2 hao = texture(gHeightAO, uv).rg;
	FragColor = vec4(p.x, n.x, a.x, hao.r);
}