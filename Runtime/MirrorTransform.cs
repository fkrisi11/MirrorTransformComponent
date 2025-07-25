#if UNITY_EDITOR
using UnityEngine;
using VRC.SDKBase;

public class MirrorTransform : MonoBehaviour, IEditorOnly
{
    public GameObject Pair;
}
#endif