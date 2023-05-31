using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Region : MonoBehaviour {

    [SerializeField]
    private uint regionId;
    [SerializeField]
    private List<Region> neighbours = new List<Region>();
    [SerializeField]
    private Vector3 regionCenter;
    [SerializeField]
    private List<Edge> frontiers = new List<Edge>();
    [NonSerialized]
    private List<Region> adjacentRegions = new List<Region>();

    public void Build(RegionDescriptor region) {
        regionId = region.id;
        frontiers = region.frontiers.Select(edge => new Edge { from = MapManager.Instance.GetScaledPosition2D(edge.from), to = MapManager.Instance.GetScaledPosition2D(edge.to) }).ToList();
        regionCenter = MapManager.Instance.GetScaledPosition3D(region.center);

        transform.SetPositionAndRotation(regionCenter, Quaternion.identity);
    }

    public void AddAdjacentRegion(Region region) {
        adjacentRegions.Add(region);
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(regionCenter, new Vector3(5f, 5f, 5f));

        Gizmos.color = Color.black;
        frontiers.ForEach(frontier => {
            Gizmos.DrawLine(new Vector3(frontier.from.x, 0f, frontier.from.y), new Vector3(frontier.to.x, 0f, frontier.to.y));
        });
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.blue;
        adjacentRegions.ForEach(region => {
            Gizmos.DrawLine(regionCenter, region.regionCenter);
        });
    }

}
