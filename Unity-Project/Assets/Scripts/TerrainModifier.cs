//PGRTerrain: Procedural Generation and Rendering of Terrain
//DH2323 Course Project in KTH
//TerrainModifier.cs
//Yang Zhou: yanzho@kth.se
//Yanbo Huang: yanboh@kth.se
//Huiting Wang: huitingw@kth.se
//2015.5

using UnityEngine;
using System;
using System.Collections;
using LibNoise;

namespace PGRTerrain.Render
{
    /// <summary>
    /// The interface for terrain modifier
    /// </summary>
    public interface TerrainModifier
    {
        //AABB for the modifier so that we do not have to upgrade all voxel terrain
        Vector3 LowerBound { get; }
        Vector3 UpperBound { get; }

        //Density function of this modifier
        //Density > 0 : solid
        //Density < 0 : air
        //Density == 0 : boundary
        float QueryDensity(Vector3 pos);

        //true -> add (union operator), false -> erode (difference operator)
        bool AddOrErode { get; set; }
    }

    /// <summary>
    /// Plane: f(x,y,z) = y0 - y
    /// </summary>
    public class PlaneModifier : TerrainModifier
    {
        public float _height;
        public Vector2 _low, _up;

        public Vector3 LowerBound
        { get { return new Vector3(_low.x, float.MinValue, _low.y); } }

        public Vector3 UpperBound
        { get { return new Vector3(_up.x, _height + 1, _up.y); } }
        
        public PlaneModifier(float height, Vector2 low, Vector2 up, bool addOrErode)
        {
            _height = height;
            if (low.x > up.x || low.y > up.y)
                throw new UnityException("invalud aabb");

            _low = low;
            _up = up;
            AddOrErode = addOrErode;
        }
        public float QueryDensity(Vector3 pos)
        {
            return _height - pos.y;
        }

        public bool AddOrErode { get; set; }
    }

    /// <summary>
    /// Sphere: f(x,y,z) = r^2 - [(x-x0)^2 + (y-y0)^2 + (z-z0)^2]
    /// </summary>
    public class SphereModifier : TerrainModifier
    {
        public Vector3 _center;
        public float _radius;
        public bool AddOrErode { get; set; }
        public Vector3 LowerBound
        { get { return new Vector3(_center.x - _radius, _center.y - _radius, _center.z - _radius); } }
        public Vector3 UpperBound
        { get { return new Vector3(_center.x + _radius, _center.y + _radius, _center.z + _radius); } }
        public float QueryDensity(Vector3 pos)
        {
            return _radius - (pos - _center).magnitude;
        }

        public SphereModifier(Vector3 center, float radius, bool addOrErode)
        {
            _center = center;
            _radius = radius;
            AddOrErode = addOrErode;
        }
    }

    /// <summary>
    /// Cylinder
    /// </summary>
    public class CylinderModifier : TerrainModifier
    {
        public Vector3 _axisStart;
        public Vector3 _axisDir;
        public float _axisLength;
        public float _radius;

        public Vector3 LowerBound
        {
            get
            {
                var leftProj = Vector3.ProjectOnPlane(Vector3.left, _axisDir); //x
                var downProj = Vector3.ProjectOnPlane(Vector3.down, _axisDir); //y
                var backProj = Vector3.ProjectOnPlane(Vector3.back, _axisDir); //z
                var floatLow = new Vector3(
                    _axisDir.x > 0f ? (_axisStart + leftProj * _radius).x : (_axisStart + _axisDir * _axisLength + leftProj * _radius).x,
                    _axisDir.y > 0f ? (_axisStart + downProj * _radius).y : (_axisStart + _axisDir * _axisLength + downProj * _radius).y,
                    _axisDir.z > 0f ? (_axisStart + backProj * _radius).z : (_axisStart + _axisDir * _axisLength + backProj * _radius).z
                    );
                return floatLow;
            }
        }

        public Vector3 UpperBound
        {
            get
            {
                var rightProj = Vector3.ProjectOnPlane(Vector3.right, _axisDir); //x
                var upProj = Vector3.ProjectOnPlane(Vector3.up, _axisDir); //y
                var foreProj = Vector3.ProjectOnPlane(Vector3.forward, _axisDir); //z
                var floatUp = new Vector3(
                    _axisDir.x < 0f ? (_axisStart + rightProj * _radius).x : (_axisStart + _axisDir * _axisLength + rightProj * _radius).x,
                    _axisDir.y < 0f ? (_axisStart + upProj * _radius).y : (_axisStart + _axisDir * _axisLength + upProj * _radius).y,
                    _axisDir.z < 0f ? (_axisStart + foreProj * _radius).z : (_axisStart + _axisDir * _axisLength + foreProj * _radius).z
                    );
                return floatUp;
            }
        }

        public CylinderModifier(Vector3 start, Vector3 dir, float length, float radius, bool addOrErode)
        {
            _axisStart = start;
            _axisDir = dir.normalized;
            _axisLength = length;
            _radius = radius;
            AddOrErode = addOrErode;
        }

        public float QueryDensity(Vector3 pos)
        {
            var start2pos = pos - _axisStart;
            var projLength = Vector3.Dot(start2pos, _axisDir);
            return Mathf.Min(projLength, _axisLength - projLength,
                            _radius - Mathf.Sqrt(start2pos.sqrMagnitude - projLength * projLength));
        }
        public bool AddOrErode { get; set; }

    }

    /// <summary>
    /// RidgedMultifractral noise function
    /// here the LibNoise is used to generate noise function
    /// </summary>
    public class RidgedMultifractalModifier : TerrainModifier
    {
        private LibNoise.RidgedMultifractal _generator;
        public bool AddOrErode { get; set; }
        public int _seed { get { return _generator.Seed; } set { _generator.Seed = value; } }
        public int _octave { get { return _generator.OctaveCount; } set { _generator.OctaveCount = value; } }
        public float _frequency { get { return (float)_generator.Frequency; } set { _generator.Frequency = value; } }
        public float _lacunarity { get { return (float)_generator.Lacunarity; } set { _generator.Lacunarity = value; } }
        
        public RidgedMultifractalModifier(int seed, int octave, float freq, float lacun, bool addOrErode)
        {
            _generator = new RidgedMultifractal();
            _generator.NoiseQuality = NoiseQuality.Standard;
            _seed = seed;
            _octave = octave;
            _frequency = freq;
            _lacunarity = lacun;

            AddOrErode = addOrErode;
        }
        public Vector3 LowerBound
        {
            get
            {
                return Vector3.zero;
            }
        }
        public Vector3 UpperBound
        {
            get
            {
                return Vector3.one * 1000;
            }
        }
        public float QueryDensity(Vector3 pos)
        {
            return (float)_generator.GetValue((double)pos.x, (double)pos.y, (double)pos.z);
        }
    }
}