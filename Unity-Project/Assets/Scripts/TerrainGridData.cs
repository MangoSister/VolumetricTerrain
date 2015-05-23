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
        public float[] _matComponents;

        public const int matComponentNum = 6;

        public TerrainSample(float elevation, int matIdx)
        {
            _elevation = elevation;
            _matComponents = new float[matComponentNum];
            _matComponents[0] = 1f;
        }
    }

    //public class TerrainGrid : TerrainModifier
    //{
    //    public int _width;
    //    public int _height;
    //    public TerrainSample[,] _samples;
    //    public int _maxElevation;

    //    public TerrainGrid(int width, int maxElevation, int height, bool addOrErode)
    //    {
    //        if (width < 1 || height < 1 || maxElevation < 0)
    //            throw new UnityException("invalid params");

    //        _width = width;
    //        _height = height;
    //        _maxElevation = maxElevation;
    //        _samples = new TerrainSample[_width, _height];

    //        AddOrErode = addOrErode;
    //    }

    //    public Vector3 LowerBound
    //    { get { return new Vector3(0, 0, 0); } }

    //    public Vector3 UpperBound
    //    { get { return new Vector3(_width - 1, _maxElevation - 1, _height - 1); } }

    //    public bool AddOrErode { get; set; }
    //    public float QueryDensity(Vector3 pos)
    //    {
    //        //pos.x: width
    //        //pos.y: elevation
    //        //pos.z: height

    //        if (width < 0 || width >= _width || height < 0 || height >= _height) //argumented to include boundary cases
    //            throw new UnityException("index exceeds bound: width: " + width + ", height: " + height);

    //        if (width == _width && width == _height)
    //            return _samples[width - 1, height - 1]._elevation * 3f -
    //                _samples[width - 2, height - 1]._elevation -
    //                _samples[width - 1, height - 2]._elevation - (float)elevation;

    //        else if (width == _width)
    //            return _samples[width - 1, height]._elevation * 2f - _samples[width - 2, height]._elevation - (float)elevation;

    //        else if (height == _height)
    //            return _samples[width, height - 1]._elevation * 2f - _samples[width, height - 2]._elevation - (float)elevation;

    //        else
    //            return _samples[width, height]._elevation - (float)elevation;
    //    }

    //    public float QueryMaterial(Vector3 pos)
    //    {
    //        //pos.x: width
    //        //pos.y: elevation
    //        //pos.z: height
    //        if (pos.x < 0 || pos.x >= _width || pos.z < 0 || pos.z >= _height)
    //            throw new UnityException("index exceeds bound: width: " + pos.x + ", height: " + pos.z);

    //        return _samples[pos.x, pos.z]._matIdx;
    //    }
    //}
}

