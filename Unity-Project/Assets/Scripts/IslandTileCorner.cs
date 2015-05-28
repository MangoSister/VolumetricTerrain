//PGRTerrain: Procedural Generation and Rendering of Terrain
//DH2323 Course Project in KTH
//IslandTileCorner.cs
//Yang Zhou: yanzho@kth.se
//Yanbo Huang: yanboh@kth.se
//Huiting Wang: huitingw@kth.se
//2015.5

using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;

namespace PGRTerrain.Generation
{
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
            adjacent = new HashSet<IslandTileCorner>();
            protrudes = new HashSet<VoronoiEdge>();
            touches = new HashSet<IslandTile>();
            position = p;
            position.data[0] = (int)position.data[0];
            position.data[1] = (int)position.data[1];
            elevation = float.MaxValue;
            Index[p] = this;
        }
    }
}