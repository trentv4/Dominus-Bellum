#version 330 core
out vec2 uv;
out vec3 position;

void main()
{
	float x = -1.0 + float((gl_VertexID & 1) << 2);
	float y = -1.0 + float((gl_VertexID & 2) << 1);
	uv.x = (x + 1.0) * 0.5;
	uv.y = (y + 1.0) * 0.5;
	position = vec3(x, y, 0);
	gl_Position = vec4(position, 1);
}