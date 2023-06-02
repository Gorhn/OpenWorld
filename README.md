This is a hobby project around building dynamic regions in a given planar map.

This is using two algorithms to build, from a set of coordinates on a 2D map, a set of regions similar to a Voronoi tesselation.
First, a Delaunay triangulation is built, linking region centers together, and then a Voronoi diagram is built from the triangulation.

Everything is running inside the Unity Editor, so there is no need to run the game at all. On the "Managers" Game Object, a MapManager script is available to play with it.

- Several checkboxes can show/hide different parts of the graph constructions, such as region centers, Delaunay graph and the final Voronoi result.
- One prefab can be setup to include an actual GameObject to be instantiated as the center of each region, further enhancing possibilities from the algorithm. This can also be used to monitor the activity of each region, as by selecting a given entity, one can check in the Editor various data about the region.
- A region JSON file must be referenced with the data of all of the region centers. This can be used to stabilize the map aspect, so that the centers don't move over time or to include additional informations about the region, like a name or a gameplay detail.
- Region parent is the Game Object under which the region centers prefabs will be instantiated to keep the hierarchy tidy.
- Finally, an Update button is available to rebuild the graphs whenever an important data is changed in the editor.

Some thoughts about potential applications to games :

- This type of tesselation can easily be used in many strategy games to represent regions to control. Some important data are also accessible, for example, given any region center, the Delaunay triangulation will give you every path to an adjacent region that has one shared edge.
- This is also something that can be used in an open world context, for example to separate regions by climate to specify which fauna can appear in any area, or to display a name whenever the player enters the area.
