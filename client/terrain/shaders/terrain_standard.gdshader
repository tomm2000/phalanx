shader_type spatial;

uniform vec4 edge_color : source_color;
uniform sampler2D noise_texture;
uniform float time_scale = 1.0; // Controls how fast the flow moves
uniform float flow_strength = 0.05; // Controls how much the UVs are offset

varying float riverness;
varying vec2 flow;
varying vec2 vertex_uv;

//=========================== HEX LIB ============================
#include "res://lib/shaders/hexlib.gdshaderinc"

vec3 srgb_to_linear(vec3 color) {
	return color * (color * (color * 0.305306011 + 0.682171111) + 0.012522878);
}

void vertex() {
	riverness = CUSTOM0.r;
    flow = CUSTOM0.gb * 2.0 - 1.0; // Correct the flow range from [0,1] to [-1,1]
	vertex_uv = UV;
}

void fragment() {
	float distance = hex_distance(UV);

	ALBEDO = srgb_to_linear(COLOR.rgb);

	if (riverness >= 0.50) {
		ROUGHNESS = 0.6;
		METALLIC = 0.3;

		// Compute moving UVs based on flow
		//vec2 flow_uv = UV + flow * (TIME * time_scale) * flow_strength;

		//vec4 normalBump = texture(noise_texture, flow_uv);
		//NORMAL += normalBump.rgb; // Scale the bump strength if needed

		//vec4 noiseValue = texture(noise_texture, flow_uv);
		//ALBEDO = noiseValue.rgb;
	} else {
		ROUGHNESS = 1.0;
		METALLIC = 0.1;
	}

	if (distance < 0.008) {
		ALBEDO = mix(ALBEDO, edge_color.rgb, edge_color.a);
	}
}
