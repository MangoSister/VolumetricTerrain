using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PCGTerrain.Generation;

namespace PCGTerrain.Render
{
    /// <summary>
    /// Data structure for terrain "heightmap" (with other infos)
    /// </summary>
    public struct TerrainSample
    {
        public float _elevation;
        public float[] _matComponents;

        public const int matComponentNum = 6;

        public TerrainSample(float elevation, int matIdx)
        {
            _elevation = elevation;
            _matComponents = new float[matComponentNum];
            _matComponents[0] = 1f;
        }
    }

    public class IslandModifier : TerrainModifier
    {
        private Island _island;
        private float[,] _heightmap;
        public Vector3 LowerBound
        { get { return new Vector3(0, float.MinValue, 0); } }

        public Vector3 UpperBound
        { get { return new Vector3(_island.width, _island._maxElevation, _island.height); } }

        public bool AddOrErode { get; set; }
        public float QueryDensity(Vector3 pos)
        {    
            //do bilinear interpolation

            //resize uv to fit heightmap
            float u = Mathf.Clamp(pos.x, 0, (float)_island.width);
            u = u / _island.width * (float)(_heightmap.GetLength(0) - 1);
            u = Mathf.Clamp(u, 0, (float)(_heightmap.GetLength(0) - 1));

            float v = Mathf.Clamp(pos.z, 0, (float)_island.height);
            v = v / _island.height * (float)(_heightmap.GetLength(1) - 1);
            v = Mathf.Clamp(v, 0, (float)(_heightmap.GetLength(1) - 1));

            int u0 = Mathf.FloorToInt(u); int u1 = Mathf.CeilToInt(u);
            int v0 = Mathf.FloorToInt(v); int v1 = Mathf.CeilToInt(v);

            float h00 = _heightmap[u0, v0];
            float h10 = _heightmap[u1, v0];
            float h01 = _heightmap[u0, v1];
            float h11 = _heightmap[u1, v1];
            
            float h0 = Mathf.Lerp(h00, h01, v - (float)v0);
            float h1 = Mathf.Lerp(h10, h11, v - (float)v0);

            float elevation = Mathf.Lerp(h0, h1, u - (float)u0);
            
            //transform to density
            return elevation - pos.y;
        }

        //only square size by now
        public IslandModifier(Island island, int resolution, bool addOrErode = true)
        {
            _island = island;
            AddOrErode = addOrErode;

            //generate a heightmap based on resolution    
            _heightmap = new float[resolution, resolution];

            for(int x = 0; x< resolution; x++)
                for(int y = 0; y <resolution; y++)
                {
                    var pos = new BenTools.Mathematics.Vector(_island.width / (float)resolution * x, _island.width / (float)resolution * y);
                    _heightmap[x, y] = _island.GetElevation(pos);
                }
        }
    }
}

