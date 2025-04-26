shader_type spatial;
render_mode unshaded;

void vertex() {
}

void fragment() {
	ALBEDO = vec3(UV, 1.);
}
