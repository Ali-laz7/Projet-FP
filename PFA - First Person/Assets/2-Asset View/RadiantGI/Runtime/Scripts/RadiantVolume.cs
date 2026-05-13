using UnityEngine;

namespace RadiantGI {

    [ExecuteInEditMode]
    [HelpURL("https://kronnect.com/guides-category/radiant-gi/")]
    public class RadiantVolume : MonoBehaviour {


        public enum VolumeMode {
            Global,
            Local
        }

        public VolumeMode mode = VolumeMode.Global;
        public RadiantProfile profile;
        public float blendDistance = 1f;

        static Color s_VolumeGizmoColorDefault = new Color(0.2f, 0.8f, 0.1f, 0.75f);

        private void OnEnable() {
            RadiantGlobalIllumination.RegisterVolume(this);
        }

        private void OnDisable() {
            RadiantGlobalIllumination.UnregisterVolume(this);
        }

        public void OnDrawGizmos() {
            Gizmos.color = s_VolumeGizmoColorDefault;
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }

        private void OnValidate() {
            blendDistance = Mathf.Max(0.0001f, blendDistance);
        }
    }


}
