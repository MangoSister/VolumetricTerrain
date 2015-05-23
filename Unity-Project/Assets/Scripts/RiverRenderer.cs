using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PCGTerrain.Generation;

namespace PCGTerrain.Render
{
    public class RiverRenderer
    {
        public Transform _transform;
        
        public Material _waterMat;
        private Island _island;

        private List<GameObject> _branchObjs;
        public class RiverNode
        {
            public Vector3 _pos;
            public float _flux;
            private float _remainFlux;

            public RiverNode _downstream;
            public List<RiverNode> _upstream;


            public bool IsEstuary { get { return _downstream == null; } }
            public bool IsSource { get { return _upstream.Count == 0; } }

            public RiverNode(Vector3 pos, float flux)
            {
                _pos = pos; _flux = flux; _remainFlux = _flux;
                _downstream = null;
                _upstream = new List<RiverNode>();
            }

            public void AddUpStream(Vector3 pos, float flux)
            {
                if (_remainFlux - flux < 0)
                    throw new UnityException("not enough flux!");

                _remainFlux -= flux;
                var up = new RiverNode(pos, flux);
                up._downstream = this;
                _upstream.Add(up);
            }


        }


        public RiverNode _root;

        public List<GameObject> GenerateRiverObjects()
        {
            List<Mesh> meshes = new List<Mesh>();
            Queue<RiverNode> nodeQueue = new Queue<RiverNode>();
            nodeQueue.Enqueue(_root);

            while (nodeQueue.Count > 0)
            {
                var curr = nodeQueue.Dequeue();
                foreach (var up in curr._upstream)
                {

                    meshes.Add(BuildSegmentMesh(curr, up));
                    nodeQueue.Enqueue(up);
                }
            }

            List<GameObject> objs = new List<GameObject>();
            foreach (var mesh in meshes)
            {
                var obj = new GameObject();
                obj.transform.parent = this._transform;
                obj.AddComponent<MeshFilter>();
                obj.AddComponent<MeshRenderer>();
                obj.AddComponent<UnityStandardAssets.Water.Water>();
                obj.GetComponent<MeshFilter>().mesh = mesh;
                obj.GetComponent<MeshRenderer>().material = _waterMat;
                objs.Add(obj);

            }

            return objs;
        }

        public List<CylinderModifier> GenerateModifier()
        {
            List<CylinderModifier> modifiers = new List<CylinderModifier>();
            Queue<RiverNode> nodeQueue = new Queue<RiverNode>();
            nodeQueue.Enqueue(_root);

            while (nodeQueue.Count > 0)
            {
                var curr = nodeQueue.Dequeue();
                foreach (var up in curr._upstream)
                {
                    var axis = up._pos - curr._pos;
                    var modifier = new CylinderModifier(curr._pos, axis.normalized, axis.magnitude, up._flux, false);
                    modifiers.Add(modifier);
                    nodeQueue.Enqueue(up);
                }
            }

            return modifiers;
        }

        private Mesh BuildSegmentMesh(RiverNode downStream, RiverNode upStream)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();
            Vector3 centralAxis = (upStream._pos - downStream._pos); //axis

            int radialVertCount = (Mathf.FloorToInt(upStream._flux / 5f) + 1) * 20;
            int dispTime = Mathf.Min(5, (Mathf.FloorToInt(centralAxis.magnitude / 5f) + 1));
            float dispScale = 0.1f;


            Vector3 downProj = Vector3.ProjectOnPlane(Vector3.down, centralAxis);
            if (Mathf.Approximately(downProj.magnitude, 0f))
                throw new UnityException("no waterfall at the very beginning");
            downProj.Normalize();
            float radialAngleInc = 1 / (float)(radialVertCount - 1) * 180f;
            for (int r = 0; r < radialVertCount; r++)
            {
                var radialoffset = Quaternion.AngleAxis(-90 + radialAngleInc * (float)r, centralAxis) * downProj * upStream._flux;
                var vert = downStream._pos + radialoffset;
                vertices.Add(vert);
            }

            //generate midpoint displacement here
            if (dispTime > 0)
            {
                float[,] disp = new float[(int)Mathf.Pow(2, dispTime) - 1, radialVertCount];

                Queue<Int2> indexQueue = new Queue<Int2>();
                int mid = disp.GetLength(0) / 2;
                indexQueue.Enqueue(new Int2(mid, dispTime));
                while (indexQueue.Count > 0)
                {
                    var curr = indexQueue.Dequeue();
                    var currIndex = curr._x;
                    var currLevel = curr._y;
                    var currScale = Mathf.Pow(2, currLevel - dispTime) * dispScale;
                    for (int r = 0; r < radialVertCount; r++)
                        disp[currIndex, r] = Random.Range(-currScale, currScale);

                    currLevel--;
                    if (currLevel > 0)
                    {
                        indexQueue.Enqueue(new Int2(currIndex - currLevel, currLevel));
                        indexQueue.Enqueue(new Int2(currIndex + currLevel, currLevel));
                    }
                }


                //add displaced vertices (and the other end)
                Vector3 axisInc = centralAxis / Mathf.Pow(2, dispTime);
                Vector3 axisNode = downStream._pos;
                for (int i = 0; i <= disp.GetLength(0); i++)
                {
                    axisNode += axisInc;
                    if (i != disp.GetLength(0))
                    {
                        for (int r = 0; r < radialVertCount; r++)
                        {
                            var vert = vertices[r] + (float)(i + 1) * axisInc;
                            vert += (vert - axisNode).normalized * disp[i, r];
                            vertices.Add(vert);
                        }
                    }
                    else
                    {
                        for (int r = 0; r < radialVertCount; r++)
                            vertices.Add(vertices[r] + (float)(i + 1) * axisInc);
                    }
                    //connect triangles
                    for (int r = 0; r < radialVertCount; r++)
                    {
                        indices.Add((i + 1) * radialVertCount + r % radialVertCount);
                        indices.Add(i * radialVertCount + r % radialVertCount);
                        indices.Add(i * radialVertCount + (r + 1) % radialVertCount);

                        indices.Add((i + 1) * radialVertCount + r % radialVertCount);
                        indices.Add(i * radialVertCount + (r + 1) % radialVertCount);
                        indices.Add((i + 1) * radialVertCount + (r + 1) % radialVertCount);
                    }
                }
            }


            Mesh segmentMesh = new Mesh();
            segmentMesh.vertices = vertices.ToArray();
            segmentMesh.triangles = indices.ToArray();
            return segmentMesh;
        }

        public void Init()
        {
            _root = new RiverNode(new Vector3(0f,4f,8f), 2f);
            _root.AddUpStream(new Vector3(10f, 4f, 8f), 2f);
            _root._upstream[0].AddUpStream(new Vector3(20f, 4f, 16f), 1f);
            _root._upstream[0].AddUpStream(new Vector3(20f, 4f, 0f), 1f);
        }

    }

}