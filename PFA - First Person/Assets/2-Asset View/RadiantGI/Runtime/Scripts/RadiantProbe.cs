using UnityEngine;

namespace RadiantGI {

    [ExecuteInEditMode]
    public class RadiantProbe : MonoBehaviour {

        ReflectionProbe probe;

        void OnEnable() {
            probe = GetComponent<ReflectionProbe>();
            RadiantGlobalIllumination.RegisterReflectionProbe(probe);
        }

        void OnDisable() {
            RadiantGlobalIllumination.UnregisterReflectionProbe(probe);
        }
    }

}
