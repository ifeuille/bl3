using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BorderLand3 : MonoBehaviour
{
    public new Camera camera;
    public ComputeShader outLineComputeShader;
    public Shader uberShader;
    public Material material;
    private RenderTexture m_OutlineTexture;
    public CameraManager cameraManager;

    public struct CmdBufferEntry
    {
        public CommandBuffer m_AfterScene;
    }
    private Dictionary<Camera, CmdBufferEntry> m_Cameras = new Dictionary<Camera, CmdBufferEntry>();

    private void Awake()
    {        
        m_OutlineTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RG16);
        m_OutlineTexture.name = "Outline RT";
        m_OutlineTexture.enableRandomWrite = true;
        m_OutlineTexture.Create();

        

    }
    private void OnDestroy()
    {
        m_OutlineTexture.Release();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    //private void OnRenderImage(RenderTexture source, RenderTexture destination)
    //{
    //    Graphics.Blit(source, destination,material);
    //}

    private void UpdateCommand()
    {
        var cam = camera;
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
        cb.SetGlobalTexture("_DepthTexture", cameraManager.GetDepthRTID());
        
        cb.DispatchCompute(outLineComputeShader, outLineComputeShader.FindKernel("CSMain"), x,y,1);

        cb.SetRenderTarget(cameraManager.GetColorRTID(), cameraManager.GetDepthRTID());    
        cb.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
        
    }

}
