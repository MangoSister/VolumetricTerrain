using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;
using Microsoft.DirectX;
public class IslandTileCorner
{
    public HashSet<IslandTileCorner> adjacent;   // Corners connected by edges
    public HashSet<VoronoiEdge> protrudes;  // Edges touching this corner
    public HashSet<IslandTile> touches;    // Tiles connected by this corner
    public Vector position;
    public float elevation = float.MaxValue;
    public int discharge = 0;//for river's discharge
    //this dictionary is for searching corner through postion
    //and I also don't wanna build a hashset when this corner is already exist. 
    public static Dictionary<Vector, IslandTileCorner> 
        Index = new Dictionary<Vector, IslandTileCorner>();
    //Constructor
    public IslandTileCorner(Vector p)
    {
        adjacent=new HashSet<IslandTileCorner>();
        protrudes = new HashSet<VoronoiEdge>();
        touches = new HashSet<IslandTile>();
        position = p;
        position.data[0] = (int)position.data[0];
        position.data[1] = (int)position.data[1];
        elevation = float.MaxValue;
        Index[p] = this;
    }
}