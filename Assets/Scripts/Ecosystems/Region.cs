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
    public Vector3 regionCenter;
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

}
