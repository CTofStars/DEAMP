using UnityEngine;

public class DEAMP_SingleEye : MonoBehaviour
{
    public RenderTexture TexturePass0;
    RenderTexture TexturePass1 = null;
    RenderTexture TexturePass2;
    RenderTexture TextureDenoise;

    RenderTexture TextureDenoise1;
    RenderTexture TextureDenoise2;

    public Material Pass1Material;
    public Material Pass2Material;
    public Material DenoiseMaterial;

    public Material MergeMaterial;
    public Material PointMaterial;

    const int width = 1024;
    const int height = 1024;

    [Range(1.00f, 4.0f)]
    float sigma = 1f;
    [Range(0.01f, 0.99f)]
    float fx = 0.38f;
    [Range(0.01f, 0.99f)]
    float fy = 0.38f;
    [Range(0.01f, 0.99f)]
    public float eyeX;
    [Range(0.01f, 0.99f)]
    public float eyeY;
    int applyPass1;
    int applyPass2;
    int applyDenoise;

    const float MAX_SIGMA = 8.0f;

    [Range(1.00f, MAX_SIGMA)]
    public float sigmaF = 1.0f;
    [Range(1.00f, MAX_SIGMA)]
    public float sigmaM = 1.0f;
    [Range(1.00f, 8.0f)]
    public float sigmaP = 1.0f;

    [Range(1.0f, 2.8f)]
    float sigma0 = 0.3f;

    public static int showEdges = 0;

    [Range(0.00f, 0.99f)]
    public float boundF;
    [Range(0.00f, 0.99f)]
    public float boundM;
    [Range(0.00f, 0.99f)]
    public float boundP;

    [Range(0.0f, 180.0f)]
    public float fov_deg = 20.0f;
    [Range(0.0f, 180.0f)]
    public float medium_deg = 45.0f;
    [Range(0.0f, 180.0f)]
    public float peri_deg = 90.0f;

    public static float eX, eY;

    private Camera camera;

    public static bool blockAllKeys = false;

    private float imgdst = 1.0f;
    float angle2viewdst(float x)
    {
        float rad = Mathf.Deg2Rad * x;
        float worlddst = imgdst * Mathf.Tan(rad / 2);
        Vector3 edgePoint = new Vector3(-worlddst, 0.0f, imgdst);
        Vector3 worldPoint1 = camera.transform.TransformPoint(edgePoint);
        Vector3 screenEdge = camera.WorldToViewportPoint(worldPoint1);

        return Vector2.Distance(new Vector2(screenEdge.x / screenEdge.z, screenEdge.y / screenEdge.z),
            new Vector2(0.5f, 0.5f));
    }

    // Start is called before the first frame update
    void Start()
    {
        camera = gameObject.GetComponent<Camera>();

        sigma0 = sigma;
        applyPass1 = 1;
        applyPass2 = 1;
        applyDenoise = 1;

        TexturePass1 = new RenderTexture(Mathf.RoundToInt(Screen.width / sigma), Mathf.RoundToInt(Screen.height / sigma), 24, RenderTextureFormat.Default);
        TexturePass1.Create();

        TexturePass2 = new RenderTexture(Mathf.RoundToInt(Screen.width), Mathf.RoundToInt(Screen.height), 24, RenderTextureFormat.Default);
        TexturePass2.Create();

        TextureDenoise = new RenderTexture(Mathf.RoundToInt(Screen.width), Mathf.RoundToInt(Screen.height), 24, RenderTextureFormat.Default);
        TextureDenoise.Create();

        TextureDenoise1 = new RenderTexture(Mathf.RoundToInt(Screen.width), Mathf.RoundToInt(Screen.height), 24, RenderTextureFormat.Default);
        TextureDenoise1.Create();

        TextureDenoise2 = new RenderTexture(Mathf.RoundToInt(Screen.width), Mathf.RoundToInt(Screen.height), 24, RenderTextureFormat.Default);
        TextureDenoise2.Create();
    }

    // Update is called once per frame
    void Update()
    {
        boundF = angle2viewdst(fov_deg);
        boundM = angle2viewdst(medium_deg);
        boundP = angle2viewdst(peri_deg);
        eX = eyeX;
        eY = eyeY;
        keyControl();
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (applyPass1 + applyPass2 + applyDenoise == 0)
        {
            PointMaterial.SetFloat("_eyeX", eyeX);
            PointMaterial.SetFloat("_eyeY", eyeY);
            Graphics.Blit(TexturePass0, dst, PointMaterial);
            return;
        }

        sigma0 = sigmaF;
        if (sigma0 != sigma)
        {
            updateTextureSize();
            sigma = sigma0;
        }
        Pass1MainL();
        Pass2MainL();
        Pass3DenoiseL();

        sigma0 = sigmaM;
        if (sigma0 != sigma)
        {
            updateTextureSize();
            sigma = sigma0;
        }
        Pass1MainL();
        Pass2MainL();
        Pass3Denoise1L();


        sigma0 = sigmaP;
        if (sigma0 != sigma)
        {
            updateTextureSize();
            sigma = sigma0;
        }
        Pass1MainL();
        Pass2MainL();
        Pass3Denoise2L();

        MergeMaterial.SetFloat("_iResolutionX", width);
        MergeMaterial.SetFloat("_iResolutionY", height);
        MergeMaterial.SetFloat("_eyeX", eyeX);
        MergeMaterial.SetFloat("_eyeY", eyeY);
        MergeMaterial.SetFloat("_boundF", boundF);
        MergeMaterial.SetFloat("_boundM", boundM);
        MergeMaterial.SetFloat("_boundP", boundP);

        MergeMaterial.SetInt("_showEdges", showEdges);

        MergeMaterial.SetTexture("_Tex1", TextureDenoise);
        MergeMaterial.SetTexture("_Tex2", TextureDenoise1);
        MergeMaterial.SetTexture("_Tex3", TextureDenoise2);

        Graphics.Blit(TextureDenoise, dst, MergeMaterial);
    }

    void Pass1MainL()
    {
        Pass1Material.SetFloat("_eyeX", eyeX);
        Pass1Material.SetFloat("_eyeY", eyeY);
        Pass1Material.SetFloat("_scaleRatio", sigma);
        Pass1Material.SetFloat("_fx", fx);
        Pass1Material.SetFloat("_fy", fy);
        Pass1Material.SetInt("_ApplyPass1", applyPass1);
        Graphics.Blit(TexturePass0, TexturePass1, Pass1Material);
    }
    void Pass2MainL()
    {
        Pass2Material.SetFloat("_eyeX", eyeX);
        Pass2Material.SetFloat("_eyeY", eyeY);
        Pass2Material.SetFloat("_scaleRatio", sigma);
        Pass2Material.SetFloat("_fx", fx);
        Pass2Material.SetFloat("_fy", fy);
        Pass2Material.SetInt("_ApplyPass2", applyPass2);
        Pass2Material.SetTexture("_MidTex", TexturePass1);
        Graphics.Blit(null, TexturePass2, Pass2Material);
    }

    void Pass3DenoiseL()
    {
        DenoiseMaterial.SetFloat("_iResolutionX", Screen.width);
        DenoiseMaterial.SetFloat("_iResolutionY", Screen.height);
        DenoiseMaterial.SetFloat("_eyeX", eyeX);
        DenoiseMaterial.SetFloat("_eyeY", eyeY);
        DenoiseMaterial.SetTexture("_Pass2Tex", TexturePass2);
        DenoiseMaterial.SetInt("_ApplyDenoise", applyDenoise);

        Graphics.Blit(null, TextureDenoise, DenoiseMaterial);
    }
    void Pass3Denoise1L()
    {
        DenoiseMaterial.SetFloat("_iResolutionX", Screen.width);
        DenoiseMaterial.SetFloat("_iResolutionY", Screen.height);
        DenoiseMaterial.SetFloat("_eyeX", eyeX);
        DenoiseMaterial.SetFloat("_eyeY", eyeY);
        DenoiseMaterial.SetTexture("_Pass2Tex", TexturePass2);
        DenoiseMaterial.SetInt("_ApplyDenoise", applyDenoise);

        Graphics.Blit(null, TextureDenoise1, DenoiseMaterial);
    }

    void Pass3Denoise2L()
    {
        DenoiseMaterial.SetFloat("_iResolutionX", Screen.width);
        DenoiseMaterial.SetFloat("_iResolutionY", Screen.height);
        DenoiseMaterial.SetFloat("_eyeX", eyeX);
        DenoiseMaterial.SetFloat("_eyeY", eyeY);
        DenoiseMaterial.SetTexture("_Pass2Tex", TexturePass2);
        Graphics.Blit(null, TextureDenoise2, DenoiseMaterial);
    }

    const float incDelta = 0.2f, eye_step = 0.05f;
    public char dominant_eye = 'l';
    public string mode = "dom";

    void keyControl()
    {
        if (blockAllKeys)
            return;
        if (Input.GetKeyDown(KeyCode.O) && isLeft())
        {
            showEdges = 1 - showEdges;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            eyeX = eyeX >= eye_step ? eyeX - eye_step : 0.0f;
        if (Input.GetKeyDown(KeyCode.RightArrow))
            eyeX = eyeX >= 1.0f - eye_step ? 1.0f : eyeX + eye_step;
        if (Input.GetKeyDown(KeyCode.DownArrow))
            eyeY = eyeY >= eye_step ? eyeY - eye_step : 0.0f;
        if (Input.GetKeyDown(KeyCode.UpArrow))
            eyeY = eyeY >= 1.0f - eye_step ? 1.0f : eyeY + eye_step;

        if (Input.GetKeyDown(KeyCode.F))
        {
            incF(incDelta);
            incM(incDelta);
            incP(incDelta);
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            incM(incDelta);
            incP(incDelta);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            incP(incDelta);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            sigmaF = 1;
            sigmaM = 1;
            sigmaP = 1;
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            mode = mode == "dom" ? "nondom" : "dom";
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            dominant_eye = dominant_eye == 'l' ? 'r' : 'l';
        }
       
    }

    void incF(float d)
    {
        if ((mode == "dom") || (mode == "nondom" && !isDom())) { 
            sigmaF += d;
            if (sigmaF > MAX_SIGMA) sigmaF = MAX_SIGMA;
        }
    }

    void incM(float d)
    {
        if ((mode == "dom") || (mode == "nondom" && !isDom()))
        {
            sigmaM += d;
            if (sigmaM > MAX_SIGMA) sigmaM = MAX_SIGMA;
        }
    }

    void incP(float d)
    {
        if ((mode == "dom") || (mode == "nondom" && !isDom()))
        {
            sigmaP += d;
            if (sigmaP > MAX_SIGMA) sigmaP = MAX_SIGMA;
        }
    }

    private bool isDom()
    {
        return (dominant_eye == 'l' && isLeft()) || (dominant_eye == 'r' && isRight());
    }

    private bool isRight()
    {
        return gameObject.name == "DisplayR";
    }

    private bool isLeft()
    {
        return gameObject.name == "DisplayL";
    }


    void updateTextureSize()
    {
        RenderTexture tempTexture = new RenderTexture(Mathf.RoundToInt(Screen.width / sigma0), Mathf.RoundToInt(Screen.height / sigma0), 24, RenderTextureFormat.Default);
        tempTexture.Create();

        TexturePass1.Release();
        TexturePass1 = tempTexture;
    }

    void DispText(int idx, float variable, string name)
    {
        GUIStyle guiStyle = new GUIStyle();
        guiStyle.fontSize = 50;
        string text = string.Format(name + " = {0}", variable);
        GUI.Label(new Rect(0, idx * 50, Screen.width, Screen.height), text, guiStyle);
    }

    void OnGUI()
    {
        int idx = 0;
        DispText(idx++, sigmaF, "sigmaF");
        DispText(idx++, sigmaM, "sigmaM");
        DispText(idx++, sigmaP, "sigmaP");
        DispText(idx++, eyeX, "eyeX");
        DispText(idx++, eyeY, "eyeY");

        GUIStyle guiStyle = new GUIStyle();
        guiStyle.fontSize = 50;
        string text = string.Format("dominant_eye" + " = {0}", dominant_eye);
        GUI.Label(new Rect(0, (idx++) * 50, Screen.width, Screen.height), text, guiStyle);

        guiStyle = new GUIStyle();
        guiStyle.fontSize = 50;
        text = string.Format("mode" + " = {0}", mode);
        GUI.Label(new Rect(0, idx * 50, Screen.width, Screen.height), text, guiStyle);

    }
}


