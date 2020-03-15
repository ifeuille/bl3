using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;
    public float backBufferScaleFactor = 1.0f;
    private RenderTexture depthRT;
    private RenderTexture colorRT;
    private int _CurrentScreenWidth = -1;
    private int _CurrentScreenHeight = -1;
    private bool _initCameraResources = false;
    public Camera _mainCamera;
    public FilterMode filterMode = FilterMode.Point;
    public RenderTexture DebugRT1;
    public RenderTexture DebugRT2;
   

    CommandBuffer command;
    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        //_CurrentScreenWidth = Screen.width;
        //_CurrentScreenHeight = Screen.height;

        _mainCamera = Camera.main;
        command = new CommandBuffer();
        ReInit();
    }

    public RenderTexture GetColorRT()
    {
        return colorRT;
    }
    public RenderTexture GetDepthRT()
    {
        return depthRT;
    }
    public RenderTargetIdentifier GetColorRTID()
    {
        return new RenderTargetIdentifier(colorRT);
    }
    public RenderTargetIdentifier GetDepthRTID()
    {
        return new RenderTargetIdentifier(depthRT);
    }
    public Camera GetMainCamera()
    {
        return _mainCamera;
    }
    public int GetWidth()
    {
        return _CurrentScreenWidth;
    }
    public int GetHeight()
    {
        return _CurrentScreenHeight;
    }
    void _CreateCameraResources()
    {
        _initCameraResources = true;
        RenderTextureFormat colorRTFormat = RenderTextureFormat.RGB111110Float;
        depthRT = new RenderTexture(_CurrentScreenWidth, _CurrentScreenHeight, 32, RenderTextureFormat.Depth);
        depthRT.name = "MainDepthBuffer";
        depthRT.filterMode = FilterMode.Point;
        depthRT.useMipMap = false;
        colorRT = new RenderTexture(_CurrentScreenWidth, _CurrentScreenHeight, 0, colorRTFormat);
        colorRT.name = "MainColorBuffer";
        ////这里需要添加判断,R8基本都支持，但精度不够,RHalf小米4不支持，雷电模拟器RFloat和RHalf都不支持
        //depthTexture = new RenderTexture(_CurrentScreenWidth, _CurrentScreenHeight, 0, RenderTextureFormat.RFloat);
        //depthTexture.name = "ScreenDepthTex";
        //depthTexture.autoGenerateMips = false;
        //depthTexture.useMipMap = false;
        //depthTexture.filterMode = FilterMode.Point;
    }
    void _ReleaseCameraResources()
    {
        depthRT.Release();
        colorRT.Release();
        m_OutlineTexture.Release();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ReInit()
    {
        if (_mainCamera == null) return;
        int sw = (int)(Screen.width / backBufferScaleFactor);
        int sh = (int)(Screen.height / backBufferScaleFactor);
        if (sw != _CurrentScreenWidth || sh != _CurrentScreenHeight)
        {
            if (_initCameraResources)
                _ReleaseCameraResources();
            _CurrentScreenWidth = sw;
            _CurrentScreenHeight = sh;
            _CreateCameraResources();
            m_OutlineTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RG16);
            m_OutlineTexture.name = "Outline RT";
            m_OutlineTexture.enableRandomWrite = true;
            m_OutlineTexture.Create();
            //UpdateCommand();
        }
    }

    public ComputeShader outLineComputeShader;
    public Shader uberShader;
    public Material material;
    private RenderTexture m_OutlineTexture;
    public struct CmdBufferEntry
    {
        public CommandBuffer m_AfterScene;
    }
    private Dictionary<Camera, CmdBufferEntry> m_Cameras = new Dictionary<Camera, CmdBufferEntry>();


    private void OnPreRender()
    {
        if (_mainCamera == null) return;
        ReInit();
        _mainCamera.SetTargetBuffers(colorRT.colorBuffer, depthRT.depthBuffer);
        //_mainCamera.depthTextureMode = DepthTextureMode.Depth;
        UpdateCommand();
    }
    private void UpdateCommand()
    {
        var cam = _mainCamera;
        if (!cam) return;
        CmdBufferEntry buf = new CmdBufferEntry();
        if (m_Cameras.ContainsKey(cam))
        {
            // use existing command buffers: clear them
            buf = m_Cameras[cam];
            buf.m_AfterScene.Clear();
        }
        else
        {
            buf.m_AfterScene = new CommandBuffer();
            buf.m_AfterScene.name = "DrawOutline";
            m_Cameras[cam] = buf;
            cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, buf.m_AfterScene);
        }
        //outLineComputeShader.SetTexture(outLineComputeShader.FindKernel("CSMain"), "_OutlineTexture", m_OutlineTexture);
        //outLineComputeShader.SetTextureFromGlobal(
        //    outLineComputeShader.FindKernel("CSMain"),
        //    Shader.PropertyToID("_DepthTexture"), 
        //    Shader.PropertyToID("_DepthTexture"));
        int x = (int)(Screen.width / 10 + 0.5);
        int y = (int)(Screen.height / 10 + 0.5);
        //outLineComputeShader.Dispatch(outLineComputeShader.FindKernel("CSMain"), x, y, 1);

        var cb = buf.m_AfterScene;
        cb.Clear();
        cb.SetGlobalTexture("_OutlineTexture", m_OutlineTexture);
        cb.SetGlobalTexture("_DepthTexture", GetDepthRTID());

        int computeKenel = outLineComputeShader.FindKernel("CSMain");
        cb.SetComputeTextureParam(outLineComputeShader, computeKenel, Shader.PropertyToID("_DepthTexture"), GetDepthRTID());
        cb.SetComputeTextureParam(outLineComputeShader, computeKenel, Shader.PropertyToID("_OutlineTexture"), m_OutlineTexture);
        //cb
        cb.SetComputeVectorParam(outLineComputeShader, Shader.PropertyToID("screenSize"),
            new Vector4(Screen.width, Screen.height, 1.0f / Screen.width, 1.0f / Screen.height));

        cb.DispatchCompute(outLineComputeShader, computeKenel, x, y, 1);
        //cb.SetRenderTarget(GetColorRTID(), GetDepthRTID());
        //cb.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
        cb.Blit(m_OutlineTexture, GetColorRTID());
    }


    void OnPostRender()
    {
        
        Graphics.Blit(colorRT, null as RenderTexture);
    }

    //void OnRenderImage(RenderTexture src, RenderTexture dest)
    //{
    //    // set our filtering mode and blit to the screen
    //    src.filterMode = filterMode;

    //    Graphics.Blit(colorRT, dest);

    //    //RenderTexture.ReleaseTemporary(rt);
    //}

}
