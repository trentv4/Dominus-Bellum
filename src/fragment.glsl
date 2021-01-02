#version 330 core
in vec2 uv;
in vec4 color;

uniform sampler2D map_diffuse;
uniform sampler2D map_gloss;
uniform sampler2D map_ao;
uniform sampler2D map_normal;
uniform sampler2D map_height;

out vec4 gl_FragColor;

void main()
{
	gl_FragColor = texture(map_diffuse, uv);
}