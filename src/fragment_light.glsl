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
	FragColor = vec4(texture(gPosition, uv).r, texture(gNormal, uv).r, texture(gAlbedoSpec, uv).r, texture(gHeightAO, uv).r);
}