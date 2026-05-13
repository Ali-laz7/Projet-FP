using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RadiantGI {

    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [ImageEffectAllowedInSceneView]
    [HelpURL("https://kronnect.com/guides-category/radiant-gi/")]
    public class RadiantGlobalIllumination : MonoBehaviour {

        public enum RenderingPath {
            Forward,
            Deferred
        }

        public enum Pass {
            CopyExact,
            Raycast,
            BlurHorizontal,
            BlurVertical,
            Upscale,
            TemporalAccum,
            Albedo,
            Normals,
            Compose,
            Compare,
            FinalGIDebug,
            Specular,
            Copy,
            WideFilter,
            Depth,
            CopyDepth,
            RSM_Debug,
            RSM,
            NFO,
            NFOBlur,
            CopyMultiTaps
        }

        static readonly List<RadiantVolume> volumes = new List<RadiantVolume>();
        static readonly List<ReflectionProbe> probes = new List<ReflectionProbe>();
        static readonly List<RadiantVirtualEmitter> emitters = new List<RadiantVirtualEmitter>();
        static bool emittersForceRefresh;

        public static class ShaderParams {

            // textures
            public static int MainTex = Shader.PropertyToID("_MainTex");
            public static int NoiseTex = Shader.PropertyToID("_NoiseTex");
            public static int Probe1Cube = Shader.PropertyToID("_Probe1Cube");
            public static int Probe2Cube = Shader.PropertyToID("_Probe2Cube");

            // temporary targets
            public static int ResolveRT = 0; // _ResolveRT;
            public static int Downscaled1RT = 1; // _Downscaled1RT;
            public static int Downscaled1RTA = 2; // _Downscaled1RTA;
            public static int Downscaled2RT = 3; // _Downscaled2RT;
            public static int Downscaled2RTA = 4; // _Downscaled2RTA;
            public static int InputRT = 5; // _InputRTGI;
            public static int InputRTNameId = Shader.PropertyToID("_InputRTGI");
            public static int CompareTex = 6; // _CompareTexGI;
            public static int CompareTexNameId = Shader.PropertyToID("_CompareTexGI");
            public static int TempAcum = 7; // _TempAcum;
            public static int DownscaledDepthRT = 8; // _DownscaledDepthRT;
            public static int DownscaledDepthRTNameId = Shader.PropertyToID("_DownscaledDepthRT");
            public static int PrevResolveNameId = Shader.PropertyToID("_PrevResolve");
            public static int NFO_RT = 9; // Shader.PropertyToID("_NFO_RT");
            public static int NFOBlurRT = 10; // Shader.PropertyToID("_NFOBlurRT");
            public static int NFO_RT_NameId = Shader.PropertyToID("_NFO_RT");

            // uniforms
            public static int IndirectData = Shader.PropertyToID("_IndirectData");
            public static int RayData = Shader.PropertyToID("_RayData");
            public static int TemporalData = Shader.PropertyToID("_TemporalData");
            public static int WorldToViewDir = Shader.PropertyToID("_WorldToViewDir");
            public static int ViewToWorldDir = Shader.PropertyToID("_ViewToWorldDir");
            public static int CompareParams = Shader.PropertyToID("_CompareParams");
            public static int ExtraData = Shader.PropertyToID("_ExtraData");
            public static int ExtraData2 = Shader.PropertyToID("_ExtraData2");
            public static int ExtraData3 = Shader.PropertyToID("_ExtraData3");
            public static int ExtraData4 = Shader.PropertyToID("_ExtraData4");
            public static int ExtraData5 = Shader.PropertyToID("_ExtraData5");
            public static int EmittersPositions = Shader.PropertyToID("_EmittersPositions");
            public static int EmittersBoxMin = Shader.PropertyToID("_EmittersBoxMin");
            public static int EmittersBoxMax = Shader.PropertyToID("_EmittersBoxMax");
            public static int EmittersColors = Shader.PropertyToID("_EmittersColors");
            public static int EmittersCount = Shader.PropertyToID("_EmittersCount");
            public static int RSMIntensity = Shader.PropertyToID("_RadiantShadowMapIntensity");
            public static int InverseViewProjectionMatrix = Shader.PropertyToID("_InvViewProjection");
            public static int StencilValue = Shader.PropertyToID("_StencilValue");
            public static int StencilCompareFunction = Shader.PropertyToID("_StencilCompareFunction");
            public static int ProbeData = Shader.PropertyToID("_ProbeData");
            public static int Probe1HDR = Shader.PropertyToID("_Probe1HDR");
            public static int Probe2HDR = Shader.PropertyToID("_Probe2HDR");
            public static int BoundsXZ = Shader.PropertyToID("_BoundsXZ");
            public static int SourceSize = Shader.PropertyToID("_SourceSize");
            public static int DebugDepthMultiplier = Shader.PropertyToID("_DebugDepthMultiplier");
            public static int NFOTint = Shader.PropertyToID("_NFOTint");
            public static int OrganicLightData = Shader.PropertyToID("_OrganicLightData");
            public static int OrganicLightTint = Shader.PropertyToID("_OrganicLightTint");
            public static int OrganicLightOffset = Shader.PropertyToID("_OrganicLightOffset");

            // keywords
            public const string SKW_FORWARD = "_FORWARD";
            public const string SKW_FORWARD_AUTONORMALS = "_FORWARD_AUTONORMALS";
            public const string SKW_FORWARD_AND_DEFERRED = "_FORWARD_AND_DEFERRED";
            public const string SKW_COMPARE_MODE = "_COMPARE_MODE";
            public const string SKW_USES_BINARY_SEARCH = "_USES_BINARY_SEARCH";
            public const string SKW_USES_MULTIPLE_RAYS = "_USES_MULTIPLE_RAYS";
            public const string SKW_REUSE_RAYS = "_REUSE_RAYS";
            public const string SKW_FALLBACK_1_PROBE = "_FALLBACK_1_PROBE";
            public const string SKW_FALLBACK_2_PROBES = "_FALLBACK_2_PROBES";
            public const string SKW_VIRTUAL_EMITTERS = "_VIRTUAL_EMITTERS";
            public const string SKW_USES_NEAR_FIELD_OBSCURANCE = "_USES_NEAR_FIELD_OBSCURANCE";
            public const string SKW_ORTHO_SUPPORT = "_ORTHO_SUPPORT";
            public const string SKW_DISTANCE_BLENDING = "_DISTANCE_BLENDING";

            const int MAX_TARGET_COUNT = 11;
            public readonly static RenderTexture[] RTs = new RenderTexture[MAX_TARGET_COUNT];

            public static void CleanUp() {
                for (int k = 0; k < MAX_TARGET_COUNT; k++) {
                    if (RTs[k] != null) {
                        RenderTexture.ReleaseTemporary(RTs[k]);
                        RTs[k] = null;
                    }
                }
            }

        }

        public static int computedGIRT;


        class RadiantPass {

            const float GOLDEN_RATIO = 0.618033989f;
            const int MAX_EMITTERS = 32;

            class PerCameraData {
                public Vector3 lastCameraPosition;
                public RenderTexture rtAcum;
                public int rtAcumCreationFrame;
                public RenderTexture rtBounce;
                public int rtBounceCreationFrame;
                // emitters
                public float emittersSortTime = float.MinValue;
                public Vector3 emittersLastCameraPosition;
                public readonly List<RadiantVirtualEmitter> emittersSorted = new List<RadiantVirtualEmitter>();
            }

            RadiantGlobalIllumination radiant;
            RenderTextureDescriptor sourceDesc, cameraTargetDesc;

            readonly Dictionary<Camera, PerCameraData> prevs = new Dictionary<Camera, PerCameraData>();
            float goldenRatioAcum;
            RadiantVolume[] volumes;
            Material mat;
            static readonly Vector4 unlimitedBounds = new Vector4(-1e8f, -1e8f, 1e8f, 1e8f);
            Vector4[] emittersBoxMin, emittersBoxMax, emittersColors, emittersPositions;
            readonly Plane[] cameraPlanes = new Plane[6];
            Vector3 camPos;

            public void Setup(RadiantGlobalIllumination radiant) {

                this.radiant = radiant;
                if (mat == null) {
                    mat = new Material(Shader.Find("Hidden/Kronnect/RadiantGI"));
                    mat.SetTexture(ShaderParams.NoiseTex, Resources.Load<Texture>("RadiantGI/blueNoiseGI128RGB"));
                }
            }

            RenderTexture cameraTarget;
            bool drawnToDestination;

            public void Execute(RenderTexture source, RenderTexture destination, Camera cam) {
                cameraTarget = destination;
                drawnToDestination = false;
                ExecuteInternal(source, cam);
                if (!drawnToDestination) {
                    Graphics.Blit(source, destination, mat, (int)Pass.CopyExact);
                }
            }

            void ExecuteInternal(RenderTexture source, Camera cam) {

                if (!radiant.IsActive()) return;

                bool isSceneView = cam.cameraType == CameraType.SceneView;
                if (cam.cameraType != CameraType.Game && !isSceneView) {
                    return;
                }

#if UNITY_EDITOR
                if (isSceneView && !radiant.showInSceneView) return;
                if (!Application.isPlaying && !radiant.showInEditMode) return;
#endif

                sourceDesc = source.descriptor;
                sourceDesc.colorFormat = RenderTextureFormat.ARGBHalf;
                sourceDesc.useMipMap = false;
                sourceDesc.msaaSamples = 1;

                sourceDesc.depthBufferBits = 0;
                cameraTargetDesc = sourceDesc;

                float downsampling = radiant.downsampling;
                sourceDesc.width = (int)(sourceDesc.width / downsampling);
                sourceDesc.height = (int)(sourceDesc.height / downsampling);

                RenderGI(source, cam);

                if (!radiant.compareMode || isSceneView) {
                    ShaderParams.CleanUp();
                }
            }

            void RenderGI(RenderTexture source, Camera cam) {

                int smoothing = radiant.smoothing;
                DebugView debugView = radiant.debugView;
                bool usesBounce = radiant.rayBounce;
                int frameCount = Application.isPlaying ? Time.frameCount : 0;
                bool usesForward = cam.actualRenderingPath == UnityEngine.RenderingPath.Forward;
                bool combineForwardMaterials = radiant.includeForward && !usesForward;
                float normalMapInfluence = radiant.normalMapInfluence;
                float lumaInfluence = radiant.lumaInfluence > 0 ? radiant.lumaInfluence * 100f : 20000;
                float downsampling = radiant.downsampling;
                int currentFrame = Time.frameCount;
                bool usesRSM = RadiantShadowMap.installed && radiant.fallbackReflectiveShadowMap && radiant.reflectiveShadowMapIntensity > 0;
                bool usesEmitters = radiant.virtualEmitters;
                bool isGameView = cam.cameraType == CameraType.Game;
                bool usesReprojection = isGameView && radiant.temporalReprojection && Application.isPlaying; // camera motion vectors not available in SceneView
                bool usesCompareMode = isGameView && radiant.compareMode;

                camPos = cam.transform.position;
                if (usesReprojection) cam.depthTextureMode |= DepthTextureMode.MotionVectors;

                // pass radiant settings to shader
                mat.SetVector(ShaderParams.IndirectData, new Vector4(radiant.indirectIntensity, radiant.indirectMaxSourceBrightness, radiant.indirectDistanceAttenuation, radiant.rayReuse));
                mat.SetVector(ShaderParams.RayData, new Vector4(radiant.rayCount, radiant.rayMaxLength, radiant.rayMaxSamples, radiant.thickness));

                Shader.SetGlobalVector(ShaderParams.ExtraData2, new Vector4(radiant.brightnessThreshold, radiant.brightnessMax, radiant.saturation, radiant.reflectiveShadowMapIntensity)); // global because these params are needed by the compare pass

                mat.DisableKeyword(ShaderParams.SKW_FORWARD);
                mat.DisableKeyword(ShaderParams.SKW_FORWARD_AUTONORMALS);
                if (usesForward) {
                    if (radiant.normalsQuality == NormalsQuality.ApproximatedFromDepth) {
                        mat.EnableKeyword(ShaderParams.SKW_FORWARD_AUTONORMALS);
                    } else {
                        mat.EnableKeyword(ShaderParams.SKW_FORWARD);
                    }
                }

                if (combineForwardMaterials) {
                    mat.EnableKeyword(ShaderParams.SKW_FORWARD_AND_DEFERRED);
                } else {
                    mat.DisableKeyword(ShaderParams.SKW_FORWARD_AND_DEFERRED);
                }

                if (radiant.rayBinarySearch) {
                    mat.EnableKeyword(ShaderParams.SKW_USES_BINARY_SEARCH);
                } else {
                    mat.DisableKeyword(ShaderParams.SKW_USES_BINARY_SEARCH);
                }

                if (radiant.rayCount > 1) {
                    mat.EnableKeyword(ShaderParams.SKW_USES_MULTIPLE_RAYS);
                } else {
                    mat.DisableKeyword(ShaderParams.SKW_USES_MULTIPLE_RAYS);
                }

                float nearFieldObscurance = radiant.nearFieldObscurance;
                bool useNFO = nearFieldObscurance > 0;
                if (useNFO) {
                    Shader.SetGlobalVector(ShaderParams.ExtraData4, new Vector4(radiant.nearFieldObscuranceMaxCameraDistance, (1f - radiant.nearFieldObscuranceOccluderDistance) * 10f, 0, 0));
                    Shader.SetGlobalColor(ShaderParams.NFOTint, radiant.nearFieldObscuranceTintColor);
                    mat.EnableKeyword(ShaderParams.SKW_USES_NEAR_FIELD_OBSCURANCE);
                } else {
                    mat.DisableKeyword(ShaderParams.SKW_USES_NEAR_FIELD_OBSCURANCE);
                }

                if (cam.orthographic) {
                    mat.EnableKeyword(ShaderParams.SKW_ORTHO_SUPPORT);
                } else {
                    mat.DisableKeyword(ShaderParams.SKW_ORTHO_SUPPORT);
                }

                Shader.SetGlobalVector(ShaderParams.ExtraData3, new Vector4(0, radiant.nearFieldObscuranceSpread * 0.5f, 1f / (radiant.nearCameraAttenuation + 0.0001f), nearFieldObscurance)); // global because these params are needed by the compare pass

                // restricts to volume bounds
                SetupVolumeBounds();

                // setup stencil
                mat.SetInt(ShaderParams.StencilValue, radiant.stencilValue);
                mat.SetInt(ShaderParams.StencilCompareFunction, radiant.stencilCheck ? (int)radiant.stencilCompareFunction : (int)CompareFunction.Always);

                // pass reprojection & other raymarch data
                if (usesReprojection) {
                    goldenRatioAcum += GOLDEN_RATIO * radiant.rayCount;
                    goldenRatioAcum %= 5000;
                }
                Shader.SetGlobalVector(ShaderParams.SourceSize, new Vector4(cameraTargetDesc.width, cameraTargetDesc.height, goldenRatioAcum, frameCount));
                Shader.SetGlobalVector(ShaderParams.ExtraData, new Vector4(radiant.rayJitter, 1f, normalMapInfluence, lumaInfluence));
                Shader.SetGlobalVector(ShaderParams.ExtraData5, new Vector4(radiant.specularContribution, downsampling, radiant.sourceBrightness, radiant.giWeight));

                // pass UNITY_MATRIX_V and inverse
                Matrix4x4 viewMatrix = cam.worldToCameraMatrix;
                Shader.SetGlobalMatrix(ShaderParams.WorldToViewDir, viewMatrix);
                Shader.SetGlobalMatrix(ShaderParams.ViewToWorldDir, cam.cameraToWorldMatrix);

                if (!radiant.IsActive()) return;

                // pass UNITY_MATRIX_I_VP
                Matrix4x4 gpuProjectionMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
                Matrix4x4 inverseViewMatrix = Matrix4x4.Inverse(viewMatrix);
                Matrix4x4 inverseProjectionMatrix = Matrix4x4.Inverse(gpuProjectionMatrix);
                Matrix4x4 inverseViewProjection = inverseViewMatrix * inverseProjectionMatrix;
                Shader.SetGlobalMatrix(ShaderParams.InverseViewProjectionMatrix, inverseViewProjection);

                // create downscaled depth
                RenderTextureDescriptor downDesc = cameraTargetDesc;
                downDesc.width = Mathf.Min(sourceDesc.width, downDesc.width / 2);
                downDesc.height = Mathf.Min(sourceDesc.height, downDesc.height / 2);

                int downHalfDescWidth = downDesc.width;
                int downHalfDescHeight = downDesc.height;

                // copy depth into an optimized render target
                int downsamplingDepth = 9 - radiant.raytracerAccuracy;
                RenderTextureDescriptor rtDownDepth = sourceDesc;
                rtDownDepth.width = Mathf.CeilToInt((float)rtDownDepth.width / downsamplingDepth);
                rtDownDepth.height = Mathf.CeilToInt((float)rtDownDepth.height / downsamplingDepth);
#if UNITY_WEBGL
                rtDownDepth.colorFormat = RenderTextureFormat.RFloat;
#else
                rtDownDepth.colorFormat = RenderTextureFormat.RHalf;
#endif
                rtDownDepth.sRGB = false;
                GetTemporaryRT(ShaderParams.DownscaledDepthRT, rtDownDepth, FilterMode.Point);
                FullScreenBlit(ShaderParams.DownscaledDepthRT, Pass.CopyDepth);
                Shader.SetGlobalTexture(ShaderParams.DownscaledDepthRTNameId, ShaderParams.RTs[ShaderParams.DownscaledDepthRT]);

                // early debug views
                switch (debugView) {
                    case DebugView.Albedo:
                        FullScreenBlit(source, Pass.Albedo);
                        return;
                    case DebugView.Normals:
                        FullScreenBlit(source, Pass.Normals);
                        return;
                    case DebugView.Specular:
                        FullScreenBlit(source, Pass.Specular);
                        return;
                    case DebugView.Depth:
                        mat.SetFloat(ShaderParams.DebugDepthMultiplier, radiant.debugDepthMultiplier);
                        FullScreenBlit(source, Pass.Depth);
                        return;
                }

                // are we reusing rays?
                if (!prevs.TryGetValue(cam, out PerCameraData frameAcumData)) {
                    prevs[cam] = frameAcumData = new PerCameraData();
                }
                RenderTexture bounceRT = frameAcumData.rtBounce;

                RenderTexture raycastInput = source;
                if (usesBounce) {
                    if (bounceRT != null && (bounceRT.width != cameraTargetDesc.width || bounceRT.height != cameraTargetDesc.height)) {
                        bounceRT.Release();
                        bounceRT = null;
                    }
                    if (bounceRT == null) {
                        bounceRT = new RenderTexture(cameraTargetDesc);
                        bounceRT.Create();
                        frameAcumData.rtBounce = bounceRT;
                        frameAcumData.rtBounceCreationFrame = currentFrame;
                    } else {
                        if (currentFrame - frameAcumData.rtBounceCreationFrame > 2) {
                            raycastInput = bounceRT; // only uses bounce rt a few frames after it's created
                        }
                    }
                } else if (bounceRT != null) {
                    bounceRT.Release();
                    DestroyImmediate(bounceRT);
                }

                // virtual emitters
                if (usesEmitters) {
                    float now = Time.time;
                    if (emittersForceRefresh) {
                        emittersForceRefresh = false;
                        foreach (PerCameraData cameraData in prevs.Values) {
                            cameraData.emittersSortTime = float.MinValue;
                        }
                    }
                    if (now - frameAcumData.emittersSortTime > 5 || (frameAcumData.emittersLastCameraPosition - camPos).sqrMagnitude > 25) {
                        frameAcumData.emittersSortTime = now;
                        frameAcumData.emittersLastCameraPosition = camPos;
                        SortEmitters(cam);
                        frameAcumData.emittersSorted.Clear();
                        frameAcumData.emittersSorted.AddRange(emitters);
                    }
                    usesEmitters = SetupEmitters(cam, frameAcumData.emittersSorted);
                }
                if (usesEmitters) {
                    mat.EnableKeyword(ShaderParams.SKW_VIRTUAL_EMITTERS);
                } else {
                    mat.DisableKeyword(ShaderParams.SKW_VIRTUAL_EMITTERS);
                }

                // set the fallback mode
                mat.DisableKeyword(ShaderParams.SKW_REUSE_RAYS);
                mat.DisableKeyword(ShaderParams.SKW_FALLBACK_1_PROBE);
                mat.DisableKeyword(ShaderParams.SKW_FALLBACK_2_PROBES);

                bool usingProbes = false;
                if (radiant.fallbackReflectionProbes) {
                    if (SetupProbes(out int numProbes)) {
                        mat.EnableKeyword(numProbes == 1 ? ShaderParams.SKW_FALLBACK_1_PROBE : ShaderParams.SKW_FALLBACK_2_PROBES);
                        usingProbes = true;
                    }
                }
                if (!usingProbes) {
                    if (radiant.fallbackReuseRays && currentFrame - frameAcumData.rtAcumCreationFrame > 2 && radiant.rayReuse > 0 && frameAcumData.rtAcum != null) {
                        Shader.SetGlobalTexture(ShaderParams.PrevResolveNameId, frameAcumData.rtAcum);
                        mat.EnableKeyword(ShaderParams.SKW_REUSE_RAYS);
                    }
                }

                // raycast & resolve
                GetTemporaryRT(ShaderParams.ResolveRT, sourceDesc, FilterMode.Bilinear);
                FullScreenBlit(
                               raycastInput,
                               ShaderParams.ResolveRT, Pass.Raycast);

                GetTemporaryRT(ShaderParams.Downscaled1RT, downDesc, FilterMode.Bilinear);
                GetTemporaryRT(ShaderParams.Downscaled1RTA, downDesc, FilterMode.Bilinear);

                // Prepare NFO
                if (useNFO) {
                    RenderTextureDescriptor nfoDesc = downDesc;
                    nfoDesc.colorFormat = RenderTextureFormat.RHalf;
                    GetTemporaryRT(ShaderParams.NFO_RT, nfoDesc, FilterMode.Bilinear);
                    GetTemporaryRT(ShaderParams.NFOBlurRT, nfoDesc, FilterMode.Bilinear);
                    FullScreenBlit(ShaderParams.NFOBlurRT, Pass.NFO);
                    FullScreenBlit(ShaderParams.NFOBlurRT, ShaderParams.NFO_RT, Pass.NFOBlur);
                    Shader.SetGlobalTexture(ShaderParams.NFO_RT_NameId, ShaderParams.RTs[ShaderParams.NFO_RT]);
                }

                // downscale & blur
                downDesc.width /= 2;
                downDesc.height /= 2;
                GetTemporaryRT(ShaderParams.Downscaled2RT, downDesc, FilterMode.Bilinear);
                int downscaledQuarterRT = ShaderParams.Downscaled2RT;

                switch (smoothing) {
                    case 0:
                        if (downsampling <= 1f) {
                            FullScreenBlit(ShaderParams.ResolveRT, ShaderParams.Downscaled1RT, Pass.Copy);
                            FullScreenBlit(ShaderParams.Downscaled1RT, ShaderParams.Downscaled2RT, Pass.WideFilter);
                        } else {
                            FullScreenBlit(ShaderParams.ResolveRT, ShaderParams.Downscaled2RT, Pass.WideFilter);
                        }
                        if (usesRSM) {
                            FullScreenBlit(ShaderParams.Downscaled2RT, Pass.RSM);
                        }
                        break;
                    case 1:
                        GetTemporaryRT(ShaderParams.Downscaled2RTA, downDesc, FilterMode.Bilinear);
                        if (downsampling <= 1f) {
                            FullScreenBlit(ShaderParams.ResolveRT, ShaderParams.Downscaled1RT, Pass.Copy);
                            FullScreenBlit(ShaderParams.Downscaled1RT, ShaderParams.Downscaled2RTA, Pass.Copy);
                        } else {
                            FullScreenBlit(ShaderParams.ResolveRT, ShaderParams.Downscaled2RTA, Pass.CopyMultiTaps);
                        }
                        if (usesRSM) {
                            FullScreenBlit(ShaderParams.Downscaled2RTA, Pass.RSM);
                        }
                        FullScreenBlit(ShaderParams.Downscaled2RTA, ShaderParams.Downscaled2RT, Pass.WideFilter);
                        break;
                    case 2:
                        GetTemporaryRT(ShaderParams.Downscaled2RTA, downDesc, FilterMode.Bilinear);
                        if (downsampling <= 1f) {
                            FullScreenBlit(ShaderParams.ResolveRT, ShaderParams.Downscaled1RT, Pass.Copy);
                            FullScreenBlit(ShaderParams.Downscaled1RT, ShaderParams.Downscaled2RT, Pass.BlurHorizontal);
                            FullScreenBlit(ShaderParams.Downscaled2RT, ShaderParams.Downscaled2RTA, Pass.BlurVertical);
                            if (usesRSM) {
                                FullScreenBlit(ShaderParams.Downscaled2RTA, Pass.RSM);
                            }
                            FullScreenBlit(ShaderParams.Downscaled2RTA, ShaderParams.Downscaled2RT, Pass.WideFilter);
                        } else {
                            FullScreenBlit(ShaderParams.ResolveRT, ShaderParams.Downscaled2RT, Pass.BlurHorizontal);
                            FullScreenBlit(ShaderParams.Downscaled2RT, ShaderParams.Downscaled2RTA, Pass.BlurVertical);
                            if (usesRSM) {
                                FullScreenBlit(ShaderParams.Downscaled2RTA, Pass.RSM);
                            }
                            FullScreenBlit(ShaderParams.Downscaled2RTA, ShaderParams.Downscaled2RT, Pass.WideFilter);
                        }
                        break;
                    case 4:
                        GetTemporaryRT(ShaderParams.Downscaled2RTA, downDesc, FilterMode.Bilinear);
                        FullScreenBlit(ShaderParams.ResolveRT, ShaderParams.Downscaled1RTA, Pass.BlurHorizontal);
                        FullScreenBlit(ShaderParams.Downscaled1RTA, ShaderParams.Downscaled1RT, Pass.BlurVertical);
                        FullScreenBlit(ShaderParams.Downscaled1RT, ShaderParams.Downscaled2RT, Pass.BlurHorizontal);
                        FullScreenBlit(ShaderParams.Downscaled2RT, ShaderParams.Downscaled2RTA, Pass.BlurVertical);
                        if (usesRSM) {
                            FullScreenBlit(ShaderParams.Downscaled2RTA, Pass.RSM);
                        }
                        FullScreenBlit(ShaderParams.Downscaled2RTA, ShaderParams.Downscaled2RT, Pass.WideFilter);
                        Shader.SetGlobalVector(ShaderParams.ExtraData, new Vector4(radiant.rayJitter, 1.25f, normalMapInfluence, lumaInfluence));
                        FullScreenBlit(ShaderParams.Downscaled2RT, ShaderParams.Downscaled2RTA, Pass.WideFilter);
                        downscaledQuarterRT = ShaderParams.Downscaled2RTA;
                        break;
                    default:
                        GetTemporaryRT(ShaderParams.Downscaled2RTA, downDesc, FilterMode.Bilinear);
                        FullScreenBlit(ShaderParams.ResolveRT, ShaderParams.Downscaled1RTA, Pass.BlurHorizontal);
                        FullScreenBlit(ShaderParams.Downscaled1RTA, ShaderParams.Downscaled1RT, Pass.BlurVertical);
                        FullScreenBlit(ShaderParams.Downscaled1RT, ShaderParams.Downscaled2RT, Pass.BlurHorizontal);
                        FullScreenBlit(ShaderParams.Downscaled2RT, ShaderParams.Downscaled2RTA, Pass.BlurVertical);
                        if (usesRSM) {
                            FullScreenBlit(ShaderParams.Downscaled2RTA, Pass.RSM);
                        }
                        FullScreenBlit(ShaderParams.Downscaled2RTA, ShaderParams.Downscaled2RT, Pass.WideFilter);
                        break;
                }

                // Upscale
                FullScreenBlit(downscaledQuarterRT, ShaderParams.Downscaled1RTA, Pass.Upscale);

                computedGIRT = ShaderParams.Downscaled1RTA;
                RenderTexture prev = frameAcumData?.rtAcum;

                if (usesReprojection) {

                    if (prev != null && (prev.width != downHalfDescWidth || prev.height != downHalfDescHeight)) {
                        prev.Release();
                        prev = null;
                    }

                    RenderTextureDescriptor acumDesc = sourceDesc;
                    acumDesc.width = downHalfDescWidth;
                    acumDesc.height = downHalfDescHeight;
                    float responseSpeed = radiant.temporalResponseSpeed;
                    Pass acumPass = Pass.TemporalAccum;

                    if (prev == null) {
                        prev = new RenderTexture(acumDesc);
                        prev.Create();
                        frameAcumData.rtAcum = prev;
                        frameAcumData.lastCameraPosition = camPos;
                        frameAcumData.rtAcumCreationFrame = currentFrame;
                        acumPass = Pass.Copy;
                    } else {
                        float camTranslationDelta = Vector3.Distance(camPos, frameAcumData.lastCameraPosition);
                        frameAcumData.lastCameraPosition = camPos;
                        responseSpeed += camTranslationDelta * radiant.temporalCameraTranslationResponse;
                    }

                    mat.SetVector(ShaderParams.TemporalData, new Vector4(Mathf.Clamp01(responseSpeed * Time.unscaledDeltaTime), radiant.temporalDepthRejection, radiant.temporalChromaThreshold, 0));

                    Shader.SetGlobalTexture(ShaderParams.PrevResolveNameId, prev);
                    GetTemporaryRT(ShaderParams.TempAcum, acumDesc, FilterMode.Bilinear);
                    FullScreenBlit(computedGIRT, ShaderParams.TempAcum, acumPass);
                    FullScreenBlit(ShaderParams.TempAcum, prev, Pass.CopyExact);
                    computedGIRT = ShaderParams.TempAcum;
                } else if (prev != null) {
                    prev.Release();
                    DestroyImmediate(prev);
                }

                // prepare output blending
                Shader.SetGlobalTexture(ShaderParams.InputRTNameId, source);

                if (usesCompareMode) {
                    GetTemporaryRT(ShaderParams.CompareTex, cameraTargetDesc, FilterMode.Point); // needed by the compare pass
                    if (usesBounce) {
                        FullScreenBlit(computedGIRT, ShaderParams.CompareTex, Pass.Compose);
                        FullScreenBlit(ShaderParams.CompareTex, bounceRT, Pass.CopyExact);
                    }
                } else if (usesBounce) {
                    FullScreenBlit(computedGIRT, bounceRT, Pass.Compose);
                    FullScreenBlit(bounceRT, cameraTarget, Pass.CopyExact);
                    drawnToDestination = true;
                } else {
                    // go up into original resolve buffer (now smoothed)
                    FullScreenBlitToCamera(computedGIRT, Pass.Compose);
                }

                switch (debugView) {
                    case DebugView.DownscaledHalf:
                        FullScreenBlitToCamera(ShaderParams.Downscaled1RT, Pass.CopyExact);
                        return;
                    case DebugView.DownscaledQuarter:
                        FullScreenBlitToCamera(downscaledQuarterRT, Pass.CopyExact);
                        return;
                    case DebugView.UpscaleToHalf:
                        FullScreenBlitToCamera(ShaderParams.Downscaled1RTA, Pass.CopyExact);
                        return;
                    case DebugView.Raycast:
                        FullScreenBlitToCamera(ShaderParams.ResolveRT, Pass.CopyExact);
                        return;
                    case DebugView.ReflectiveShadowMap:
                        if (usesRSM) {
                            FullScreenBlit(source, Pass.RSM_Debug);
                            drawnToDestination = false;
                        }
                        return;
                    case DebugView.TemporalAccumulationBuffer:
                        if (usesReprojection) {
                            FullScreenBlitToCamera(ShaderParams.TempAcum, Pass.CopyExact);
                        }
                        return;
                    case DebugView.FinalGI:
                        FullScreenBlitToCamera(computedGIRT, Pass.FinalGIDebug);
                        return;
                }

            }

            void FullScreenBlitToCamera(int source, Pass pass) {
                Graphics.Blit(ShaderParams.RTs[source], cameraTarget, mat, (int)pass);
                drawnToDestination = true;
            }

            void GetTemporaryRT(int nameId, RenderTextureDescriptor rtDesc, FilterMode filterMode) {
                RenderTexture rt = RenderTexture.GetTemporary(rtDesc);
                rt.filterMode = filterMode;
                ShaderParams.RTs[nameId] = rt;
            }

            void FullScreenBlit(int destination, Pass pass) {
                Graphics.Blit((Texture)null, ShaderParams.RTs[destination], mat, (int)pass);
            }

            void FullScreenBlit(RenderTexture destination, Pass pass) {
                Graphics.Blit((Texture)null, destination, mat, (int)pass);
            }

            void FullScreenBlit(int source, RenderTexture destination, Pass pass) {
                Graphics.Blit(ShaderParams.RTs[source], destination, mat, (int)pass);
            }

            void FullScreenBlit(RenderTexture source, int destination, Pass pass) {
                Graphics.Blit(source, ShaderParams.RTs[destination], mat, (int)pass);
            }

            void FullScreenBlit(RenderTexture source, RenderTexture destination, Pass pass) {
                Graphics.Blit(source, destination, mat, (int)pass);
            }

            void FullScreenBlit(int source, int destination, Pass pass) {
                Graphics.Blit(ShaderParams.RTs[source], ShaderParams.RTs[destination], mat, (int)pass);
            }


            float CalculateProbeWeight(Vector3 wpos, Vector3 probeBoxMin, Vector3 probeBoxMax, float blendDistance) {
                Vector3 weightDir = Vector3.Min(wpos - probeBoxMin, probeBoxMax - wpos) / blendDistance;
                return Mathf.Clamp01(Mathf.Min(weightDir.x, Mathf.Min(weightDir.y, weightDir.z)));
            }


            bool SetupProbes(out int numProbes) {

                numProbes = PickNearProbes(out ReflectionProbe probe1, out ReflectionProbe probe2);
                if (numProbes == 0) return false;
                if (!probe1.bounds.Contains(camPos)) return false;
                if (numProbes >= 2 && !probe2.bounds.Contains(camPos)) numProbes = 1;

                float probe1Weight = 0, probe2Weight = 0;
                if (numProbes >= 1) {
                    Shader.SetGlobalTexture(ShaderParams.Probe1Cube, probe1.texture);
                    Shader.SetGlobalVector(ShaderParams.Probe1HDR, probe1.textureHDRDecodeValues);
                    Bounds probe1Bounds = probe1.bounds;
                    probe1Weight = CalculateProbeWeight(camPos, probe1Bounds.min, probe1Bounds.max, probe1.blendDistance);
                }
                if (numProbes >= 2) {
                    Shader.SetGlobalTexture(ShaderParams.Probe2Cube, probe2.texture);
                    Shader.SetGlobalVector(ShaderParams.Probe2HDR, probe1.textureHDRDecodeValues);
                    Bounds probe2Bounds = probe2.bounds;
                    probe2Weight = CalculateProbeWeight(camPos, probe2Bounds.min, probe2Bounds.max, probe2.blendDistance);
                }
                Shader.SetGlobalVector(ShaderParams.ProbeData, new Vector4(probe1Weight * radiant.probesIntensity, probe2Weight * radiant.probesIntensity, 0, 0));

                return true;
            }

            int PickNearProbes(out ReflectionProbe probe1, out ReflectionProbe probe2) {
                int probesCount = probes.Count;
                probe1 = probe2 = null;
                if (probesCount == 0) {
                    return 0;
                }
                if (probesCount == 1) {
                    probe1 = probes[0];
                    return 1;
                }

                float probe1Value = float.MaxValue;
                float probe2Value = float.MaxValue;
                for (int k = 0; k < probesCount; k++) {
                    ReflectionProbe probe = probes[k];
                    float probeValue = ComputeProbeValue(camPos, probe);
                    if (probeValue < probe2Value) {
                        probe2 = probe;
                        probe2Value = probeValue;
                        if (probe2Value < probe1Value) {
                            // swap probe1 & probe2
                            probeValue = probe1Value;
                            probe = probe1;
                            probe1 = probe2;
                            probe1Value = probe2Value;
                            probe2 = probe;
                            probe2Value = probeValue;
                        }
                    }
                }
                return 2;

            }

            float ComputeProbeValue(Vector3 camPos, ReflectionProbe probe) {
                Vector3 probePos = probe.transform.position;
                float d = (probePos - camPos).sqrMagnitude * (probe.importance + 1) * 1000;
                if (!probe.bounds.Contains(camPos)) d += 100000;
                return d;
            }

            void SetupVolumeBounds() {
                if (!radiant.limitToVolumeBounds) {
                    Shader.SetGlobalVector(ShaderParams.BoundsXZ, unlimitedBounds);
                    return;
                }
                if (volumes == null) {
                    volumes = FindObjectsOfType<RadiantVolume>(true);
                }
                int volumeCount = volumes.Length;
                for (int k = 0; k < volumeCount; k++) {
                    RadiantVolume volume = volumes[k];
                    Bounds bounds = new Bounds(volume.transform.position, volume.transform.localScale);
                    if (bounds.Contains(camPos)) {
                        Vector4 effectBounds = new Vector4(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                        Shader.SetGlobalVector(ShaderParams.BoundsXZ, effectBounds);
                        return;
                    }
                }
            }

            bool SetupEmitters(Camera cam, List<RadiantVirtualEmitter> emitters) {
                // copy emitters data
                if (emittersBoxMax == null || emittersBoxMax.Length != MAX_EMITTERS) {
                    emittersBoxMax = new Vector4[MAX_EMITTERS];
                    emittersBoxMin = new Vector4[MAX_EMITTERS];
                    emittersColors = new Vector4[MAX_EMITTERS];
                    emittersPositions = new Vector4[MAX_EMITTERS];
                }
                int emittersCount = 0;

                const int EMITTERS_BUDGET = 150; // max number of emitters to be processed per frame
                int emittersMax = Mathf.Min(EMITTERS_BUDGET, emitters.Count);

                GeometryUtility.CalculateFrustumPlanes(cam, cameraPlanes);

                for (int k = 0; k < emittersMax; k++) {
                    RadiantVirtualEmitter emitter = emitters[k];

                    // Cull emitters

                    // disabled emitter?
                    if (emitter == null || !emitter.isActiveAndEnabled) continue;

                    // emitter with no intensity or range?
                    if (emitter.intensity <= 0 || emitter.range <= 0) continue;

                    // emitter with black color (nothing to inject)?
                    Vector4 colorAndRange = emitter.GetGIColorAndRange();
                    if (colorAndRange.x == 0 && colorAndRange.y == 0 && colorAndRange.z == 0) continue;

                    // emitter bounds out of camera frustum
                    Bounds emitterBounds = emitter.GetBounds();
                    if (!GeometryUtility.TestPlanesAABB(cameraPlanes, emitterBounds)) continue;

                    // add emitter
                    Vector3 emitterPosition = emitter.transform.position;
                    emittersPositions[emittersCount] = emitterPosition;

                    emittersColors[emittersCount] = colorAndRange;

                    Vector3 boxMin = emitterBounds.min;
                    Vector3 boxMax = emitterBounds.max;

                    float lightRangeSqr = colorAndRange.w * colorAndRange.w;
                    // Commented out for future versions if needed
                    //float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
                    //float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
                    //float oneOverFadeRangeSqr = 1.0f / fadeRangeSqr;
                    //float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
                    float oneOverLightRangeSqr = 1.0f / Mathf.Max(0.0001f, lightRangeSqr);

                    float pointAttenX = oneOverLightRangeSqr;
                    //float pointAttenY = lightRangeSqrOverFadeRangeSqr;

                    emittersBoxMin[emittersCount] = new Vector4(boxMin.x, boxMin.y, boxMin.z, pointAttenX);
                    emittersBoxMax[emittersCount] = new Vector4(boxMax.x, boxMax.y, boxMax.z, 0); // pointAttenY

                    emittersCount++;
                    if (emittersCount >= MAX_EMITTERS) break;
                }

                if (emittersCount == 0) return false;

                Shader.SetGlobalVectorArray(ShaderParams.EmittersPositions, emittersPositions);
                Shader.SetGlobalVectorArray(ShaderParams.EmittersBoxMin, emittersBoxMin);
                Shader.SetGlobalVectorArray(ShaderParams.EmittersBoxMax, emittersBoxMax);
                Shader.SetGlobalVectorArray(ShaderParams.EmittersColors, emittersColors);
                Shader.SetGlobalInt(ShaderParams.EmittersCount, emittersCount);

                return true;
            }

            void SortEmitters(Camera cam) {
                emitters.Sort(EmittersDistanceComparer);
            }

            int EmittersDistanceComparer(RadiantVirtualEmitter p1, RadiantVirtualEmitter p2) {
                Vector3 p1Pos = p1.transform.position;
                Vector3 p2Pos = p2.transform.position;
                float d1 = (p1Pos - camPos).sqrMagnitude;
                float d2 = (p2Pos - camPos).sqrMagnitude;
                Bounds p1bounds = p1.GetBounds();
                Bounds p2bounds = p2.GetBounds();
                if (!p1bounds.Contains(camPos)) d1 += 100000;
                if (!p2bounds.Contains(camPos)) d2 += 100000;
                if (d1 < d2) return -1; else if (d1 > d2) return 1;
                return 0;
            }

            public void CleanUp() {
                if (mat != null) DestroyImmediate(mat);
                if (prevs != null) {
                    foreach (PerCameraData fad in prevs.Values) {
                        if (fad.rtAcum != null) {
                            fad.rtAcum.Release();
                            DestroyImmediate(fad.rtAcum);
                        }
                        if (fad.rtBounce != null) {
                            fad.rtBounce.Release();
                            DestroyImmediate(fad.rtBounce);
                        }
                    }
                    prevs.Clear();
                }
                ShaderParams.CleanUp();
            }
        }


        [Tooltip("Include forward rendering path materials that render in opaque queue when camera is rendering in deferred.")]
        public bool includeForward;

        [Tooltip("Used to filter which Radiant Volume should be used with this camera.")]
        public LayerMask volumeMask = -1;

        public enum NormalsQuality {
            High,
            ApproximatedFromDepth
        }
        public NormalsQuality normalsQuality = NormalsQuality.High;

        [Tooltip("Intensity of the indirect lighting.")]
        public float indirectIntensity;

        [Tooltip("Intensity of the near field obscurance effect. Darkens surfaces occluded by other nearby surfaces.")]
        public float nearFieldObscurance;

        [Tooltip("Spread or radius of the near field obscurance effect")]
        [Range(0.01f, 1f)]
        public float nearFieldObscuranceSpread = 0.2f;

        [Tooltip("Maximum camera distance of Near Field Obscurance effect")]
        public float nearFieldObscuranceMaxCameraDistance = 125f;

        [Tooltip("Distance threshold of the occluder.")]
        [Range(0f, 1f)]
        public float nearFieldObscuranceOccluderDistance = 0.825f;

        [Tooltip("Tint color of Near Field Obscurance effect")]
        [ColorUsage(showAlpha: false)]
        public Color nearFieldObscuranceTintColor = Color.black;

        [Tooltip("Distance attenuation applied to indirect lighting. Reduces innearFieldObscuranceMaxCameraDistancedistance.")]
        [Range(0, 1)]
        public float indirectDistanceAttenuation;

        [Tooltip("Maximum brightness of indirect source.")]
        public float indirectMaxSourceBrightness = 8;

        [Tooltip("Determines how much influence has the surface normal map when receiving indirect lighting.")]
        [Range(0, 1)]
        public float normalMapInfluence = 1f;

        [Tooltip("Add one ray bounce.")]
        public bool rayBounce;

        [Tooltip("Only in forward rendering mode: uses pixel luma to enhance results by adding variety to the effect based on the perceptual brigthness. Set this value to 0 to disable this feature.")]
        public float lumaInfluence;

        [Tooltip("Enable user-defined light emitters in the scene.")]
        public bool virtualEmitters;

        [Tooltip("Intensity of organic light. This option injects artifical/procedural light variations into g-buffers to product a more natural and interesting lit environment. This added lighting is also used as source for indirect lighting.")]
        [Range(0, 1)]
        public float organicLight;

        [Tooltip("Threshold of organic light noise calculation")]
        [Range(0, 1)]
        public float organicLightThreshold = 0.5f;

        [Tooltip("Organic light spread")]
        [Range(0.9f, 1f)]
        public float organicLightSpread = 0.98f;

        [Tooltip("Organic light normal influence preserves normal map effect on textures")]
        [Range(0, 1)]
        public float organicLightNormalsInfluence = 0.95f;

        [Tooltip("Organic light tint color")]
        [ColorUsage(showAlpha: false)]
        public Color organicLightTintColor = Color.white;

        [Tooltip("Animation speed")]
        public Vector3 organicLightAnimationSpeed;

        [Tooltip("Reduces organic light pattern repetition at the distance")]
        public bool organicLightDistanceScaling;

        [Tooltip("Number of rays per pixel")]
        [Range(1, 4)]
        public int rayCount = 1;

        [Tooltip("Max ray length. Increasing this value may also require increasing the 'Max Samples' value to avoid losing quality.")]
        public float rayMaxLength = 8;

        [Tooltip("Max samples taken during raymarch.")]
        public int rayMaxSamples = 32;

        [Tooltip("Jitter adds a random offset to the ray direction to reduce banding. Useful when using low sample count.")]
        public float rayJitter;

        [Tooltip("The assumed thickness for any geometry. Used to determine if ray crosses a surface.")]
        public float thickness = 1f;

        [Tooltip("Improves raymarch accuracy by using binary search.")]
        public bool rayBinarySearch = true;

        [Tooltip("In case a ray miss a target, reuse rays from previous frames.")]
        public bool fallbackReuseRays;

        [Tooltip("If a ray misses a target, reuse result from history buffer. This value is the intensity of the previous color in case the ray misses the target.")]
        [Range(0, 1)]
        public float rayReuse;

        [Tooltip("In case a ray miss a target, use nearby probes if they're available.")]
        public bool fallbackReflectionProbes;

        [Tooltip("Custom global probe intensity multiplier. Note that each probe has also an intensity property.")]
        public float probesIntensity = 1f;

        [Tooltip("In case a ray miss a target, use reflective shadow map data from the main directional light. You need to add the ReflectiveShadowMap script to the directional light to use this feature.")]
        public bool fallbackReflectiveShadowMap;

        [Range(0, 1)]
        public float reflectiveShadowMapIntensity = 0.8f;

        [Tooltip("Reduces resolution of all GI stages improving performance")]
        [Range(1, 4)]
        public float downsampling = 1;

        [Tooltip("Raytracing accuracy. Reducing this value will shrink the depth buffer used during raytracing, improving performance in exchange of accuracy.")]
        [Range(1, 8)]
        public int raytracerAccuracy = 8;

        [Tooltip("Extra blur passes")]
        [Range(0, 4)]
        public int smoothing = 3;

        [Tooltip("Uses motion vectors to blend into a history buffer to reduce flickering. Only applies in play mode.")]
        public bool temporalReprojection = true;

        [Tooltip("Reaction speed to screen changes. Higher values reduces ghosting but also the smoothing.")]
        public float temporalResponseSpeed = 12;

        [Tooltip("Reaction speed to camera position change. Higher values reduces ghosting when camera moves.")]
        public float temporalCameraTranslationResponse = 100;

        [Tooltip("Difference in depth with current frame to discard history buffer when reusing rays.")]
        public float temporalDepthRejection = 1f;

        [Tooltip("Allowed difference in color between history and current GI buffers.")]
        [Range(0, 2f)]
        public float temporalChromaThreshold = 0.2f;

        [Tooltip("Renders the effect also in edit mode (when not in play-mode).")]
        public bool showInEditMode = true;

        [Tooltip("Renders the effect also in Scene View.")]
        public bool showInSceneView = true;

        [Tooltip("Computes GI emitted by objects with a minimum luminosity.")]
        public float brightnessThreshold;

        [Tooltip("Maximum GI brightness.")]
        public float brightnessMax = 8f;

        [Range(0, 1)]
        [Tooltip("Amount of GI which adds to specular surfaces. Reduce this value to avoid overexposition of shiny materials.")]
        public float specularContribution = 0.5f;

        [Range(0, 2)]
        [Tooltip("Brightness of the original image. You may reduce this value to make GI more prominent.")]
        public float sourceBrightness = 1f;

        [Tooltip("Increases final GI contribution vs source color pixel. Increase this value to reduce the intensity of the source pixel color based on the received GI amount, making the applied GI more apparent.")]
        public float giWeight;

        [Tooltip("Attenuates GI brightness from nearby surfaces.")]
        public float nearCameraAttenuation;

        [Tooltip("Adjusted color saturation for the computed GI.")]
        [Range(0, 2)]
        public float saturation = 1f;

        [Tooltip("Applies GI only inside the post processing volume (use only if the volume is local)")]
        public bool limitToVolumeBounds;

        [Tooltip("Enables stencil check during GI composition. This option let you exclude GI over certain objects that also use stencil buffer.")]
        public bool stencilCheck;

        public int stencilValue;

        public CompareFunction stencilCompareFunction = CompareFunction.NotEqual;

        public DebugView debugView = DebugView.None;

        [Tooltip("Depth values multiplier for the depth debug view")]
        public float debugDepthMultiplier = 10;

        public bool compareMode;

        public bool compareSameSide;

        [Range(0, 0.5f)]
        public float comparePanning = 0.25f;

        [Range(-Mathf.PI, Mathf.PI)]
        public float compareLineAngle = 1.4f;

        [Range(0.0001f, 0.05f)]
        public float compareLineWidth = 0.002f;

        public bool IsActive() => indirectIntensity > 0 || compareMode;

        public static bool needRTRefresh;

        RadiantPass radiantPass;
        Camera cam;
        RadiantGlobalIlluminationComparer comparer;
        CommandBuffer cmdOrganicLight;
        Material matOrganicLight;
        Texture2D noiseTex;
        Vector3 offset;

        public static bool isRenderingInDeferred;


        private void OnEnable() {

            if (radiantPass == null) {
                radiantPass = new RadiantPass();
            }
            cam = GetComponent<Camera>();
        }

        void OnDisable() {
            SetupOrganicLights();
            if (radiantPass != null) {
                radiantPass.CleanUp();
            }
        }

        private void OnDestroy() {
            if (matOrganicLight != null) {
                DestroyImmediate(matOrganicLight);
            }
        }

        void Reset() {
            needRTRefresh = true;
        }

        [ImageEffectOpaque]
        void OnRenderImage(RenderTexture source, RenderTexture destination) {

            cam.depthTextureMode |= DepthTextureMode.Depth;
            if (cam.actualRenderingPath == UnityEngine.RenderingPath.Forward) {
                cam.depthTextureMode |= DepthTextureMode.DepthNormals;
            }

#if UNITY_EDITOR
            if (cam.cameraType == CameraType.Game) {
                isRenderingInDeferred = cam.actualRenderingPath == UnityEngine.RenderingPath.DeferredShading;
            }
            if (UnityEditor.ShaderUtil.anythingCompiling) {
                needRTRefresh = true;
            }
            if (needRTRefresh) {
                needRTRefresh = false;
                radiantPass.CleanUp();
            }
#endif

            SetupOrganicLights();
            ComputeRadiantVolumes(cam);
            radiantPass.Setup(this);
            radiantPass.Execute(source, destination, cam);
        }


        void ComputeRadiantVolumes(Camera cam) {
            Vector3 pos = cam.transform.position;
            indirectIntensity = 0;
            compareMode = false;
            foreach (RadiantVolume rv in volumes) {
                if (rv == null || rv.profile == null || !rv.isActiveAndEnabled) continue;
                if ((volumeMask & (1 << rv.gameObject.layer)) == 0) continue;
                float w = 1f;
                if (rv.mode == RadiantVolume.VolumeMode.Local) {
                    Bounds bounds = new Bounds(rv.transform.position, rv.transform.lossyScale);
                    w = CalculateVolumeWeight(pos, bounds.min, bounds.max, rv.blendDistance);
                }
                if (w > 0) {
                    rv.profile.Apply(this);
                    indirectIntensity *= w;
                }
            }

            if (compareMode) {
                if (comparer == null) {
                    comparer = GetComponent<RadiantGlobalIlluminationComparer>();
                    if (comparer == null) {
                        comparer = gameObject.AddComponent<RadiantGlobalIlluminationComparer>();
                    }
                }
                if (!comparer.enabled) {
                    comparer.enabled = true;
                }
            } else {
                if (comparer != null) {
                    comparer.enabled = false;
                }
            }

        }

        void SetupOrganicLights() {
            if (organicLight <= 0 || cam.actualRenderingPath != UnityEngine.RenderingPath.DeferredShading) {
                if (cmdOrganicLight != null) {
                    cam.RemoveCommandBuffer(CameraEvent.BeforeLighting, cmdOrganicLight);
                }
                return;
            }

            if (cmdOrganicLight == null) {
                cmdOrganicLight = new CommandBuffer();
                cmdOrganicLight.name = "Radiant Organic Light";
            } else {
                cam.RemoveCommandBuffer(CameraEvent.BeforeLighting, cmdOrganicLight);
            }

            if (noiseTex == null) {
                noiseTex = Resources.Load<Texture2D>("RadiantGI/NoiseTex");
            }

            if (matOrganicLight == null) {
                Shader shader = Shader.Find("Hidden/Kronnect/RadiantGIOrganicLight");
                if (shader == null) {
                    Debug.LogWarning("Shader Radiant GI Organic Light not found");
                    return;
                }
                matOrganicLight = new Material(shader);
            }

            offset += organicLightAnimationSpeed * Time.deltaTime;
            offset.x %= 10000f;
            offset.y %= 10000f;
            offset.z %= 10000f;
            matOrganicLight.SetVector(ShaderParams.OrganicLightOffset, offset);

            matOrganicLight.SetTexture(ShaderParams.NoiseTex, noiseTex);
            matOrganicLight.SetVector(ShaderParams.OrganicLightData, new Vector4(1.001f - organicLightSpread, organicLight, organicLightThreshold, organicLightNormalsInfluence));
            matOrganicLight.SetColor(ShaderParams.OrganicLightTint, organicLightTintColor);

            if (organicLightDistanceScaling) {
                matOrganicLight.EnableKeyword(ShaderParams.SKW_DISTANCE_BLENDING);
            } else {
                matOrganicLight.DisableKeyword(ShaderParams.SKW_DISTANCE_BLENDING);
            }

            cmdOrganicLight.Clear();
            RenderTargetIdentifier rti = new RenderTargetIdentifier(BuiltinRenderTextureType.GBuffer0, 0, CubemapFace.Unknown, -1);
            cmdOrganicLight.Blit(null, rti, matOrganicLight);
            cam.AddCommandBuffer(CameraEvent.BeforeLighting, cmdOrganicLight);
        }

        float CalculateVolumeWeight(Vector3 wpos, Vector3 volumeMin, Vector3 volumeMax, float blendDistance) {
            Vector3 weightDir = Vector3.Min(wpos - volumeMin, volumeMax - wpos) / blendDistance;
            return Mathf.Clamp01(Mathf.Min(weightDir.x, Mathf.Min(weightDir.y, weightDir.z)));
        }

        public static void RegisterReflectionProbe(ReflectionProbe probe) {
            if (probe == null) return;
            if (!probes.Contains(probe)) {
                probes.Add(probe);
            }
        }

        public static void UnregisterReflectionProbe(ReflectionProbe probe) {
            if (probe == null) return;
            if (probes.Contains(probe)) {
                probes.Remove(probe);
            }
        }

        public static void RegisterVirtualEmitter(RadiantVirtualEmitter emitter) {
            if (emitter == null) return;
            if (!emitters.Contains(emitter)) {
                emitters.Add(emitter);
                emittersForceRefresh = true;
            }
        }

        public static void UnregisterVirtualEmitter(RadiantVirtualEmitter emitter) {
            if (emitter == null) return;
            if (emitters.Contains(emitter)) {
                emitters.Remove(emitter);
                emittersForceRefresh = true;
            }
        }

        public static void RegisterVolume(RadiantVolume volume) {
            if (volume == null) return;
            if (!volumes.Contains(volume)) {
                volumes.Add(volume);
            }
        }

        public static void UnregisterVolume(RadiantVolume volume) {
            if (volume == null) return;
            if (volumes.Contains(volume)) {
                volumes.Remove(volume);
            }
        }

    }

}

