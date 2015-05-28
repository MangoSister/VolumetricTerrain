//PGRTerrain: Procedural Generation and Rendering of Terrain
//DH2323 Course Project in KTH
//SceneManager.cs
//Yang Zhou: yanzho@kth.se
//Yanbo Huang: yanboh@kth.se
//Huiting Wang: huitingw@kth.se
//2015.5

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PGRTerrain.Render;

/// <summary>
/// This is the manager class of the demo application
/// handles user inputs and control flows
/// </summary>
[RequireComponent(typeof(TerrainEngine))]
public class SceneManager : MonoBehaviour 
{
    public Camera _roamCamera;
    public Canvas _canvas;
    public static readonly int worldSize = 256;
    public static readonly float maxElevation = 70;
    public InputField _inputSeed;
    public Slider _sliderPolygonNum;
    public Slider _sliderRelaxation;
    public Slider _sliderRiverNum;
    public Slider _sliderMainStreamLengthRatio;
    public Slider _sliderSubStreamLengthRatio;
    public Slider _sliderRiverSplitFreq;
    
    public Slider _sliderVoxelScale;
    public Toggle _toggleMultiUv;
    public Toggle _toggleProcDetail;
    public Material _vtBasic;
    public Material _vtMultiUv;
    public Material _vtProcDetail;
    public Material _vtBoth;

    public Texture2D _editCursor;
    private bool _editMode;
    public float _maxEditDist = 1000;
    public float _editRadius = 10;
    public void LaunchEngine()
    {
        var seed = System.Convert.ToInt32(_inputSeed.text);
        var polyNum = (int)_sliderPolygonNum.value;
        var relaxation = (int)_sliderRelaxation.value;
        var riverNum = (int)_sliderRiverNum.value;
        var mainStreamLengthRatio = _sliderMainStreamLengthRatio.value;
        var subStreamLengthRatio = _sliderSubStreamLengthRatio.value;
        var riverSplitFreq = _sliderRiverSplitFreq.value;

        var voxelScale = (int)_sliderVoxelScale.value;
        var multiUv = _toggleMultiUv.isOn;
        var procDetail = _toggleProcDetail.isOn;

        if (!multiUv && !procDetail)
        { GetComponent<TerrainEngine>()._vtMaterial = _vtBasic; }
        else if (multiUv && !procDetail)
        { GetComponent<TerrainEngine>()._vtMaterial = _vtMultiUv; }
        else if (!multiUv && procDetail)
        { GetComponent<TerrainEngine>()._vtMaterial = _vtProcDetail; }
        else
        { GetComponent<TerrainEngine>()._vtMaterial = _vtBoth; }

        GetComponent<TerrainEngine>().Init(worldSize, worldSize,
                                    maxElevation, relaxation, polyNum,
                                    riverNum, mainStreamLengthRatio, subStreamLengthRatio, riverSplitFreq,
                                    voxelScale,
                                    seed);

        _roamCamera.gameObject.GetComponent<FlyCamera>().enabled = true;
        _canvas.enabled = false;
        _editMode = false;

        _roamCamera.transform.position = new Vector3(0, 50, 0);
        _roamCamera.transform.LookAt(new Vector3(50, 20, 50));
    }

    private void Start()
    {
        _roamCamera.gameObject.GetComponent<FlyCamera>().enabled = false;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q))
        {
            if(!_canvas.enabled)
            { 
                _canvas.enabled = true;
                _roamCamera.gameObject.GetComponent<FlyCamera>().enabled = false;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                _editMode = false;
            }
            else
            { 
                _canvas.enabled = false;
                _roamCamera.gameObject.GetComponent<FlyCamera>().enabled = true;
            }
        }

        if(Input.GetKeyDown(KeyCode.E) && !_canvas.enabled)
        {
            _roamCamera.gameObject.GetComponent<FlyCamera>().enabled = !_roamCamera.gameObject.GetComponent<FlyCamera>().enabled;
            _editMode = !_editMode;
            if (_editMode)
                Cursor.SetCursor(_editCursor, Vector2.zero, CursorMode.Auto);
            else Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        if (_editMode)
        {
            if(Input.GetMouseButton(0))
            {
                Ray ray = _roamCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitInfo;

                if (Physics.Raycast(ray, out hitInfo, _maxEditDist, 1 << VoxelTerrain.voxelTerrainLayer))
                    GetComponent<TerrainEngine>().ModifyTerrain(hitInfo.point, _editRadius, false);
            }
            if(Input.GetMouseButton(1))
            {
                Ray ray = _roamCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo, _maxEditDist, 1 << VoxelTerrain.voxelTerrainLayer))
                    GetComponent<TerrainEngine>().ModifyTerrain(hitInfo.point, _editRadius, true);
            }
        }
    }
}