#version 330 core
in vec2 uv;
in vec4 color;
in vec3 position;

out vec4 FragColor;

uniform sampler2D gPosition; 
uniform sampler2D gNormal; 
uniform sampler2D gAlbedoSpec; 
uniform vec3 cameraPosition;
void main()
{
	vec4 gPositionVec = texture(gPosition, uv);
	vec4 gNormalVec = texture(gNormal, uv);
	vec4 gAlbedoSpecVec = texture(gAlbedoSpec, uv);

	vec3 xyz = gPositionVec.xyz;
	vec3 normal = gNormalVec.xyz;
	vec3 albedo = gAlbedoSpecVec.xyz;
	float ao = gPositionVec.w;
	float height = gNormalVec.w;
	float gloss = gAlbedoSpecVec.w;

	// Light data
	vec3 Position = vec3(1.0, 0.0, 1.0);
	vec3 Color = vec3(1.0, 1.0, 1.0) * 0.7;

	vec3 lighting = albedo * 0.2; // Ambient
	vec3 lightDirection = normalize(Position - xyz);
	vec3 viewDirection = normalize(cameraPosition - xyz);
	vec3 halfwayDirection = normalize(lightDirection + viewDirection);

	// Diffuse
	lighting += max(dot(normal, lightDirection), 0.0) * albedo * Color;
	// Specular
	lighting += Color * pow(max(dot(normal, halfwayDirection), 0.0), gloss);
	lighting *= ao;

	FragColor = vec4(lighting, 1.0);
}