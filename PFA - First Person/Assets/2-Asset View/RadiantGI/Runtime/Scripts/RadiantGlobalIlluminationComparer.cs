using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Pass = RadiantGI.RadiantGlobalIllumination.Pass;
using ShaderParams = RadiantGI.RadiantGlobalIllumination.ShaderParams;

namespace RadiantGI {

    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [HelpURL("https://kronnect.com/guides-category/radiant-gi/")]
    public class RadiantGlobalIlluminationComparer : MonoBehaviour {

        class RadiantComparePass {

            Material mat;
            RadiantGlobalIllumination radiant;

            public bool Setup(RadiantGlobalIllumination radiant) {

                Camera cam = Camera.current;
                this.radiant = radiant;
                if (radiant == null || !radiant.IsActive() || radiant.debugView != DebugView.None || cam.cameraType != CameraType.Game) return false;

#if UNITY_EDITOR
                if (cam.cameraType == CameraType.SceneView && !radiant.showInSceneView) return false;
                if (!Application.isPlaying && !radiant.showInEditMode) return false;
#endif

                if (!radiant.compareMode) return false;

                if (mat == null) {
                    mat = new Material(Shader.Find("Hidden/Kronnect/RadiantGI"));
                }

                // setup stencil
                mat.SetInt(ShaderParams.StencilValue, radiant.stencilValue);
                mat.SetInt(ShaderParams.StencilCompareFunction, radiant.stencilCheck ? (int)radiant.stencilCompareFunction : (int)CompareFunction.Always);

                return true;
            }


            public void Execute(RenderTexture source, RenderTexture destination) {

                Camera cam = Camera.current;
                mat.DisableKeyword(ShaderParams.SKW_FORWARD);
                mat.DisableKeyword(ShaderParams.SKW_FORWARD_AUTONORMALS);
                if (cam.actualRenderingPath == RenderingPath.Forward) {
                    if (radiant.normalsQuality == RadiantGlobalIllumination.NormalsQuality.ApproximatedFromDepth) {
                        mat.EnableKeyword(ShaderParams.SKW_FORWARD_AUTONORMALS);
                    } else {
                        mat.EnableKeyword(ShaderParams.SKW_FORWARD);
                    }
                }
                if (radiant.virtualEmitters) {
                    mat.EnableKeyword(ShaderParams.SKW_VIRTUAL_EMITTERS);
                } else {
                    mat.DisableKeyword(ShaderParams.SKW_VIRTUAL_EMITTERS);
                }
                if (radiant.nearFieldObscurance > 0) {
                    mat.EnableKeyword(ShaderParams.SKW_USES_NEAR_FIELD_OBSCURANCE);
                } else {
                    mat.DisableKeyword(ShaderParams.SKW_USES_NEAR_FIELD_OBSCURANCE);
                }

                float angle = radiant.compareSameSide ? Mathf.PI * 0.5f : radiant.compareLineAngle;
                mat.SetVector(ShaderParams.CompareParams, new Vector4(Mathf.Cos(angle), Mathf.Sin(angle), radiant.compareSameSide ? radiant.comparePanning : -10, radiant.compareLineWidth));

                FullScreenBlit(source, ShaderParams.RTs[ShaderParams.InputRT], Pass.CopyExact); // include transparent objects in the original compare texture
                Shader.SetGlobalTexture(ShaderParams.CompareTexNameId, ShaderParams.RTs[ShaderParams.CompareTex]);
                FullScreenBlit(ShaderParams.RTs[RadiantGlobalIllumination.computedGIRT], ShaderParams.RTs[ShaderParams.CompareTex], Pass.Compose); // add gi over transparent objects
                Graphics.Blit(source, destination, mat, (int)Pass.Compare);
            }

            void FullScreenBlit(RenderTexture source, RenderTexture destination, Pass pass) {
                Graphics.Blit(source, destination, mat, (int)pass);
            }

            public void Cleanup() {
                if (mat != null) {
                    DestroyImmediate(mat);
                }
            }
        }

        RadiantGlobalIllumination radiant;
        RadiantComparePass comparePass;
        Camera cam;


        private void OnEnable() {
            radiant = GetComponent<RadiantGlobalIllumination>();
            if (comparePass == null) {
                comparePass = new RadiantComparePass();
            }
            cam = GetComponent<Camera>();
        }

        void OnDestroy() {
            if (comparePass != null) {
                comparePass.Cleanup();
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination) {
            if (comparePass.Setup(radiant)) {
                comparePass.Execute(source, destination);
            } else {
                Graphics.Blit(source, destination);
            }
            ShaderParams.CleanUp();
        }

    }

}

