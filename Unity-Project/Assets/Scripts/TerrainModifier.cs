using UnityEngine;
using System;
using System.Collections;

namespace PCGTerrain
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
    public interface TerrainModifier
    {
        Int3 LowerBound { get; }
        Int3 UpperBound { get; }

        //Density > 0 : solid
        //Density < 0 : air
        //Density == 0 : boundary
        float QueryDensity(int x, int y, int z);
    }


    public class SphereModifier : TerrainModifier
    {
        public Vector3 _center;
        public float _radius;

        public Int3 LowerBound
        {
            get
            {
                return new Int3(Mathf.FloorToInt((_center.x - _radius)),
                                Mathf.FloorToInt((_center.y - _radius)),
                                Mathf.FloorToInt((_center.z - _radius)));
            }
        }
        public Int3 UpperBound
        {
            get
            {
                return new Int3(Mathf.CeilToInt((_center.x + _radius)),
                                Mathf.CeilToInt((_center.y + _radius)),
                                Mathf.CeilToInt((_center.z + _radius)));
            }
        }
        public float QueryDensity(int x, int y, int z)
        {
            return _radius - (new Vector3(x, y, z) - _center).magnitude;
        }

        public SphereModifier(Vector3 center, float radius)
        {
            _center = center;
            _radius = radius;
        }
    }
}