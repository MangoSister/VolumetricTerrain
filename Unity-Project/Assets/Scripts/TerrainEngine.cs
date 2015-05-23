using UnityEngine;
using System;
using System.Collections;
using PCGTerrain.Generation;

namespace PCGTerrain.Render
{
    public class TerrainEngine : MonoBehaviour
    {
        private Island _island;
        private VoxelTerrain _voxelTerrain;
        private RiverRenderer _riverRenderer;

        public ComputeShader _vtShaderNormal;
        public ComputeShader _vtShaderCollecTriNum;
        public ComputeShader _vtShaderMarchingCube;
        public Material _vtMaterial;
        public Material _riverMaterial;

        private void TestModifier()
        {
            //A plane
            //_voxelTerrain.InsertModifier(new PlaneModifier(4, Vector2.zero, Vector2.one * 16, true));
            
            //a noise function
            RidgedMultifractalModifier noise = new RidgedMultifractalModifier(0, 4, 0.06f, 2.0f, true);
            _voxelTerrain.InsertModifier(noise);

            //a sphere density function
            //Vector3 center1 = new Vector3((float)_voxelTerrain._width, (float)_voxelTerrain._height, (float)_voxelTerrain._elevation) * 0.5f;
            //float radius1 = 6f;
            //_voxelTerrain.InsertModifier(new SphereModifier(center1, radius1, true));

            //another sphere density function
            //Vector3 center2 = new Vector3((float)_width, (float)_height, (float)_elevation) * 0.5f;
            //float radius2 = 4f;
            //_modifierQueue.Enqueue(new SphereModifier(center2, radius2, false));

            //a cylinder density function
            //_voxelTerrain.InsertModifier(new CylinderModifier(new Vector3(0, 4, 8), Vector3.right, 10, 2, false));

            //a heightmap grid
            //TerrainGrid terrainInfo = new TerrainGrid(17, 9, 17, true); //sample (2d)
            //for (int x = 0; x <= 16; x++)
            //    for (int z = 0; z <= 16; z++)
            //    {
            //        terrainInfo._samples[x, z]._elevation = 8 - 0.5f * (new Vector2(x - 8, z - 8)).magnitude * Random.Range(0.8f, 1.2f);
            //    }
            //_modifierQueue.Enqueue(terrainInfo);

            //float[] sample = { 0f, 0.5f, 0.8f, 0.9f, 1f, 0.9f, 0.8f, 0.5f, 0f };
            //var m = new CrossSectionModifier(sample, new Vector3(2, 8, 8), Vector3.right, 10, new Vector2(8, 3), true);
            //m.QueryDensity(4, 13, 9);
            //_voxelTerrain.InsertModifier(m);
        }

        // Use this for initialization
        void Start()
        {
            _voxelTerrain = new VoxelTerrain();
            _riverRenderer = new RiverRenderer();

            _voxelTerrain._transform = transform;
            _voxelTerrain._shaderNormal = _vtShaderNormal;
            _voxelTerrain._shaderCollectTriNum = _vtShaderCollecTriNum;
            _voxelTerrain._shaderMarchingCube = _vtShaderMarchingCube;
            _voxelTerrain._material = _vtMaterial;

            _voxelTerrain._width = 256;
            _voxelTerrain._elevation = 16;
            _voxelTerrain._height = 256;
            _voxelTerrain.TerrainOrigin = Vector3.zero;
            _voxelTerrain._voxelScale = 2;

            _riverRenderer._transform = transform;
            _riverRenderer._waterMat = _riverMaterial;

            _voxelTerrain.Init();
            TestModifier();

            _riverRenderer.Init();   
            _riverRenderer.GenerateRiverObjects();
            var modifiers = _riverRenderer.GenerateModifier();
            foreach (var m in modifiers)
                _voxelTerrain.InsertModifier(m);
        }

        // Update is called once per frame
        void Update()
        {
            
            _voxelTerrain.Update();
        }

        void OnDestroy()
        {
            _voxelTerrain.Free();
        }
    }
}