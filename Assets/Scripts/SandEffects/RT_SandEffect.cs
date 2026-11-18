using UnityEngine;
using UnityEngine.InputSystem;

namespace SandEffects
{
    public class RT_SandEffect : MonoBehaviour
    {
        public RenderTexture sandRT;
        public Texture drawTex;
        public Material blendMaterial;
        public Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
            if (sandRT == null) return;

            //清空RT
            RenderTexture previousRT = RenderTexture.active;
            RenderTexture.active = sandRT;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = previousRT;

            GetComponent<Renderer>().material.mainTexture = sandRT;
        }

        private void DrawSandRT(RenderTexture rt, Texture image, int x, int y)
        {
            if (rt)
            {
                RenderTexture.active = rt;
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, rt.width, 0, rt.height);

                x -= (int)(image.width * 0.5);
                y -= (int)(image.height * 0.5);
                Rect rect = new Rect(x, y, image.width, image.height);
                Graphics.DrawTexture(rect, image, blendMaterial);
                GL.PopMatrix();

                RenderTexture.active = null;
            }
        }

        private void Update()
        {
#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
            if (Mouse.current.leftButton.isPressed)
            {
                Debug.Log("Mouse Left");
                Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                    DrawSandRT(sandRT, drawTex, (int)(hit.textureCoord.x * sandRT.width),
                        (int)(hit.textureCoord.y * sandRT.height));
            }
#endif
        }
    }
}