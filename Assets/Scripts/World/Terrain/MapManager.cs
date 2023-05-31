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
		JSONNode regionNodes = JSON.Parse(regionJson.text);
		List<RegionDescriptor> descriptors = JSON.getJsonArray<RegionDescriptor>(regionNodes["regions"].ToString()).ToList();

		BuildVoronoiData(descriptors);

		descriptors.ForEach(descriptor => {
			Region region = Instantiate(regionPrefab, regionParent).GetComponent<Region>();
			region.Build(descriptor);
		});
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
		Debug.Log("Starting Voronoi calculations.");

		Dictionary<Vector2, RegionDescriptor> regionMap = new Dictionary<Vector2, RegionDescriptor>();
		descriptors.ForEach(descriptor => regionMap.Add(new Vector2(descriptor.x, descriptor.y), descriptor));

		Dictionary<Vector2, List<Vector2>> adjacentRegions = new Dictionary<Vector2, List<Vector2>>();
		Dictionary<Vector2, List<Edge>> regionBoundaries = new Dictionary<Vector2, List<Edge>>();

		List<Vector2> points = regionMap.Keys.ToList();

		/* Triangle englobant. */
		Vector2 startingA = new Vector2(0.0f, 0.0f);
		Vector2 startingB = new Vector2(3.0f, 0.0f);
		Vector2 startingC = new Vector2(0.0f, 3.0f);
		Triangle startingTriangle = GetTriangle(startingA, startingB, startingC);

		Dictionary<Edge, List<Triangle>> adjacentTriangles = new Dictionary<Edge, List<Triangle>>();
		List<Triangle> triangulation = new List<Triangle>();

		GetTriangleEdges(startingTriangle).ForEach(edge => SetTriangleAdjacency(adjacentTriangles, edge, startingTriangle, true));
		triangulation.Add(startingTriangle);

		points.ForEach(point => {
			List<Triangle> dirtyTriangles = new List<Triangle>();

			triangulation.ForEach(triangle => {
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
				GetTriangleEdges(triangle).ForEach(edge => SetTriangleAdjacency(adjacentTriangles, edge, triangle, false));
				triangulation.Remove(triangle);
			});

			polygon.ForEach(edge => {
				Triangle addedTriangle = GetTriangle(edge.from, edge.to, point);
				GetTriangleEdges(addedTriangle).ForEach(edge => {
					SetTriangleAdjacency(adjacentTriangles, edge, addedTriangle, true);
				});
				triangulation.Add(addedTriangle);
			});
		});

		List<Triangle> invalidTriangles = new List<Triangle>();

		triangulation.ForEach(triangle => {
			if (IsVertexFromTriangle(triangle, startingA) || IsVertexFromTriangle(triangle, startingB) || IsVertexFromTriangle(triangle, startingC)) {
				invalidTriangles.Add(triangle);
			}

			if (IsPointOutsideMap(triangle.circonscrit.center)) {
				invalidTriangles.Add(triangle);
			}
		});

		invalidTriangles.ForEach(triangle => triangulation.Remove(triangle));

		/* Triangulation done. */

		Debug.Log("Voronoi calculations ended.");

		points.ForEach(point => {
			adjacentRegions.Add(point, triangulation
				.Where(triangle => IsVertexFromTriangle(triangle, point))
				.Select(triangle => triangle.vertices)
				.SelectMany(p => p)
				.Where(vertex => !vertex.Equals(point))
				.Distinct()
				.ToList());
		});

		List<Triangle> processed = new List<Triangle>();

		points.ForEach(point => {
			regionBoundaries.Add(point, new List<Edge>());
			List<Triangle> processed = new List<Triangle>();
			List<Triangle> regionTriangles = triangulation.Where(triangle => IsVertexFromTriangle(triangle, point)).ToList();
			regionTriangles.ForEach(triangle => {
				GetTriangleEdges(triangle).Select(edge => adjacentTriangles[edge]).SelectMany(t => t).Where(t => IsVertexFromTriangle(t, point)).ToList().ForEach(other => {
					if (!processed.Contains(other)) {
						regionBoundaries[point].Add(new Edge { from = triangle.circonscrit.center, to = other.circonscrit.center });
					}
				});
				processed.Add(triangle);
			});
		});

		points.Where(point => regionBoundaries[point].Any(edge => IsPointOutsideMap(edge.from) || IsPointOutsideMap(edge.to))).ToList().ForEach(point => {
			List<Edge> overflowingEdges = regionBoundaries[point].Where(edge => IsPointOutsideMap(edge.from) || IsPointOutsideMap(edge.to)).ToList();

			List<Vector2> pointsToLink = new List<Vector2>();

			overflowingEdges.ForEach(edge => {
				Vector2 pointOut;
				Vector2 pointIn;

				if (IsPointOutsideMap(edge.from)) {
					pointOut = edge.from;
					pointIn = edge.to;
				} else {
					pointOut = edge.to;
					pointIn = edge.from;
				}

				List<Edge> mapEdgesToCheck = new List<Edge>();

				if (pointOut.x < 0) {
					mapEdgesToCheck.Add(GetEdge(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 1.0f)));
				}
				if (pointOut.x > 1) {
					mapEdgesToCheck.Add(GetEdge(new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f)));
				}
				if (pointOut.y < 0) {
					mapEdgesToCheck.Add(GetEdge(new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f)));
				}
				if (pointOut.y > 1) {
					mapEdgesToCheck.Add(GetEdge(new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f)));
				}

				Vector2 newPoint = new Vector2(100.0f, 100.0f);

				mapEdgesToCheck.ForEach(mapEdge => {
					Vector2 crossing = GetIntersection(edge, mapEdge);

					if (GetDistance(pointIn, crossing) < GetDistance(pointIn, newPoint)) {
						newPoint = crossing;
					}
				});

				Edge result;

				if (IsPointOutsideMap(edge.from)) {
					result.from = newPoint;
					result.to = edge.to;
				} else {
					result.from = edge.from;
					result.to = newPoint;
				}

				regionBoundaries[point].Remove(edge);
				regionBoundaries[point].Add(result);

				pointsToLink.Add(newPoint);
			});

			if (GetBorderFromPoint(pointsToLink[0]).Equals(GetBorderFromPoint(pointsToLink[1]))) {
				regionBoundaries[point].Add(GetEdge(pointsToLink[0], pointsToLink[1]));
			} else {
				Vector2 corner = GetIntersection(GetBorderFromPoint(pointsToLink[0]), GetBorderFromPoint(pointsToLink[1]));
				regionBoundaries[point].Add(GetEdge(pointsToLink[0], corner));
				regionBoundaries[point].Add(GetEdge(corner, pointsToLink[1]));
			}

		});

		points.ForEach(point => {
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

	private void SetTriangleAdjacency(Dictionary<Edge, List<Triangle>> adjacencyMap, Edge edge, Triangle triangle, bool adjacent) {
		if (adjacent) {
			if (adjacencyMap.ContainsKey(edge)) {
				adjacencyMap[edge].Add(triangle);
			} else {
				adjacencyMap.Add(edge, new List<Triangle> { triangle });
			}
		} else {
			adjacencyMap[edge].Remove(triangle);
		}
	}

	private bool IsPointOutsideMap(Vector2 point) {
		return point.x < 0.0f || point.x > 1.0f || point.y < 0.0f || point.y > 1.0f;
	}

	private Vector2 GetIntersection(Edge e1, Edge e2) {
		float tmp = (e2.to.x - e2.from.x) * (e1.to.y - e1.from.y) - (e2.to.y - e2.from.y) * (e1.to.x - e1.from.x);
		float mu = ((e1.from.x - e2.from.x) * (e1.to.y - e1.from.y) - (e1.from.y - e2.from.y) * (e1.to.x - e1.from.x)) / tmp;

		return new Vector2(
			e2.from.x + (e2.to.x - e2.from.x) * mu,
			e2.from.y + (e2.to.y - e2.from.y) * mu
		);
	}

	private Edge GetBorderFromPoint(Vector2 point) {
		if (point.x == 0) {
			return GetEdge(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 1.0f));
		} else if (point.x == 1) {
			return GetEdge(new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f));
		} else if (point.y == 0) {
			return GetEdge(new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f));
		} else if (point.y == 1) {
			return GetEdge(new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f));
		}

		return GetEdge(new Vector2(100.0f, 100.0f), new Vector2(200.0f, 200.0f));
	}

    #endregion

}

[Serializable]
public struct Edge {
	public Vector2 from;
	public Vector2 to;

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