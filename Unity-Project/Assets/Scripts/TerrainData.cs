using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PCGTerrain
{
    /// <summary>
    /// Data structure for 
    /// </summary>
    public struct TerrainSample
    {
        public float _elevation;
        public int _matIdx;

        public const int EmptyMatIdx = 0;

        public TerrainSample(float elevation, int matIdx)
        {
            _elevation = elevation;
            _matIdx = matIdx;
        }
    }
    public class TerrainGrid
    {
        private int _width;
        private int _height;
        private TerrainSample[,] _samples;
        private int _maxElevation;
        public int Width { get { return _width; } }
        public int Height { get { return _height; } }
        public int MaxElevation { get { return _maxElevation; } }

        public TerrainGrid(int width, int height, int maxElevation) 
        {
            if (width < 0 || height < 0 || maxElevation < 0)
                throw new UnityException("invalid params");

            _width = width;
            _height = height;
            _maxElevation = maxElevation;
            _samples = new TerrainSample[_width, _height];
        }

        public float QueryDensity(int width, int height, float elevation)
        {
            if(width < 0 || width >= _width || height < 0 || height >= _height)
                throw new UnityException("index exceeds bound: width: "+width+ ", height: "+height);
            return _samples[width,height]._elevation - elevation;
        }

        public float QueryMaterial(int width, int height, float elevation)
        {
            if (width < 0 || width >= _width || height < 0 || height >= _height)
                throw new UnityException("index exceeds bound: width: " + width + ", height: " + height);
            if (_samples[width, height]._elevation - elevation < 0)
                return TerrainSample.EmptyMatIdx;
            else return _samples[width, height]._matIdx;
        }
    }
}

