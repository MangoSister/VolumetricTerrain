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
        public int _width;
        public int _height;
        public TerrainSample[,] _samples;
        public int _maxElevation;

        public TerrainGrid(int width, int maxElevation, int height) 
        {
            if (width < 1 || height < 1 || maxElevation < 0)
                throw new UnityException("invalid params");

            _width = width;
            _height = height;
            _maxElevation = maxElevation;
            _samples = new TerrainSample[_width, _height];
        }

        public Int3 LowerBound
        { get { return new Int3(0, 0, 0); } }

        public Int3 UpperBound
        { get { return new Int3(_width - 1, _maxElevation - 1, _height - 1); } }

        public float QueryDensity(int width, int elevation, int height)
        {
            if(width < 0 || width >= _width || height < 0 || height >= _height) //argumented to include boundary cases
                throw new UnityException("index exceeds bound: width: "+width+ ", height: "+height);

            if (width == _width && width == _height)
                return _samples[width - 1, height - 1]._elevation * 3f -
                    _samples[width - 2, height - 1]._elevation -
                    _samples[width - 1, height - 2]._elevation - (float)elevation;

            else if (width == _width)
                return _samples[width - 1, height]._elevation * 2f - _samples[width - 2, height]._elevation - (float)elevation;

            else if (height == _height)
                return _samples[width, height - 1]._elevation * 2f - _samples[width, height - 2]._elevation - (float)elevation;

            else
                return _samples[width, height]._elevation - (float)elevation;
        }

        public float QueryMaterial(int width, float elevation, int height)
        {
            if (width < 0 || width >= _width || height < 0 || height >= _height)
                throw new UnityException("index exceeds bound: width: " + width + ", height: " + height);
            
            return _samples[width, height]._matIdx;
        }
    }
}

