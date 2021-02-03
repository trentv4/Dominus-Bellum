#version 330 core
out vec2 uv;

void main() {
	float x = -1.0 + float((gl_VertexID & 1) << 2);
	float y = -1.0 + float((gl_VertexID & 2) << 1);
	uv.x = (x + 1.0) * 0.5;
	uv.y = (y + 1.0) * 0.5;
	gl_Position = vec4(x, y, 0, 1);
}

<split>

#version 330 core
struct Light {
	vec3 position;
	vec3 color;
	vec3 direction;
	float strength;
};

in vec2 uv;

uniform sampler2D gPosition; 
uniform sampler2D gNormal; 
uniform sampler2D gAlbedoSpec; 
uniform vec3 cameraPosition;
uniform Light lights[16];

out vec4 FragColor;

void main() {
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
	// Ambient
	HDR += albedo * 0.1;

	// Processing all lights in the scene
	for(int i = 0; i < 16; i++) {
		float strength = lights[i].strength;
		float attenuation = 1 / pow(((distance(position, xyz) / 6) + 1), 2);
		if(strength == 0.0 || attenuation < 0.001) {
			continue;
		}

		vec3 position = lights[i].position;
		vec3 color = lights[i].color;
		vec3 direction = lights[i].direction;


		vec3 result = vec3(0.0);

		vec3 lightDirection = normalize(position - xyz);
		vec3 viewDirection = normalize(cameraPosition - xyz);
		vec3 halfwayDirection = normalize(lightDirection + viewDirection);

		// Diffuse
		result += max(dot(normal, lightDirection), 0.0) * albedo * ao * color;
		// Specularity
		result += gloss * color * pow(max(dot(normal, halfwayDirection), 0.0), 32);
		// Strength
		result *= strength;
		// Attenuation
		result *= attenuation;
		// Spotlight attenuation
		if(direction != vec3(0)) {
			result *= max(pow(dot(lightDirection, normalize(direction)), 8), 0);
		}

		HDR += result;
	}

	FragColor = vec4(HDR, 1.0);
}