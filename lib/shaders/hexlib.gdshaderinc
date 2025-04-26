//=========================== MATH CONSTANTS =====================

const float hex_radius = 0.5;

//            1
//          /   \
//         2     0
//         |  *  |
//         3     5
//          \   /
//            4

const float angle_offset = PI / 6.0;
const float angle_step = PI / 3.0;

const float[] angles = {
  angle_step * 0.0 + angle_offset,
  angle_step * 1.0 + angle_offset,
  angle_step * 2.0 + angle_offset,
  angle_step * 3.0 + angle_offset,
  angle_step * 4.0 + angle_offset,
  angle_step * 5.0 + angle_offset
};

const vec2[] normalized_vertices = {
	vec2(cos(angles[0]), sin(angles[0])),
  vec2(cos(angles[1]), sin(angles[1])),
  vec2(cos(angles[2]), sin(angles[2])),
  vec2(cos(angles[3]), sin(angles[3])),
  vec2(cos(angles[4]), sin(angles[4])),
  vec2(cos(angles[5]), sin(angles[5]))
};

float point_segment_distance(vec2 vertexa, vec2 vertexb, vec2 point) {
	float l2 = dot(vertexb - vertexa, vertexb - vertexa);
	float t = clamp(dot(point - vertexa, vertexb - vertexa) / l2, 0.0, 1.0);
	return length(point - (vertexa + t * (vertexb - vertexa)));
}

//           / \
//          2   1
//        |       |
//        3       0
//        |       |
//          4   5
//           \ /

float calc_edge_distance(vec2 pixel, int edge_index) {
  int index_a = edge_index;
  int index_b = edge_index - 1;

  if (edge_index == 0) {  index_b = 5; }

	return point_segment_distance(
		normalized_vertices[index_a]       * hex_radius + vec2(0.5, 0.5),
		normalized_vertices[index_b] * hex_radius + vec2(0.5, 0.5),
		pixel
	) / hex_radius;
}

float[6] calc_edge_distances(vec2 pixel) {
	float[6] distances;
	for (int i = 0; i < 6; i++) {
		distances[i] = calc_edge_distance(pixel, i);
	}
	return distances;
}

float hex_distance(vec2 pixel) {
	float min_distance = 1000000.0;
	for (int i = 0; i < 6; i++) {
		min_distance = min(min_distance, calc_edge_distance(pixel, i));
	}
	return min_distance;
}