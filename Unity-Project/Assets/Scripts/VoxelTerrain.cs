using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace PCGTerrain.Render
{
    public class VoxelTerrain
    {
        private struct CSTriangle
        {
            public Vector3 _position0;
            public Vector3 _position1;
            public Vector3 _position2;
            public Vector3 _normal0;
            public Vector3 _normal1;
            public Vector3 _normal2;
            public int _block;

            public static int stride = sizeof(float) * 3 * 6 + sizeof(int);
        };

        //x: width, y: elevation, z: height
        public int _width = 16, _elevation = 16, _height = 16; // voxel size
        private float[, ,] _voxelSamples;
        
        public const int maxSampleResolution = 1025; //sample size = voxel size + 1   
        public float voidDensity { get { return Random.Range(-2f, -1f); } }
        public float fullDensity { get { return Random.Range(1f, 2f); } }

        private int _blockSize = 8; // voxel size = sample size - 1
        private GameObject[, ,] _blocks;

        public ComputeShader _shaderNormal;
        public ComputeShader _shaderCollectTriNum;
        public ComputeShader _shaderMarchingCube;
        public Material _material;

        private ComputeBuffer _bufferCubeEdgeFlags;
        private ComputeBuffer _bufferCornerToTriNumTable;
        private ComputeBuffer _bufferTriangleConnectionTable;

        private List<Int3> _nextUpdateblocks;

        private Queue<TerrainModifier> _modifierQueue;

        public Transform _transform;

        public Vector3 TerrainOrigin { get { return _transform.position; } set { _transform.position = value; } }
        public Vector3 TerrainSize { get { return new Vector3(_width, _elevation, _height) * _voxelScale; } }

        public float _voxelScale = 1f;        
        //voxel index -> world pos: (voxel index - 0) * _voxelScale + TerrainOrigin
        //world pos -> voxel index (space): (world pos - TerrainOrigin) / _voxelScale

        public int _matControlFineness = 8;

        public void Init()
        {
            if (_shaderNormal == null)
                
                throw new UnityException("null shader: _shaderNormal");
            
            if (_shaderCollectTriNum == null)
                throw new UnityException("null shader: _shaderCollectTriNum");
            
            if (_shaderMarchingCube == null)
                throw new UnityException("null shader: _shaderMarchingCube");

            if (_blockSize < 2)
                throw new UnityException("block size must be geq than 2");

            if (_width % _blockSize != 0 || _elevation % _blockSize != 0 || _height % _blockSize != 0)
                throw new UnityException("block size must align to terrain size");

            if (_width + 1 > maxSampleResolution || _elevation + 1 > maxSampleResolution || _height + 1 > maxSampleResolution)
                throw new UnityException("too high resolution (exceeds " + maxSampleResolution + ")");

            _blocks = new GameObject[_width / _blockSize, _elevation / _blockSize, _height / _blockSize];
            _voxelSamples = new float[_width + 2, _elevation + 2, _height + 2]; //augmented to provide correct normal on positive boundary
            for (int x = 0; x < _width + 2; x++)
                for (int y = 0; y < _elevation + 2; y++)
                    for (int z = 0; z < _height + 2; z++)
                        _voxelSamples[x, y, z] = voidDensity;

            _bufferCubeEdgeFlags = new ComputeBuffer(256, sizeof(int));
            _bufferCubeEdgeFlags.SetData(cubeEdgeFlags);
            _bufferCornerToTriNumTable = new ComputeBuffer(256, sizeof(int));
            _bufferCornerToTriNumTable.SetData(cornerToTriNumTable);   
            _bufferTriangleConnectionTable = new ComputeBuffer(256 * 15, sizeof(int));
            _bufferTriangleConnectionTable.SetData(triangleConnectionTable);

            _nextUpdateblocks = new List<Int3>();
            for (int x = 0; x < _width / _blockSize; x++)
                for (int y = 0; y < _elevation / _blockSize; y++)
                    for (int z = 0; z < _height / _blockSize; z++)
                    {
                        _blocks[x, y, z] = new GameObject();
                        _blocks[x, y, z].AddComponent<MeshFilter>();
                        _blocks[x, y, z].AddComponent<MeshRenderer>();

                        //_blocks[x, y, z] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        //_blocks[x, y, z].transform.localScale = Vector3.one * (float)_blockSize;

                        _blocks[x, y, z].transform.parent = this._transform;
                        var pos = new Vector3(x, y, z) * (float)_blockSize * _voxelScale;
                        _blocks[x, y, z].transform.localPosition = pos;
                    }

            _modifierQueue = new Queue<TerrainModifier>();
            
            //DEBUG: set a simple 3d splat map
            _matControlFineness = Mathf.Clamp(_matControlFineness, 1, 8);
            int controlMapSize = _matControlFineness * 16;
            Texture3D controlMap = new Texture3D(controlMapSize, controlMapSize, controlMapSize, TextureFormat.ARGB32, false);
            controlMap.filterMode = FilterMode.Bilinear;
            controlMap.wrapMode = TextureWrapMode.Repeat;

            Color[] colors = new Color[controlMapSize * controlMapSize * controlMapSize];
            LibNoise.Billow control = new LibNoise.Billow();
            control.Frequency = 0.02;
            control.OctaveCount = 1;
            for(int x = 0; x < controlMapSize; x++)
                for(int y = 0; y < controlMapSize; y++)
                    for (int z = 0; z < controlMapSize; z++)
                    {
                        if ((float)y < (float)controlMapSize * 0.25)
                            colors[x + y * controlMapSize + z * controlMapSize * controlMapSize] = new Color(1, 0, 0, 0);
                        else if ((float)y < (float)controlMapSize * 0.5)
                            colors[x + y * controlMapSize + z * controlMapSize * controlMapSize] = new Color(0, 1, 0, 0);
                        else if ((float)y < (float)controlMapSize * 0.75)
                            colors[x + y * controlMapSize + z * controlMapSize * controlMapSize] = new Color(0, 0, 1, 0);
                        else
                            colors[x + y * controlMapSize + z * controlMapSize * controlMapSize] = new Color(0, 0, 0, 1);
                        //var r = (float)control.GetValue(x,y,z);
                        //var g = (float)control.GetValue(x + 7.7,y - 4.9,z + 93.3);
                        //var b = (float)control.GetValue(x - 14.3,y + 28.7,z - 14.7);
                        //var a = (float)control.GetValue(x + 71.9, y + 23.7, z - 1.1);
                        //var inv_sum = 1f/ ( r + g + b + a);
                        //r *= inv_sum; g *= inv_sum; b *= inv_sum; a *= inv_sum;
                        //colors[x + y * controlMapSize + z * controlMapSize * controlMapSize] = new Color(r, g, b, a);
                    }
            controlMap.SetPixels(colors);
            controlMap.Apply();
            _material.SetTexture("_MatControl", controlMap);
            _material.SetVector("_Offset", new Vector4(TerrainOrigin.x, TerrainOrigin.y, TerrainOrigin.z, 0f));
            _material.SetVector("_Scale", new Vector4(1 / TerrainSize.x, 1 / TerrainSize.y, 1 / TerrainSize.z, 1));
        }

        public void Free()
        {
            if (_bufferCubeEdgeFlags != null)
            {
                _bufferCubeEdgeFlags.Release();
                _bufferCubeEdgeFlags = null;
            }

            if(_bufferCornerToTriNumTable != null)
            {
                _bufferCornerToTriNumTable.Release();
                _bufferCornerToTriNumTable = null;
            }

            if (_bufferTriangleConnectionTable != null)
            {
                _bufferTriangleConnectionTable.Release();
                _bufferTriangleConnectionTable = null;
            }
        }   

        public void InsertModifier(TerrainModifier modifier)
        {
            _modifierQueue.Enqueue(modifier);
        }
        public void Update()
        {
            HashSet<Int3> updateBlocks = new HashSet<Int3>();
            while(_modifierQueue.Count > 0)
            {
                var modifier = _modifierQueue.Peek();
                _modifierQueue.Dequeue();

                //voxel index -> world pos: (voxel index - 0) * _voxelScale + TerrainOrigin
                //world pos -> voxel index (space): (world pos - TerrainOrigin) / _voxelScale

                Vector3 worldLow = modifier.LowerBound;
                worldLow = (worldLow - TerrainOrigin) / _voxelScale;
                Int3 low = new Int3(Mathf.FloorToInt(worldLow.x), Mathf.FloorToInt(worldLow.y), Mathf.FloorToInt(worldLow.z));
                low._x = Mathf.Max(low._x, 0); low._y = Mathf.Max(low._y, 0); low._z = Mathf.Max(low._z, 0);   

                Vector3 worldUp = modifier.UpperBound;
                worldUp = (worldUp - TerrainOrigin) / _voxelScale;
                Int3 up = new Int3(Mathf.CeilToInt(worldUp.x), Mathf.CeilToInt(worldUp.y), Mathf.CeilToInt(worldUp.z));
                up._x = Mathf.Min(up._x, _width + 1); up._y = Mathf.Min(up._y, _elevation + 1); up._z = Mathf.Min(up._z, _height + 1);
               
                if (modifier.AddOrErode == true)
                {
                    for (int x = low._x; x <= up._x; x++)
                        for (int y = low._y; y <= up._y; y++)
                            for (int z = low._z; z <= up._z; z++)
                            {
                                var worldPos = new Vector3(x, y, z) * _voxelScale + TerrainOrigin;
                                var md = Mathf.Clamp(modifier.QueryDensity(worldPos), voidDensity, fullDensity);
                                _voxelSamples[x, y, z] = Mathf.Max(_voxelSamples[x, y, z], md);
                            }
                }
                else
                {
                    for (int x = low._x; x <= up._x; x++)
                        for (int y = low._y; y <= up._y; y++)
                            for (int z = low._z; z <= up._z; z++)
                            {
                                var worldPos = new Vector3(x, y, z) * _voxelScale + TerrainOrigin;
                                var minus_md = - Mathf.Clamp(modifier.QueryDensity(worldPos), voidDensity, fullDensity);
                                _voxelSamples[x, y, z] = Mathf.Clamp(Mathf.Min(_voxelSamples[x, y, z], minus_md), voidDensity, fullDensity);
                            }
                }

                for (int x = 0; x < _width / _blockSize; x++)
                    for (int y = 0; y < _elevation / _blockSize; y++)
                        for (int z = 0; z < _height / _blockSize; z++)
                        {
                            if( (up._x >= x * _blockSize && low._x <= x*_blockSize + _blockSize) &&
                                (up._y >= y * _blockSize && low._y <= y*_blockSize + _blockSize) &&
                                (up._z >= z * _blockSize && low._z <= z*_blockSize + _blockSize) )
                            {
                                updateBlocks.Add(new Int3(x, y, z));
                            }
                        }

            }

            _nextUpdateblocks = updateBlocks.ToList();
            if(_nextUpdateblocks.Count > 0)
                BatchUpdate();
            _nextUpdateblocks.Clear();
        }
        private void BatchUpdate()
        {
            if (_nextUpdateblocks.Count == 0)
                return;

            int ag1BlockSize = _blockSize + 1;
            int ag2BlockSize = _blockSize + 2;

            float[] samples = new float[_nextUpdateblocks.Count * (ag2BlockSize) * (ag2BlockSize) * (ag2BlockSize)];
            for (int blockNum = 0; blockNum < _nextUpdateblocks.Count; blockNum++)
            {
                var blockIdx = _nextUpdateblocks[blockNum];
                int x = blockIdx._x; int y = blockIdx._y; int z = blockIdx._z;
                for (int ix = 0; ix < ag2BlockSize; ix++)
                    for (int iy = 0; iy < ag2BlockSize; iy++)
                        for (int iz = 0; iz < ag2BlockSize; iz++)
                        {
                            var queryX = x * _blockSize + ix;
                            var queryY = y * _blockSize + iy;
                            var queryZ = z * _blockSize + iz;

                            //preserve value
                            samples[ix +
                                iy * (ag2BlockSize) +
                                iz * (ag2BlockSize) * (ag2BlockSize) +
                                blockNum * (ag2BlockSize) * (ag2BlockSize) * (ag2BlockSize)]
                                = _voxelSamples[queryX, queryY, queryZ];
                        }
            }

            ////rebuild normals
            int nrmKernel = _shaderNormal.FindKernel("SampleNormal");
            if(nrmKernel < 0)
                throw new UnityException("Fail to find kernel of shader: " + _shaderNormal.name);
            ComputeBuffer bufferSamples = new ComputeBuffer(_nextUpdateblocks.Count * (ag2BlockSize) * (ag2BlockSize) * (ag2BlockSize), sizeof(float));
            bufferSamples.SetData(samples);
            _shaderNormal.SetBuffer(nrmKernel, "_Samples", bufferSamples);

            ComputeBuffer bufferNormals = new ComputeBuffer(_nextUpdateblocks.Count * (ag1BlockSize) * (ag1BlockSize) * (ag1BlockSize), sizeof(float) * 3);
            _shaderNormal.SetBuffer(nrmKernel, "_Normals", bufferNormals);
            //dispatch compute shader
            _shaderNormal.Dispatch(nrmKernel, _nextUpdateblocks.Count, 1, 1);

            //marching-cube

            //STAGE I: collect triangle number
            int ctnKernel = _shaderCollectTriNum.FindKernel("CollectTriNum");
            if (ctnKernel < 0)
                throw new UnityException("Fail to find kernel of shader: " + _shaderCollectTriNum.name);
            ComputeBuffer bufferTriNum = new ComputeBuffer(1, sizeof(int));
            bufferTriNum.SetData(new int[] { 0 });
            ComputeBuffer bufferCornerFlags = new ComputeBuffer(_nextUpdateblocks.Count * _blockSize * _blockSize * _blockSize, sizeof(int));
            
            _shaderCollectTriNum.SetBuffer(ctnKernel, "_Samples", bufferSamples);
            _shaderCollectTriNum.SetBuffer(ctnKernel, "_CornerToTriNumTable", _bufferCornerToTriNumTable);
            _shaderCollectTriNum.SetBuffer(ctnKernel, "_TriNum", bufferTriNum);
            _shaderCollectTriNum.SetBuffer(ctnKernel, "_CornerFlags", bufferCornerFlags);
            
            _shaderCollectTriNum.Dispatch(ctnKernel, _nextUpdateblocks.Count, 1, 1);
            
            int[] triNum = new int[1];
            bufferTriNum.GetData(triNum);
            if(triNum[0] == 0)
            {
                //no triangles, early exit
                bufferNormals.Release();
                bufferSamples.Release();

                bufferTriNum.Release();
                bufferCornerFlags.Release();
                return;
            }
            //Debug.Log("triangles count " + triNum[0]);
            
            //STAGE II: do marching cube
            int mcKernel = _shaderMarchingCube.FindKernel("MarchingCube");
            if (mcKernel < 0)
                throw new UnityException("Fail to find kernel of shader: " + _shaderMarchingCube.name);
            ComputeBuffer bufferMeshes = new ComputeBuffer(triNum[0], CSTriangle.stride);
            ComputeBuffer bufferTriEndIndex = new ComputeBuffer(1, sizeof(int));
            bufferTriEndIndex.SetData(new int[] { 0 });
            _shaderMarchingCube.SetBuffer(mcKernel, "_Samples", bufferSamples);
            _shaderMarchingCube.SetBuffer(mcKernel, "_Normals", bufferNormals);
            _shaderMarchingCube.SetBuffer(mcKernel, "_CornerFlags", bufferCornerFlags);
            _shaderMarchingCube.SetBuffer(mcKernel, "_CubeEdgeFlags", _bufferCubeEdgeFlags);
            _shaderMarchingCube.SetBuffer(mcKernel, "_TriangleConnectionTable", _bufferTriangleConnectionTable);
            _shaderMarchingCube.SetBuffer(mcKernel, "_Meshes", bufferMeshes);
            _shaderMarchingCube.SetBuffer(mcKernel, "_TriEndIndex", bufferTriEndIndex);
            //dispatch compute shader
            _shaderMarchingCube.Dispatch(mcKernel, _nextUpdateblocks.Count, 1, 1);

            //split bufferMeshes to meshes for individual blocks
            CSTriangle[] csTriangles = new CSTriangle[triNum[0]];//need a counter here
            bufferMeshes.GetData(csTriangles);


            List<Vector3>[] vertices = new List<Vector3>[_nextUpdateblocks.Count];
            List<Vector3>[] normals = new List<Vector3>[_nextUpdateblocks.Count];
            for (int i = 0; i < _nextUpdateblocks.Count; i++)
            {
                vertices[i] = new List<Vector3>();
                normals[i] = new List<Vector3>();
            }
            foreach (var vt in csTriangles)
            {
                vertices[vt._block].Add(vt._position0 * _voxelScale);
                vertices[vt._block].Add(vt._position1 * _voxelScale);
                vertices[vt._block].Add(vt._position2 * _voxelScale);

                normals[vt._block].Add(vt._normal0);
                normals[vt._block].Add(vt._normal1);
                normals[vt._block].Add(vt._normal2);
            }

            for (int i = 0; i < _nextUpdateblocks.Count; i++)
            {
                var x = _nextUpdateblocks[i]._x;
                var y = _nextUpdateblocks[i]._y;
                var z = _nextUpdateblocks[i]._z;
                _blocks[x, y, z].GetComponent<MeshFilter>().mesh.Clear();
                _blocks[x, y, z].GetComponent<MeshFilter>().mesh.vertices = vertices[i].ToArray();
                var idx = Enumerable.Range(0, vertices[i].Count).ToArray();
                _blocks[x, y, z].GetComponent<MeshFilter>().mesh.SetTriangles(idx, 0);
                _blocks[x, y, z].GetComponent<MeshFilter>().mesh.normals = normals[i].ToArray();
                _blocks[x, y, z].GetComponent<MeshRenderer>().material = _material;
            }

            bufferNormals.Release();
            bufferSamples.Release();

            bufferTriNum.Release();
            bufferCornerFlags.Release();

            bufferMeshes.Release();
            bufferTriEndIndex.Release();
        }

        /*****      data for marching-cube      *****/
        public const int maxTriNumPerCell = 5;

        // two looking up tables

        // For any edge, if one vertex is inside of the surface and the other is outside of the surface
        // then the edge intersects the surface
        // For each of the 8 vertices of the cube can be two possible states : either inside or outside of the surface
        // For any cube the are 2^8=256 possible sets of vertex states
        // This table lists the edges intersected by the surface for all 256 possible vertex states
        // There are 12 edges.  For each entry in the table, if edge #n is intersected, then bit #n is set to 1
        // cubeEdgeFlags[256]
        static int[] cubeEdgeFlags = new int[]
	    {
		    0x000, 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c, 0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00, 
		    0x190, 0x099, 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c, 0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90, 
		    0x230, 0x339, 0x033, 0x13a, 0x636, 0x73f, 0x435, 0x53c, 0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30, 
		    0x3a0, 0x2a9, 0x1a3, 0x0aa, 0x7a6, 0x6af, 0x5a5, 0x4ac, 0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0, 
		    0x460, 0x569, 0x663, 0x76a, 0x066, 0x16f, 0x265, 0x36c, 0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60, 
		    0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0x0ff, 0x3f5, 0x2fc, 0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0, 
		    0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x055, 0x15c, 0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950, 
		    0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0x0cc, 0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0, 
		    0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc, 0x0cc, 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0, 
		    0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c, 0x15c, 0x055, 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650, 
		    0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc, 0x2fc, 0x3f5, 0x0ff, 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0, 
		    0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c, 0x36c, 0x265, 0x16f, 0x066, 0x76a, 0x663, 0x569, 0x460, 
		    0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac, 0x4ac, 0x5a5, 0x6af, 0x7a6, 0x0aa, 0x1a3, 0x2a9, 0x3a0, 
		    0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c, 0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x033, 0x339, 0x230, 
		    0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c, 0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x099, 0x190, 
		    0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c, 0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x000
	    };



        //cornerToTriNum[256]
        static int[] cornerToTriNumTable = new int[] 
        {
	        0,1,1,2,1,2,2,3,1,2,2,3,2,3,3,2,
	        1,2,2,3,2,3,3,4,2,3,3,4,3,4,4,3,
	        1,2,2,3,2,3,3,4,2,3,3,4,3,4,4,3,
	        2,3,3,2,3,4,4,3,3,4,4,3,4,5,5,2,
	        1,2,2,3,2,3,3,4,2,3,3,4,3,4,4,3,
	        2,3,3,4,3,4,4,5,3,4,4,5,4,5,5,4,
	        2,3,3,4,3,4,2,3,3,4,4,5,4,5,3,2,
	        3,4,4,3,4,5,3,2,4,5,5,4,5,2,4,1,
	        1,2,2,3,2,3,3,4,2,3,3,4,3,4,4,3,
	        2,3,3,4,3,4,4,5,3,2,4,3,4,3,5,2,
	        2,3,3,4,3,4,4,5,3,4,4,5,4,5,5,4,
	        3,4,4,3,4,5,5,4,4,3,5,2,5,4,2,1,
	        2,3,3,4,3,4,4,5,3,4,4,5,2,3,3,2,
	        3,4,4,5,4,5,5,2,4,3,5,4,3,2,4,1,
	        3,4,4,5,4,5,3,4,4,5,5,2,3,4,2,1,
	        2,3,3,2,3,4,2,1,3,2,4,1,2,1,1,0
        };

        //  For each of the possible vertex states listed in cubeEdgeFlags there is a specific triangulation
        //  of the edge intersection points.  triangleConnectionTable lists all of them in the form of
        //  0-5 edge triples with the list terminated by the invalid value -1.
        //  For example: triangleConnectionTable[3] list the 2 triangles formed when corner[0] 
        //  and corner[1] are inside of the surface, but the rest of the cube is not.
        //  triangleConnectionTable[256][15]
        static int[,] triangleConnectionTable = new int[,] 
        {
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {0,8,3,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {0,1,9,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {1,8,3,9,8,1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {1,2,10,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {0,8,3,1,2,10,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {9,2,10,0,2,9,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {2,8,3,2,10,8,10,9,8,-1,-1,-1,-1,-1,-1},
            {3,11,2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {0,11,2,8,11,0,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {1,9,0,2,3,11,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {1,11,2,1,9,11,9,8,11,-1,-1,-1,-1,-1,-1},
            {3,10,1,11,10,3,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {0,10,1,0,8,10,8,11,10,-1,-1,-1,-1,-1,-1},
            {3,9,0,3,11,9,11,10,9,-1,-1,-1,-1,-1,-1},
            {9,8,10,10,8,11,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {4,7,8,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {4,3,0,7,3,4,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {0,1,9,8,4,7,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {4,1,9,4,7,1,7,3,1,-1,-1,-1,-1,-1,-1},
            {1,2,10,8,4,7,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {3,4,7,3,0,4,1,2,10,-1,-1,-1,-1,-1,-1},
            {9,2,10,9,0,2,8,4,7,-1,-1,-1,-1,-1,-1},
            {2,10,9,2,9,7,2,7,3,7,9,4,-1,-1,-1},
            {8,4,7,3,11,2,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {11,4,7,11,2,4,2,0,4,-1,-1,-1,-1,-1,-1},
            {9,0,1,8,4,7,2,3,11,-1,-1,-1,-1,-1,-1},
            {4,7,11,9,4,11,9,11,2,9,2,1,-1,-1,-1},
            {3,10,1,3,11,10,7,8,4,-1,-1,-1,-1,-1,-1},
            {1,11,10,1,4,11,1,0,4,7,11,4,-1,-1,-1},
            {4,7,8,9,0,11,9,11,10,11,0,3,-1,-1,-1},
            {4,7,11,4,11,9,9,11,10,-1,-1,-1,-1,-1,-1},
            {9,5,4,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {9,5,4,0,8,3,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {0,5,4,1,5,0,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {8,5,4,8,3,5,3,1,5,-1,-1,-1,-1,-1,-1},
            {1,2,10,9,5,4,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {3,0,8,1,2,10,4,9,5,-1,-1,-1,-1,-1,-1},
            {5,2,10,5,4,2,4,0,2,-1,-1,-1,-1,-1,-1},
            {2,10,5,3,2,5,3,5,4,3,4,8,-1,-1,-1},
            {9,5,4,2,3,11,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {0,11,2,0,8,11,4,9,5,-1,-1,-1,-1,-1,-1},
            {0,5,4,0,1,5,2,3,11,-1,-1,-1,-1,-1,-1},
            {2,1,5,2,5,8,2,8,11,4,8,5,-1,-1,-1},
            {10,3,11,10,1,3,9,5,4,-1,-1,-1,-1,-1,-1},
            {4,9,5,0,8,1,8,10,1,8,11,10,-1,-1,-1},
            {5,4,0,5,0,11,5,11,10,11,0,3,-1,-1,-1},
            {5,4,8,5,8,10,10,8,11,-1,-1,-1,-1,-1,-1},
            {9,7,8,5,7,9,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {9,3,0,9,5,3,5,7,3,-1,-1,-1,-1,-1,-1},
            {0,7,8,0,1,7,1,5,7,-1,-1,-1,-1,-1,-1},
            {1,5,3,3,5,7,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {9,7,8,9,5,7,10,1,2,-1,-1,-1,-1,-1,-1},
            {10,1,2,9,5,0,5,3,0,5,7,3,-1,-1,-1},
            {8,0,2,8,2,5,8,5,7,10,5,2,-1,-1,-1},
            {2,10,5,2,5,3,3,5,7,-1,-1,-1,-1,-1,-1},
            {7,9,5,7,8,9,3,11,2,-1,-1,-1,-1,-1,-1},
            {9,5,7,9,7,2,9,2,0,2,7,11,-1,-1,-1},
            {2,3,11,0,1,8,1,7,8,1,5,7,-1,-1,-1},
            {11,2,1,11,1,7,7,1,5,-1,-1,-1,-1,-1,-1},
            {9,5,8,8,5,7,10,1,3,10,3,11,-1,-1,-1},
            {5,7,0,5,0,9,7,11,0,1,0,10,11,10,0},
            {11,10,0,11,0,3,10,5,0,8,0,7,5,7,0},
            {11,10,5,7,11,5,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {10,6,5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {0,8,3,5,10,6,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {9,0,1,5,10,6,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {1,8,3,1,9,8,5,10,6,-1,-1,-1,-1,-1,-1},
            {1,6,5,2,6,1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {1,6,5,1,2,6,3,0,8,-1,-1,-1,-1,-1,-1},
            {9,6,5,9,0,6,0,2,6,-1,-1,-1,-1,-1,-1},
            {5,9,8,5,8,2,5,2,6,3,2,8,-1,-1,-1},
            {2,3,11,10,6,5,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {11,0,8,11,2,0,10,6,5,-1,-1,-1,-1,-1,-1},
            {0,1,9,2,3,11,5,10,6,-1,-1,-1,-1,-1,-1},
            {5,10,6,1,9,2,9,11,2,9,8,11,-1,-1,-1},
            {6,3,11,6,5,3,5,1,3,-1,-1,-1,-1,-1,-1},
            {0,8,11,0,11,5,0,5,1,5,11,6,-1,-1,-1},
            {3,11,6,0,3,6,0,6,5,0,5,9,-1,-1,-1},
            {6,5,9,6,9,11,11,9,8,-1,-1,-1,-1,-1,-1},
            {5,10,6,4,7,8,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {4,3,0,4,7,3,6,5,10,-1,-1,-1,-1,-1,-1},
            {1,9,0,5,10,6,8,4,7,-1,-1,-1,-1,-1,-1},
            {10,6,5,1,9,7,1,7,3,7,9,4,-1,-1,-1},
            {6,1,2,6,5,1,4,7,8,-1,-1,-1,-1,-1,-1},
            {1,2,5,5,2,6,3,0,4,3,4,7,-1,-1,-1},
            {8,4,7,9,0,5,0,6,5,0,2,6,-1,-1,-1},
            {7,3,9,7,9,4,3,2,9,5,9,6,2,6,9},
            {3,11,2,7,8,4,10,6,5,-1,-1,-1,-1,-1,-1},
            {5,10,6,4,7,2,4,2,0,2,7,11,-1,-1,-1},
            {0,1,9,4,7,8,2,3,11,5,10,6,-1,-1,-1},
            {9,2,1,9,11,2,9,4,11,7,11,4,5,10,6},
            {8,4,7,3,11,5,3,5,1,5,11,6,-1,-1,-1},
            {5,1,11,5,11,6,1,0,11,7,11,4,0,4,11},
            {0,5,9,0,6,5,0,3,6,11,6,3,8,4,7},
            {6,5,9,6,9,11,4,7,9,7,11,9,-1,-1,-1},
            {10,4,9,6,4,10,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {4,10,6,4,9,10,0,8,3,-1,-1,-1,-1,-1,-1},
            {10,0,1,10,6,0,6,4,0,-1,-1,-1,-1,-1,-1},
            {8,3,1,8,1,6,8,6,4,6,1,10,-1,-1,-1},
            {1,4,9,1,2,4,2,6,4,-1,-1,-1,-1,-1,-1},
            {3,0,8,1,2,9,2,4,9,2,6,4,-1,-1,-1},
            {0,2,4,4,2,6,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {8,3,2,8,2,4,4,2,6,-1,-1,-1,-1,-1,-1},
            {10,4,9,10,6,4,11,2,3,-1,-1,-1,-1,-1,-1},
            {0,8,2,2,8,11,4,9,10,4,10,6,-1,-1,-1},
            {3,11,2,0,1,6,0,6,4,6,1,10,-1,-1,-1},
            {6,4,1,6,1,10,4,8,1,2,1,11,8,11,1},
            {9,6,4,9,3,6,9,1,3,11,6,3,-1,-1,-1},
            {8,11,1,8,1,0,11,6,1,9,1,4,6,4,1},
            {3,11,6,3,6,0,0,6,4,-1,-1,-1,-1,-1,-1},
            {6,4,8,11,6,8,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {7,10,6,7,8,10,8,9,10,-1,-1,-1,-1,-1,-1},
            {0,7,3,0,10,7,0,9,10,6,7,10,-1,-1,-1},
            {10,6,7,1,10,7,1,7,8,1,8,0,-1,-1,-1},
            {10,6,7,10,7,1,1,7,3,-1,-1,-1,-1,-1,-1},
            {1,2,6,1,6,8,1,8,9,8,6,7,-1,-1,-1},
            {2,6,9,2,9,1,6,7,9,0,9,3,7,3,9},
            {7,8,0,7,0,6,6,0,2,-1,-1,-1,-1,-1,-1},
            {7,3,2,6,7,2,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {2,3,11,10,6,8,10,8,9,8,6,7,-1,-1,-1},
            {2,0,7,2,7,11,0,9,7,6,7,10,9,10,7},
            {1,8,0,1,7,8,1,10,7,6,7,10,2,3,11},
            {11,2,1,11,1,7,10,6,1,6,7,1,-1,-1,-1},
            {8,9,6,8,6,7,9,1,6,11,6,3,1,3,6},
            {0,9,1,11,6,7,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {7,8,0,7,0,6,3,11,0,11,6,0,-1,-1,-1},
            {7,11,6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {7,6,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {3,0,8,11,7,6,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {0,1,9,11,7,6,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {8,1,9,8,3,1,11,7,6,-1,-1,-1,-1,-1,-1},
            {10,1,2,6,11,7,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {1,2,10,3,0,8,6,11,7,-1,-1,-1,-1,-1,-1},
            {2,9,0,2,10,9,6,11,7,-1,-1,-1,-1,-1,-1},
            {6,11,7,2,10,3,10,8,3,10,9,8,-1,-1,-1},
            {7,2,3,6,2,7,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {7,0,8,7,6,0,6,2,0,-1,-1,-1,-1,-1,-1},
            {2,7,6,2,3,7,0,1,9,-1,-1,-1,-1,-1,-1},
            {1,6,2,1,8,6,1,9,8,8,7,6,-1,-1,-1},
            {10,7,6,10,1,7,1,3,7,-1,-1,-1,-1,-1,-1},
            {10,7,6,1,7,10,1,8,7,1,0,8,-1,-1,-1},
            {0,3,7,0,7,10,0,10,9,6,10,7,-1,-1,-1},
            {7,6,10,7,10,8,8,10,9,-1,-1,-1,-1,-1,-1},
            {6,8,4,11,8,6,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {3,6,11,3,0,6,0,4,6,-1,-1,-1,-1,-1,-1},
            {8,6,11,8,4,6,9,0,1,-1,-1,-1,-1,-1,-1},
            {9,4,6,9,6,3,9,3,1,11,3,6,-1,-1,-1},
            {6,8,4,6,11,8,2,10,1,-1,-1,-1,-1,-1,-1},
            {1,2,10,3,0,11,0,6,11,0,4,6,-1,-1,-1},
            {4,11,8,4,6,11,0,2,9,2,10,9,-1,-1,-1},
            {10,9,3,10,3,2,9,4,3,11,3,6,4,6,3},
            {8,2,3,8,4,2,4,6,2,-1,-1,-1,-1,-1,-1},
            {0,4,2,4,6,2,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {1,9,0,2,3,4,2,4,6,4,3,8,-1,-1,-1},
            {1,9,4,1,4,2,2,4,6,-1,-1,-1,-1,-1,-1},
            {8,1,3,8,6,1,8,4,6,6,10,1,-1,-1,-1},
            {10,1,0,10,0,6,6,0,4,-1,-1,-1,-1,-1,-1},
            {4,6,3,4,3,8,6,10,3,0,3,9,10,9,3},
            {10,9,4,6,10,4,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {4,9,5,7,6,11,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {0,8,3,4,9,5,11,7,6,-1,-1,-1,-1,-1,-1},
            {5,0,1,5,4,0,7,6,11,-1,-1,-1,-1,-1,-1},
            {11,7,6,8,3,4,3,5,4,3,1,5,-1,-1,-1},
            {9,5,4,10,1,2,7,6,11,-1,-1,-1,-1,-1,-1},
            {6,11,7,1,2,10,0,8,3,4,9,5,-1,-1,-1},
            {7,6,11,5,4,10,4,2,10,4,0,2,-1,-1,-1},
            {3,4,8,3,5,4,3,2,5,10,5,2,11,7,6},
            {7,2,3,7,6,2,5,4,9,-1,-1,-1,-1,-1,-1},
            {9,5,4,0,8,6,0,6,2,6,8,7,-1,-1,-1},
            {3,6,2,3,7,6,1,5,0,5,4,0,-1,-1,-1},
            {6,2,8,6,8,7,2,1,8,4,8,5,1,5,8},
            {9,5,4,10,1,6,1,7,6,1,3,7,-1,-1,-1},
            {1,6,10,1,7,6,1,0,7,8,7,0,9,5,4},
            {4,0,10,4,10,5,0,3,10,6,10,7,3,7,10},
            {7,6,10,7,10,8,5,4,10,4,8,10,-1,-1,-1},
            {6,9,5,6,11,9,11,8,9,-1,-1,-1,-1,-1,-1},
            {3,6,11,0,6,3,0,5,6,0,9,5,-1,-1,-1},
            {0,11,8,0,5,11,0,1,5,5,6,11,-1,-1,-1},
            {6,11,3,6,3,5,5,3,1,-1,-1,-1,-1,-1,-1},
            {1,2,10,9,5,11,9,11,8,11,5,6,-1,-1,-1},
            {0,11,3,0,6,11,0,9,6,5,6,9,1,2,10},
            {11,8,5,11,5,6,8,0,5,10,5,2,0,2,5},
            {6,11,3,6,3,5,2,10,3,10,5,3,-1,-1,-1},
            {5,8,9,5,2,8,5,6,2,3,8,2,-1,-1,-1},
            {9,5,6,9,6,0,0,6,2,-1,-1,-1,-1,-1,-1},
            {1,5,8,1,8,0,5,6,8,3,8,2,6,2,8},
            {1,5,6,2,1,6,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {1,3,6,1,6,10,3,8,6,5,6,9,8,9,6},
            {10,1,0,10,0,6,9,5,0,5,6,0,-1,-1,-1},
            {0,3,8,5,6,10,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {10,5,6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {11,5,10,7,5,11,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {11,5,10,11,7,5,8,3,0,-1,-1,-1,-1,-1,-1},
            {5,11,7,5,10,11,1,9,0,-1,-1,-1,-1,-1,-1},
            {10,7,5,10,11,7,9,8,1,8,3,1,-1,-1,-1},
            {11,1,2,11,7,1,7,5,1,-1,-1,-1,-1,-1,-1},
            {0,8,3,1,2,7,1,7,5,7,2,11,-1,-1,-1},
            {9,7,5,9,2,7,9,0,2,2,11,7,-1,-1,-1},
            {7,5,2,7,2,11,5,9,2,3,2,8,9,8,2},
            {2,5,10,2,3,5,3,7,5,-1,-1,-1,-1,-1,-1},
            {8,2,0,8,5,2,8,7,5,10,2,5,-1,-1,-1},
            {9,0,1,5,10,3,5,3,7,3,10,2,-1,-1,-1},
            {9,8,2,9,2,1,8,7,2,10,2,5,7,5,2},
            {1,3,5,3,7,5,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {0,8,7,0,7,1,1,7,5,-1,-1,-1,-1,-1,-1},
            {9,0,3,9,3,5,5,3,7,-1,-1,-1,-1,-1,-1},
            {9,8,7,5,9,7,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {5,8,4,5,10,8,10,11,8,-1,-1,-1,-1,-1,-1},
            {5,0,4,5,11,0,5,10,11,11,3,0,-1,-1,-1},
            {0,1,9,8,4,10,8,10,11,10,4,5,-1,-1,-1},
            {10,11,4,10,4,5,11,3,4,9,4,1,3,1,4},
            {2,5,1,2,8,5,2,11,8,4,5,8,-1,-1,-1},
            {0,4,11,0,11,3,4,5,11,2,11,1,5,1,11},
            {0,2,5,0,5,9,2,11,5,4,5,8,11,8,5},
            {9,4,5,2,11,3,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {2,5,10,3,5,2,3,4,5,3,8,4,-1,-1,-1},
            {5,10,2,5,2,4,4,2,0,-1,-1,-1,-1,-1,-1},
            {3,10,2,3,5,10,3,8,5,4,5,8,0,1,9},
            {5,10,2,5,2,4,1,9,2,9,4,2,-1,-1,-1},
            {8,4,5,8,5,3,3,5,1,-1,-1,-1,-1,-1,-1},
            {0,4,5,1,0,5,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {8,4,5,8,5,3,9,0,5,0,3,5,-1,-1,-1},
            {9,4,5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {4,11,7,4,9,11,9,10,11,-1,-1,-1,-1,-1,-1},
            {0,8,3,4,9,7,9,11,7,9,10,11,-1,-1,-1},
            {1,10,11,1,11,4,1,4,0,7,4,11,-1,-1,-1},
            {3,1,4,3,4,8,1,10,4,7,4,11,10,11,4},
            {4,11,7,9,11,4,9,2,11,9,1,2,-1,-1,-1},
            {9,7,4,9,11,7,9,1,11,2,11,1,0,8,3},
            {11,7,4,11,4,2,2,4,0,-1,-1,-1,-1,-1,-1},
            {11,7,4,11,4,2,8,3,4,3,2,4,-1,-1,-1},
            {2,9,10,2,7,9,2,3,7,7,4,9,-1,-1,-1},
            {9,10,7,9,7,4,10,2,7,8,7,0,2,0,7},
            {3,7,10,3,10,2,7,4,10,1,10,0,4,0,10},
            {1,10,2,8,7,4,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {4,9,1,4,1,7,7,1,3,-1,-1,-1,-1,-1,-1},
            {4,9,1,4,1,7,0,8,1,8,7,1,-1,-1,-1},
            {4,0,3,7,4,3,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {4,8,7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {9,10,8,10,11,8,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {3,0,9,3,9,11,11,9,10,-1,-1,-1,-1,-1,-1},
            {0,1,10,0,10,8,8,10,11,-1,-1,-1,-1,-1,-1},
            {3,1,10,11,3,10,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {1,2,11,1,11,9,9,11,8,-1,-1,-1,-1,-1,-1},
            {3,0,9,3,9,11,1,2,9,2,11,9,-1,-1,-1},
            {0,2,11,8,0,11,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {3,2,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {2,3,8,2,8,10,10,8,9,-1,-1,-1,-1,-1,-1},
            {9,10,2,0,9,2,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {2,3,8,2,8,10,0,1,8,1,10,8,-1,-1,-1},
            {1,10,2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {1,3,8,9,1,8,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {0,9,1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {0,3,8,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        };
    }
}

