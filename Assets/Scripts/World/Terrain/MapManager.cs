using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class MapManager : Singleton<MapManager> {

    private float minX = 0.0f;
    private float maxX = 1000.0f;
    private float minY = 0.0f;
    private float maxY = 1000.0f;
    private float offsetX = -500.0f;
    private float offsetY = -500.0f;

	/* Visualization variables. */

	public bool showRegionCenters = true;
	public bool showTriangleGraph = true;
	public bool showRegionFrontiers = true;

	private List<Vector2> regionCenters = new List<Vector2>();
	private List<Triangle> voronoiTriangles = new List<Triangle>();
	private Dictionary<Vector2, List<Edge>> regionBoundaries = new Dictionary<Vector2, List<Edge>>();

	[SerializeField]
	private GameObject regionPrefab;
	[SerializeField]
	private TextAsset regionJson;
	[SerializeField]
	private Transform regionParent;

	private void Start() {
		InstantiateRegions();
	}

	public void InstantiateRegions() {
		Debug.Log("Region graph computation started.");

		regionCenters.Clear();
		voronoiTriangles.Clear();
		regionBoundaries.Clear();

		JSONNode regionNodes = JSON.Parse(regionJson.text);
		List<RegionDescriptor> descriptors = JSON.getJsonArray<RegionDescriptor>(regionNodes["regions"].ToString()).ToList();

		BuildVoronoiData(descriptors);

		while (regionParent.childCount != 0) { 
			DestroyImmediate(regionParent.GetChild(0).gameObject);
        }

		descriptors.ForEach(descriptor => {
			/* Instantiation of actual Unity game objects if needed here with all the region informations stored : */
			Region region = Instantiate(regionPrefab, regionParent).GetComponent<Region>();
			region.Build(descriptor);
		});

		Debug.Log("Region graph computation finished.");
	}

	#region Terrain Calculations

	public Vector3 GetScaledPosition3D(Vector2 position) {
        float x = (position.x * (maxX - minX)) + offsetX;
        float y = (position.y * (maxY - minY)) + offsetY;
        float z = getHeightByCoordinates(new Vector2(x, y));
        return new Vector3(x, z, y);
    }

    public Vector2 GetScaledPosition2D(Vector2 position) {
        float x = (position.x * (maxX - minX)) + offsetX;
        float y = (position.y * (maxY - minY)) + offsetY;
        return new Vector2(x, y);
    }

    private float getHeightByCoordinates(Vector2 coordinates) {
        return getTerrainByCoordinates(coordinates).SampleHeight(new Vector3(coordinates.x, 0.0f, coordinates.y));
    }

    private Terrain getTerrainByCoordinates(Vector2 coordinates) {
        Terrain[] terrains = Terrain.activeTerrains;
        Vector3 position = new Vector3(coordinates.x, 0.0f, coordinates.y);
        float minValue = terrains.Min(terrain => (terrain.GetPosition() - position).sqrMagnitude);
        Terrain result = terrains.Where(terrain => (terrain.GetPosition() - position).sqrMagnitude == minValue).First();
        return result;
    }

    #endregion

    #region Voronoi Calculations

    private void BuildVoronoiData(List<RegionDescriptor> descriptors) {
		Dictionary<Vector2, RegionDescriptor> regionMap = new Dictionary<Vector2, RegionDescriptor>();
		descriptors.ForEach(descriptor => regionMap.Add(new Vector2(descriptor.x, descriptor.y), descriptor));

		Dictionary<Vector2, List<Vector2>> adjacentRegions = new Dictionary<Vector2, List<Vector2>>();

		regionCenters = regionMap.Keys.ToList();

		/* Triangle englobant. */
		Vector2 startingA = new Vector2(0.0f, 0.0f);
		Vector2 startingB = new Vector2(3.0f, 0.0f);
		Vector2 startingC = new Vector2(0.0f, 3.0f);
		Triangle startingTriangle = GetTriangle(startingA, startingB, startingC);

		Dictionary<Edge, List<Triangle>> adjacentTriangles = new Dictionary<Edge, List<Triangle>>();

		voronoiTriangles.Add(startingTriangle);

		regionCenters.ForEach(point => {
			List<Triangle> dirtyTriangles = new List<Triangle>();

			voronoiTriangles.ForEach(triangle => {
				if (IsPointInsideCircle(triangle.circonscrit, point)) {
					dirtyTriangles.Add(triangle);
				}
			});

			List<Edge> polygon = new List<Edge>();

			dirtyTriangles.ForEach(triangle => {
				GetTriangleEdges(triangle).ForEach(edge => {
					if (!dirtyTriangles.Where(t => !t.Equals(triangle)).Select(t => GetTriangleEdges(t)).SelectMany(e => e).Contains(edge)) {
						polygon.Add(edge);
					}
				});
			});

			dirtyTriangles.ForEach(triangle => {
				voronoiTriangles.Remove(triangle);
			});

			polygon.ForEach(edge => {
				Triangle addedTriangle = GetTriangle(edge.from, edge.to, point);
				voronoiTriangles.Add(addedTriangle);
			});
		});

		List<Triangle> invalidTriangles = new List<Triangle>();

		voronoiTriangles.ForEach(triangle => {
			if (IsVertexFromTriangle(triangle, startingA) || IsVertexFromTriangle(triangle, startingB) || IsVertexFromTriangle(triangle, startingC)) {
				invalidTriangles.Add(triangle);
			}

			if (IsPointOutsideMap(triangle.circonscrit.center)) {
				invalidTriangles.Add(triangle);
			}
		});

		invalidTriangles.ForEach(triangle => voronoiTriangles.Remove(triangle));

		List<Edge> edges = voronoiTriangles.Select(triangle => GetTriangleEdges(triangle)).SelectMany(edge => edge).Distinct().ToList();

		edges.ForEach(edge => {
			adjacentTriangles[edge] = voronoiTriangles.Where(triangle => GetTriangleEdges(triangle).Contains(edge)).ToList();
		});

		regionCenters.ForEach(point => {
			List<Edge> linkedEdges = edges.Where(edge => edge.from == point || edge.to == point).ToList();
			regionBoundaries.Add(point, new List<Edge>());
			linkedEdges.ForEach(edge => {
				if (adjacentTriangles[edge].Count == 2) {
					regionBoundaries[point].Add(GetEdge(adjacentTriangles[edge][0].circonscrit.center, adjacentTriangles[edge][1].circonscrit.center));
				} else {
					Vector2 edgeCenter = new Vector2((edge.from.x + edge.to.x) / 2f, (edge.from.y + edge.to.y) / 2f);
					Vector2 mapEdgeIntersection = new Vector2(100.0f, 100.0f);
					Vector2 direction = IsObtuse(adjacentTriangles[edge][0]) && IsTriangleLongestEdge(adjacentTriangles[edge][0], edge) ? 
						adjacentTriangles[edge][0].circonscrit.center - edgeCenter : 
						edgeCenter - adjacentTriangles[edge][0].circonscrit.center;

					GetClosestMapEdgesFromDirection(direction).ToList().ForEach(mapEdge => {
						Vector2 crossing = GetIntersection(GetEdge(adjacentTriangles[edge][0].circonscrit.center, edgeCenter), mapEdge);

						if (GetDistance(edgeCenter, crossing) < GetDistance(edgeCenter, mapEdgeIntersection)) {
							mapEdgeIntersection = crossing;
						}
					});

					regionBoundaries[point].Add(GetEdge(adjacentTriangles[edge][0].circonscrit.center, mapEdgeIntersection));
				}
			});
		});

		regionCenters.ForEach(point => {
			adjacentRegions.Add(point, voronoiTriangles
				.Where(triangle => IsVertexFromTriangle(triangle, point))
				.Select(triangle => triangle.vertices)
				.SelectMany(p => p)
				.Where(vertex => !vertex.Equals(point))
				.Distinct()
				.ToList());
		});

		regionCenters.ForEach(point => {
			regionMap[point].center = point;
			regionMap[point].adjacentRegion = adjacentRegions[point].Select(point => regionMap[point]).ToList();
			regionMap[point].frontiers = regionBoundaries[point];
		});

	}

	private Triangle GetTriangle(Vector2 a, Vector2 b, Vector2 c) {
		Triangle result = new Triangle();
		result.vertices = new List<Vector2> { a, b, c };
		result.circonscrit = GetCircumscribedCircle(a, b, c);
		return result;
	}

	private Edge GetEdge(Vector2 a, Vector2 b) {
		return new Edge { from = a, to = b };
	}

	private Circle GetCircumscribedCircle(Vector2 a, Vector2 b, Vector2 c) {
		Circle result = new Circle();
		result.center = GetCircumcenter(a, b, c);
		result.radius = GetDistance(a, result.center);
		return result;
	}

	private float GetDistance(Vector2 a, Vector2 b) {
		return Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
	}

	private Vector2 GetCircumcenter(Vector2 a, Vector2 b, Vector2 c) {
		double ad = a.x * a.x + a.y * a.y;
		double bd = b.x * b.x + b.y * b.y;
		double cd = c.x * c.x + c.y * c.y;
		double d = 2 * (a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y));
		return new Vector2(
			(float)(1 / d * (ad * (b.y - c.y) + bd * (c.y - a.y) + cd * (a.y - b.y))),
			(float)(1 / d * (ad * (c.x - b.x) + bd * (a.x - c.x) + cd * (b.x - a.x)))
		);
	}

	private List<Edge> GetTriangleEdges(Triangle triangle) {
		List<Edge> result = new List<Edge>();
		result.Add(new Edge { from = triangle.vertices[0], to = triangle.vertices[1] });
		result.Add(new Edge { from = triangle.vertices[1], to = triangle.vertices[2] });
		result.Add(new Edge { from = triangle.vertices[2], to = triangle.vertices[0] });
		return result;
	}

	private bool IsPointInsideCircle(Circle circle, Vector2 point) {
		return GetDistance(circle.center, point) <= circle.radius;
	}

	private bool IsVertexFromTriangle(Triangle triangle, Vector2 point) {
		return point.Equals(triangle.vertices[0]) || point.Equals(triangle.vertices[1]) || point.Equals(triangle.vertices[2]);
	}

	private bool IsPointOutsideMap(Vector2 point) {
		return point.x < 0.0f || point.x > 1.0f || point.y < 0.0f || point.y > 1.0f;
	}

	private bool IsObtuse(Triangle triangle) {
		bool result = false;
		
		triangle.vertices.ToList().ForEach(vertex => {
			Vector2 direction1 = triangle.vertices.ToList().Where(v => v != vertex).ToList()[0] - vertex;
			Vector2 direction2 = triangle.vertices.ToList().Where(v => v != vertex).ToList()[1] - vertex;

			if (Vector2.Angle(direction1, direction2) > 90) {
				result = true;
            }
		});

		return result;
    }

	private bool IsTriangleLongestEdge(Triangle triangle, Edge edge) {
		return GetTriangleEdges(triangle).Max(e => GetDistance(e.from, e.to)) == GetDistance(edge.from, edge.to);
    }

	private Vector2 GetIntersection(Edge e1, Edge e2) {
		float tmp = (e2.to.x - e2.from.x) * (e1.to.y - e1.from.y) - (e2.to.y - e2.from.y) * (e1.to.x - e1.from.x);
		float mu = ((e1.from.x - e2.from.x) * (e1.to.y - e1.from.y) - (e1.from.y - e2.from.y) * (e1.to.x - e1.from.x)) / tmp;

		return new Vector2(
			e2.from.x + (e2.to.x - e2.from.x) * mu,
			e2.from.y + (e2.to.y - e2.from.y) * mu
		);
	}

	private List<Edge> GetClosestMapEdgesFromDirection(Vector2 direction) {
		List<Edge> result = new List<Edge>();
		
		if (direction.x >= 0.0f) {
			result.Add(Edge.EAST_EDGE);
        } else {
			result.Add(Edge.WEST_EDGE);
		}

		if (direction.y >= 0.0f) {
			result.Add(Edge.NORTH_EDGE);
		} else {
			result.Add(Edge.SOUTH_EDGE);
		}

		return result;
	}

	#endregion

	private void OnDrawGizmos() {
		if (showRegionCenters) {
			Gizmos.color = Color.red;

			regionCenters.ForEach(center => {
				Gizmos.DrawCube(GetScaledPosition3D(center), new Vector3(5f, 5f, 5f));
			});
		}

		if (showTriangleGraph) {
			Gizmos.color = Color.blue;

			voronoiTriangles.ForEach(triangle => {
				Gizmos.DrawLine(GetScaledPosition3D(triangle.vertices[0]), GetScaledPosition3D(triangle.vertices[1]));
				Gizmos.DrawLine(GetScaledPosition3D(triangle.vertices[1]), GetScaledPosition3D(triangle.vertices[2]));
				Gizmos.DrawLine(GetScaledPosition3D(triangle.vertices[2]), GetScaledPosition3D(triangle.vertices[0]));
			});
		}

		if (showRegionFrontiers) {
			Gizmos.color = Color.black;

			regionBoundaries.Keys.ToList().ForEach(point => {
				regionBoundaries[point].ForEach(edge => {
					Gizmos.DrawLine(GetScaledPosition3D(edge.from), GetScaledPosition3D(edge.to));
				});
			});
        }
	}

}

[Serializable]
public struct Edge {
	public Vector2 from;
	public Vector2 to;

	public static Edge NORTH_EDGE = new Edge { from = new Vector2(0.0f, 1.0f), to = new Vector2(1.0f, 1.0f) };
	public static Edge SOUTH_EDGE = new Edge { from = new Vector2(0.0f, 0.0f), to = new Vector2(1.0f, 0.0f) };
	public static Edge WEST_EDGE = new Edge { from = new Vector2(0.0f, 0.0f), to = new Vector2(0.0f, 1.0f) };
	public static Edge EAST_EDGE = new Edge { from = new Vector2(1.0f, 1.0f), to = new Vector2(1.0f, 0.0f) };

	public override bool Equals(object obj) {
		return obj is Edge e && ((e.from == this.from && e.to == this.to) || (e.from == this.to && e.to == this.from));
	}

	public override int GetHashCode() {
		return HashCode.Combine(from, to);
	}
}

[Serializable]
public struct Triangle {
	public List<Vector2> vertices;
	public Circle circonscrit;
}

[Serializable]
public struct Circle {
	public Vector2 center;
	public float radius;
}