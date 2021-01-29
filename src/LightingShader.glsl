#version 330 core
out vec2 uv;

void main()
{
	float x = -1.0 + float((gl_VertexID & 1) << 2);
	float y = -1.0 + float((gl_VertexID & 2) << 1);
	uv.x = (x + 1.0) * 0.5;
	uv.y = (y + 1.0) * 0.5;
	gl_Position = vec4(x, y, 0, 1);
}

<split>

#version 330 core
in vec2 uv;

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

	vec3 HDR = vec3(0.0);

	// Light data
	vec3 Position = vec3(20, 5, 6);
	vec3 Color = vec3(1.0, 1.0, 1.0);

	vec3 lightDirection = normalize(Position - xyz);
	HDR += albedo * 0.1;
	HDR += max(dot(normal, lightDirection), 0.0) * albedo * ao;

	vec3 LDR = pow(HDR / (HDR + vec3(1.0)), vec3(1.0 / 2.2));

	FragColor = vec4(HDR, 1.0);
}