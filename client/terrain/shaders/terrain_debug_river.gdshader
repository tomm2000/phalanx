shader_type spatial;

uniform vec4 edge_color: source_color;

//=========================== HEX LIB ============================
#include "res://lib/shaders/hexlib.gdshaderinc"

vec3 srgb_to_linear(vec3 color) {
	return color * (color * (color * 0.305306011 + 0.682171111) + 0.012522878);
}

void vertex() {
	COLOR = vec4(CUSTOM0.r, 0, 0, 0);
}

void fragment() {
	ALBEDO = srgb_to_linear(COLOR.rgb);
	ROUGHNESS = 1.;
	METALLIC = 0.1;
}