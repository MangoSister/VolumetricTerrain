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
    public class TerrainGrid : TerrainModifier
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

        public Int3 LowerBound
        { get { return new Int3(0, 0, 0); } }

        public Int3 UpperBound
        { get { return new Int3(_width, _height, _maxElevation); } }

        public float QueryDensity(int width, int height, int elevation)
        {
            if(width < 0 || width >= _width || height < 0 || height >= _height) //argumented to include boundary cases
                throw new UnityException("index exceeds bound: width: "+width+ ", height: "+height);

            if (width == Width && width == Height)
                return _samples[width - 1, height - 1]._elevation * 3f -
                    _samples[width - 2, height - 1]._elevation -
                    _samples[width - 1, height - 2]._elevation - (float)elevation;

            else if (width == Width)
                return _samples[width - 1, height]._elevation * 2f - _samples[width - 2, height]._elevation - (float)elevation;

            else if (height == Height)
                return _samples[width, height - 1]._elevation * 2f - _samples[width, height - 2]._elevation - (float)elevation;

            else
                return _samples[width, height]._elevation - (float)elevation;
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

