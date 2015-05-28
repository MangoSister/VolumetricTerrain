//PGRTerrain: Procedural Generation and Rendering of Terrain
//DH2323 Course Project in KTH
//TerrainEngine.cs
//Yang Zhou: yanzho@kth.se
//Yanbo Huang: yanboh@kth.se
//Huiting Wang: huitingw@kth.se
//2015.5

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using PGRTerrain.Generation;

namespace PGRTerrain.Render
{
    /// <summary>
    /// TerrainEngine combines the terrain generation and voxel terrain rendering
    /// It uses the data generated to feed the voxel terrain
    /// </summary>
    public class TerrainEngine : MonoBehaviour
    {
        private Island _island; //terrain generation
        private VoxelTerrain _voxelTerrain; //terrain rendering
        private List<GameObject> _riverObjs; //a list to hold river obejcts

        //compute shaders for voxel terrain
        public ComputeShader _vtShaderNormal;
        public ComputeShader _vtShaderCollecTriNum;
        public ComputeShader _vtShaderMarchingCube;
        
        [HideInInspector]
        public Material _vtMaterial; //terrain material
        public Material _riverMaterial; //river material

        /// <summary>
        /// This function initiates the whole generation
        /// </summary>
        /// <param name="mapWidth">heightmap/world size (width)</param>
        /// <param name="mapHeight">heightmap/world size (height)</param>
        /// <param name="maxElevation">maximum elevation</param>
        /// <param name="mapRelaxTime">iteration time of Lloyd relaxation</param>
        /// <param name="mapPolygonNum">the number of voronoi cell</param>
        /// <param name="riverNum">the maximum number of rivers</param>
        /// <param name="mainStreamLengthRatio">the largest possible river length / max(mapWidth, mapHeight)</param>
        /// <param name="subStreamLengthRatio">the largest possible subbranch length / the largest possible river length</param>
        /// <param name="riverSplitFreq">The probability of a river branch splitting into two sub branches</param>
        /// <param name="voxelScale">the size of each voxel cell</param>
        /// <param name="seed">seed for random number generator</param>
        public void Init(int mapWidth, int mapHeight, float maxElevation, 
                        int mapRelaxTime, int mapPolygonNum, 
                        int riverNum, float mainStreamLengthRatio, float subStreamLengthRatio, float riverSplitFreq,
                        int voxelScale, 
                        int seed)
        {
            if (_voxelTerrain != null)
            {
                _voxelTerrain.Free();
            }
            if (_riverObjs != null)
            {
                foreach (var river in _riverObjs)
                    Destroy(river);
                _riverObjs.Clear();
            }
            _island = new Island(mapWidth, mapHeight, mapRelaxTime, 
                                mapPolygonNum, riverNum, maxElevation, 
                                mainStreamLengthRatio, subStreamLengthRatio, riverSplitFreq,
                                seed);

            _voxelTerrain = new VoxelTerrain();
            _voxelTerrain._transform = transform;
            _voxelTerrain._shaderNormal = _vtShaderNormal;
            _voxelTerrain._shaderCollectTriNum = _vtShaderCollecTriNum;
            _voxelTerrain._shaderMarchingCube = _vtShaderMarchingCube;
            _voxelTerrain._material = _vtMaterial;

            _voxelTerrain._voxelScale = voxelScale;
            _voxelTerrain._width = Mathf.CeilToInt(mapWidth / (float)voxelScale / (float)VoxelTerrain.blockSize) * VoxelTerrain.blockSize;
            _voxelTerrain._height = Mathf.CeilToInt(mapHeight / (float)voxelScale / (float)VoxelTerrain.blockSize) * VoxelTerrain.blockSize;
            _voxelTerrain._elevation = Mathf.CeilToInt(maxElevation / (float)voxelScale / (float)VoxelTerrain.blockSize) * VoxelTerrain.blockSize;  
            _voxelTerrain.TerrainOrigin = Vector3.zero;

            CreateControlMap();
            _voxelTerrain.Init();

            _voxelTerrain.InsertModifier(new IslandModifier(_island, mapWidth / voxelScale, mapHeight / voxelScale, true));

            //extract rivers
            _riverObjs = new List<GameObject>();
            var rivers = _island.rivers;
            foreach (var river in rivers)
            {
                RiverRenderer riverRenderer = new RiverRenderer(transform, _riverMaterial, _island, river);

                _riverObjs.AddRange(riverRenderer.GenerateRiverObjects());
                var modifiers = riverRenderer.GenerateModifier();
                foreach (var m in modifiers)
                    _voxelTerrain.InsertModifier(m);
            }

        }

        /// <summary>
        /// Set splatmap based on information given by terrain generation routine
        /// </summary>
        private void CreateControlMap()
        {   
            _voxelTerrain._matControlFineness = 4;
            var matControlFineness = Mathf.Clamp(_voxelTerrain._matControlFineness, 1, 8);
            int controlMapSize = matControlFineness * 16;

            Color[] map1 = new Color[controlMapSize * controlMapSize * controlMapSize];
            Color[] map2 = new Color[controlMapSize * controlMapSize * controlMapSize];

            LibNoise.Billow control = new LibNoise.Billow();
            control.Frequency = 0.02;
            control.OctaveCount = 1;
            
            for (int x = 0; x < controlMapSize; x++)
                for (int z = 0; z < controlMapSize; z++)
                {
                    float u = (float)x / (float)controlMapSize * (float)_island.width;
                    float v = (float)z / (float)controlMapSize * (float)_island.height;
                    var biomeVal = _island.GetBiome(new BenTools.Mathematics.Vector(u, v));
                    Color data1, data2;
                    data1 = new Color(biomeVal[BiomeType.Beach],
                                        biomeVal[BiomeType.GrassLand],
                                        biomeVal[BiomeType.RainForest],
                                        biomeVal[BiomeType.BareRock]);
                    data2 = new Color(biomeVal[BiomeType.Snow], 0, 0, 0);

                    for (int y = 0; y < controlMapSize; y++)
                    {
                        map1[x + y * controlMapSize + z * controlMapSize * controlMapSize] = data1;
                        map2[x + y * controlMapSize + z * controlMapSize * controlMapSize] = data2;
                    }
                }

            _voxelTerrain.SetControlMap(map1, 1);
            _voxelTerrain.SetControlMap(map2, 2);
        }

        // Update is called once per frame
        void Update()
        {
            if (_voxelTerrain != null)
                _voxelTerrain.Update();
        }

        void OnDestroy()
        {
            if (_voxelTerrain != null)
                _voxelTerrain.Free();
        }

        //Manipulate the terrain by sphere modifiers
        public void ModifyTerrain(Vector3 pos, float radius, bool addOrErode)
        {
            _voxelTerrain.InsertModifier(new SphereModifier(pos, radius, addOrErode));
        }
    }
}