//PGRTerrain: Procedural Generation and Rendering of Terrain
//DH2323 Course Project in KTH
//RiverRenderer.cs
//Yang Zhou: yanzho@kth.se
//Yanbo Huang: yanboh@kth.se
//Huiting Wang: huitingw@kth.se
//2015.5

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PGRTerrain.Generation;
using Int2 = PGRTerrain.MathHelper.Int2;

namespace PGRTerrain.Render
{
    public class RiverRenderer
    {
        public Transform _transform;
        
        public Material _waterMat;
        private Island _island;
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

            public RiverNode(River river)
            {
                _pos = new Vector3((float)river.data.position[0], 
                                    river.data.elevation, 
                                    (float)river.data.position[1]);
                _flux = river.discharge;
                _remainFlux = _flux;
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

        private class RiverTuple
        {
            public River _dataNode;
            public RiverNode _renderNode;
            public RiverTuple(River dataNode, RiverNode renderNode)
            {
                _dataNode = dataNode;
                _renderNode = renderNode;
            }
        }

        public RiverRenderer(Transform transform, Material mat, Island island, River river)
        {
            _transform = transform;
            _waterMat = mat;
            _island = island;

            _root = new RiverNode(river);
            
            Queue<RiverTuple> riverQueue = new Queue<RiverTuple>();
            riverQueue.Enqueue(new RiverTuple(river, _root));
            while (riverQueue.Count > 0)
            {
                var currNode = riverQueue.Dequeue();
                if(currNode._dataNode.left!=null)
                {
                    var pos = new Vector3((float)currNode._dataNode.left.data.position[0], 
                                    currNode._dataNode.left.data.elevation, 
                                    (float)currNode._dataNode.left.data.position[1]);
                    currNode._renderNode.AddUpStream(pos, currNode._dataNode.left.discharge);
                    riverQueue.Enqueue(new RiverTuple(currNode._dataNode.left,
                        currNode._renderNode._upstream[currNode._renderNode._upstream.Count - 1]));
                }
                if(currNode._dataNode.right!=null)
                {
                    var pos = new Vector3((float)currNode._dataNode.right.data.position[0],
                                    currNode._dataNode.right.data.elevation,
                                    (float)currNode._dataNode.right.data.position[1]);
                    currNode._renderNode.AddUpStream(pos, currNode._dataNode.right.discharge);
                    riverQueue.Enqueue(new RiverTuple(currNode._dataNode.right,
                       currNode._renderNode._upstream[currNode._renderNode._upstream.Count - 1]));
                }
            }

        }

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
                obj.name = "RiverBranch";
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
            float dispScale = 0.5f;


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
                    axisNode.y = _island.GetElevation(new BenTools.Mathematics.Vector(axisNode.x, axisNode.z));
                    if (i != disp.GetLength(0))
                    {
                        for (int r = 0; r < radialVertCount; r++)
                        {
                            var vert = vertices[r] + (float)(i + 1) * axisInc;
                            vert = axisNode + (vert - axisNode) * (1f + disp[i, r]);
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

    }

}