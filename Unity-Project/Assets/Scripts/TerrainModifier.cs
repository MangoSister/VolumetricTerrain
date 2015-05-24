using UnityEngine;
using System;
using System.Collections;
using LibNoise;

namespace PCGTerrain.Render
{
    public struct Int3 : IEquatable<Int3>
    {
        public int _x, _y, _z;
        public Int3(int x, int y, int z)
        { _x = x; _y = y; _z = z; }

        public bool Equals(Int3 other)
        { return _x == other._x && _y == other._y && _z == other._z; }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is Int3))
                return false;
            Int3 other = (Int3)obj;
            return _x == other._x && _y == other._y && _z == other._z;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + _x.GetHashCode();
                hash = hash * 23 + _y.GetHashCode();
                hash = hash * 23 + _z.GetHashCode();
                return hash;
            }
        }
    }

    public struct Int2 : IEquatable<Int2>
    {
        public int _x, _y;
        public Int2(int x, int y)
        { _x = x; _y = y; }

        public bool Equals(Int2 other)
        { return _x == other._x && _y == other._y; }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is Int2))
                return false;
            Int2 other = (Int2)obj;
            return _x == other._x && _y == other._y;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + _x.GetHashCode();
                hash = hash * 23 + _y.GetHashCode();
                return hash;
            }
        }
    }

    public interface TerrainModifier
    {
        Vector3 LowerBound { get; }
        Vector3 UpperBound { get; }

        //Density > 0 : solid
        //Density < 0 : air
        //Density == 0 : boundary
        float QueryDensity(Vector3 pos);

        bool AddOrErode { get; set; } //true -> add, false -> erode
    }

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

    //public class CrossSectionModifier : TerrainModifier
    //{
    //    private float[] _crossSectionLookup; // length must be odd, and must >= 3
    //    public Vector2 _radialScale = Vector2.one;

    //    public Vector3 _axisStart;
    //    public Vector3 _axisDir;
    //    public float _axisLength;

    //    public bool AddOrErode { get; set; }
    //    public Int3 LowerBound
    //    {
    //        get
    //        {
    //            var majorScale = Mathf.Max(_radialScale.x, _radialScale.y);
    //            var leftProj = Vector3.ProjectOnPlane(Vector3.left, _axisDir); //x
    //            var downProj = Vector3.ProjectOnPlane(Vector3.down, _axisDir); //y
    //            var backProj = Vector3.ProjectOnPlane(Vector3.back, _axisDir); //z
    //            var floatLow = new Vector3(
    //                _axisDir.x > 0f ? (_axisStart + leftProj * majorScale).x : (_axisStart + _axisDir * _axisLength + leftProj * majorScale).x,
    //                _axisDir.y > 0f ? (_axisStart + downProj * majorScale).y : (_axisStart + _axisDir * _axisLength + downProj * majorScale).y,
    //                _axisDir.z > 0f ? (_axisStart + backProj * majorScale).z : (_axisStart + _axisDir * _axisLength + backProj * majorScale).z
    //                );
    //            return new Int3(Mathf.FloorToInt(floatLow.x), Mathf.FloorToInt(floatLow.y), Mathf.FloorToInt(floatLow.z));
    //        }
    //    }

    //    public Int3 UpperBound
    //    {
    //        get
    //        {
    //            var majorScale = Mathf.Max(_radialScale.x, _radialScale.y);
    //            var rightProj = Vector3.ProjectOnPlane(Vector3.right, _axisDir); //x
    //            var upProj = Vector3.ProjectOnPlane(Vector3.up, _axisDir); //y
    //            var foreProj = Vector3.ProjectOnPlane(Vector3.forward, _axisDir); //z
    //            var floatLow = new Vector3(
    //                _axisDir.x < 0f ? (_axisStart + rightProj * majorScale).x : (_axisStart + _axisDir * _axisLength + rightProj * majorScale).x,
    //                _axisDir.y < 0f ? (_axisStart + upProj * majorScale).y : (_axisStart + _axisDir * _axisLength + upProj * majorScale).y,
    //                _axisDir.z < 0f ? (_axisStart + foreProj * majorScale).z : (_axisStart + _axisDir * _axisLength + foreProj * majorScale).z
    //                );
    //            return new Int3(Mathf.CeilToInt(floatLow.x), Mathf.CeilToInt(floatLow.y), Mathf.CeilToInt(floatLow.z));
    //        }
    //    }

    //    public CrossSectionModifier(float[] samples, Vector3 start, Vector3 dir, float length, Vector2 radialScale, bool addOrErode)
    //    {
    //        if (samples.Length < 3 || samples.Length % 2 == 0)
    //            throw new UnityException("invalide samples");
    //        _crossSectionLookup = samples;
    //        _axisStart = start;
    //        _axisDir = dir.normalized;
    //        _axisLength = length;
    //        _radialScale = radialScale;
    //        AddOrErode = addOrErode;
    //    }

    //    public float QueryDensity(int x, int y, int z)
    //    {
    //        float radialDensity = 0f;
    //        Vector3 pos = new Vector3(x, y, z);
    //        Vector3 start2pos = pos - _axisStart;
    //        float projLength = Vector3.Dot(start2pos, _axisDir);
    //        Vector3 proj = projLength * _axisDir;
    //        Vector3 radial = start2pos - proj;
    //        if (radial.magnitude == 0f)
    //        {
    //            radialDensity = 1f;
    //            return Mathf.Min(projLength, _axisLength - projLength, radialDensity);
    //        }

    //        Vector3 radialUnit = radial.normalized;
    //        Vector3 radialXAxis = Vector3.zero;
    //        Vector3.OrthoNormalize(ref _axisDir, ref radialXAxis);
    //        float angle = Vector3.Angle(radialXAxis, radialUnit); //degree (< 180)
    //        if (Vector3.Dot(_axisDir, Vector3.Cross(radialXAxis, radialUnit)) < 0f)
    //            angle *= -1f;
    //        angle *= Mathf.Deg2Rad; //change to rad
    //        var slope = Mathf.Tan(angle);
            
    //        if(float.IsInfinity(slope))
    //        { radialDensity = (radialUnit * _radialScale.y).magnitude - radial.magnitude; }

    //        else//ray marching to find the nearest boundary point
    //        {
    //            float horiInc = 2 * _radialScale.x / (float)(_crossSectionLookup.Length - 1f);
    //            float last = 0f;
    //            float curr = 0f;
    //            float boundaryXAxisproj = 0f;
    //            if (Mathf.Abs(angle) < Mathf.PI * 0.5f)
    //            {
    //                for (int i = _crossSectionLookup.Length / 2 + 1; i < _crossSectionLookup.Length; i++)
    //                {
    //                    last = curr;
    //                    curr += Mathf.Abs(slope) * horiInc;
    //                    if(curr >= _crossSectionLookup[i] * _radialScale.y)
    //                    {
    //                        Vector2 l1p1 = new Vector2((i - 1 - _crossSectionLookup.Length / 2) * horiInc, last);
    //                        Vector2 l1p2 = new Vector2((i - _crossSectionLookup.Length / 2) * horiInc, curr);
    //                        Vector2 l2p1 = new Vector2((i - 1 - _crossSectionLookup.Length / 2) * horiInc, _crossSectionLookup[i - 1] * _radialScale.y);
    //                        Vector2 l2p2 = new Vector2((i - _crossSectionLookup.Length / 2) * horiInc, _crossSectionLookup[i] * _radialScale.y);
    //                        Vector2 intersection;
    //                        bool result = MathHelper.InfLineIntersection(l1p1, l1p2, l2p1, l2p2, out intersection);
    //                        if (result == false || intersection.x > l1p2.x || intersection.x < l1p1.x)
    //                            throw new UnityException("expect intersection but fail");
    //                        boundaryXAxisproj = intersection.x;
    //                        break;
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                for (int i = _crossSectionLookup.Length / 2 - 1; i >= 0; i--)
    //                {
    //                    last = curr;
    //                    curr += Mathf.Abs(slope) * horiInc;
    //                    if(curr >= _crossSectionLookup[i] * _radialScale.y)
    //                    {
    //                        Vector2 l1p1 = new Vector2((i + 1 - _crossSectionLookup.Length / 2) * horiInc, last);
    //                        Vector2 l1p2 = new Vector2((i - _crossSectionLookup.Length / 2) * horiInc, curr);
    //                        Vector2 l2p1 = new Vector2((i + 1 - _crossSectionLookup.Length / 2) * horiInc, _crossSectionLookup[i + 1] * _radialScale.y);
    //                        Vector2 l2p2 = new Vector2((i - _crossSectionLookup.Length / 2) * horiInc, _crossSectionLookup[i] * _radialScale.y);
    //                        Vector2 intersection;
    //                        bool result = MathHelper.InfLineIntersection(l1p1, l1p2, l2p1, l2p2, out intersection);
    //                        if (result == false || intersection.x > l1p1.x || intersection.x < l1p2.x)
    //                            throw new UnityException("expect intersection but fail");
    //                        boundaryXAxisproj = intersection.x;
    //                        break;
    //                    }
    //                }
    //            }

    //            //boundaryXAxisproj: projection on radialXAxis
    //            radialDensity = Mathf.Sqrt(boundaryXAxisproj * boundaryXAxisproj * (1 + slope * slope)) - radial.magnitude;
    //        }

    //        return Mathf.Min(projLength, _axisLength - projLength, radialDensity);
    //    }
    //}
}