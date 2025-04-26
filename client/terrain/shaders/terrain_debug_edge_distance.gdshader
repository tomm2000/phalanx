shader_type spatial;


//=========================== HEX LIB ============================
#include "res://lib/shaders/hexlib.gdshaderinc"

void vertex() {
}

void fragment() {
  ALBEDO = vec3(hex_distance(UV));
}