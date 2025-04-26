shader_type spatial;

uniform vec4 edge_color: source_color;

//=========================== HEX LIB ============================
#include "res://lib/shaders/hexlib.gdshaderinc"

vec3 srgb_to_linear(vec3 color) {
	return color * (color * (color * 0.305306011 + 0.682171111) + 0.012522878);
}

void vertex() {
}

void fragment() {
	float distance = hex_distance(UV);

	ALBEDO = srgb_to_linear(COLOR.rgb);
	ROUGHNESS = 1.;
	METALLIC = 0.1;

	if (distance < 0.008) {
		ALBEDO = mix(ALBEDO, edge_color.rgb, edge_color.a);
	}
}