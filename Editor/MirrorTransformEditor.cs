#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

[CustomEditor(typeof(MirrorTransform))]
public class MirrorTransformEditor : Editor, IEditorOnly
{
    private static Vector3 copiedPosition;
    private static Vector3 copiedRotation;
    private static Vector3 copiedScale;

    private enum MirrorType
    {
        PositionX,
        PositionY,
        PositionZ,
        PositionAll,
        Rotation,
        Scale
    }

    private enum CopyPasteType
    {
        Position,
        Rotation,
        Scale,
        All
    }

    public override void OnInspectorGUI()
    {
        MirrorTransform mirrorTransform = (MirrorTransform)target;

        GUILayout.Space(10);
        serializedObject.Update();
        SerializedProperty property = serializedObject.GetIterator();

        while (property.NextVisible(true))
        {
            if (property.name == "m_Script")
                continue;

            EditorGUILayout.PropertyField(property, true);
        }
        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(10);
        DrawWorldTransformSection(mirrorTransform.transform, "This Transform", "This");
        GUILayout.Space(10);

        if (mirrorTransform.Pair != null)
            DrawWorldTransformSection(mirrorTransform.Pair.transform, "Pair Transform", "Pair");
        else
            EditorGUILayout.HelpBox("No pair assigned!", MessageType.Warning);

        GUILayout.Space(10);
    }

    private void DrawWorldTransformSection(Transform transform, string label, string contextKey)
    {
        EditorGUILayout.BeginHorizontal();

        GUIContent icon = EditorGUIUtility.IconContent("Transform Icon");
        GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));

        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        if (GUILayout.Button("•••", GUILayout.Width(30)))
            ShowContextMenu(transform);

        EditorGUILayout.EndHorizontal();

        Vector3 worldPosition = transform.position;
        EditorGUI.BeginChangeCheck();
        Vector3 newPosition = EditorGUILayout.Vector3Field("Position", worldPosition);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(transform, $"{label} Position Change");
            transform.position = newPosition;
        }

        Vector3 worldRotation = SanitizeRotation(transform.eulerAngles);
        EditorGUI.BeginChangeCheck();
        Vector3 newRotation = EditorGUILayout.Vector3Field("Rotation", worldRotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(transform, $"{label} Rotation Change");
            transform.eulerAngles = newRotation;
        }

        Vector3 worldScale = transform.lossyScale;
        EditorGUI.BeginChangeCheck();
        Vector3 newScale = EditorGUILayout.Vector3Field("Scale", worldScale);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(transform, $"{label} Scale Change");

            if (transform.parent != null)
            {
                Vector3 parentScale = transform.parent.lossyScale;
                transform.localScale = new Vector3(
                    newScale.x / parentScale.x,
                    newScale.y / parentScale.y,
                    newScale.z / parentScale.z
                );
            }
            else
            {
                transform.localScale = newScale;
            }
        }
    }

    private void ShowContextMenu(Transform transform)
    {
        GenericMenu menu = new GenericMenu();
        MirrorTransform mirrorTransform = (MirrorTransform)target;

        menu.AddItem(new GUIContent("Copy/Position"), false, () => CopyTransformData(transform, CopyPasteType.Position));
        menu.AddItem(new GUIContent("Copy/Rotation"), false, () => CopyTransformData(transform, CopyPasteType.Rotation));
        menu.AddItem(new GUIContent("Copy/Scale"), false, () => CopyTransformData(transform, CopyPasteType.Scale));
        menu.AddItem(new GUIContent("Copy/All"), false, () => CopyTransformData(transform, CopyPasteType.All));
        menu.AddSeparator("");

        menu.AddItem(new GUIContent("Paste/Position"), false, () => PasteTransformData(transform, CopyPasteType.Position));
        menu.AddItem(new GUIContent("Paste/Rotation"), false, () => PasteTransformData(transform, CopyPasteType.Rotation));
        menu.AddItem(new GUIContent("Paste/Scale"), false, () => PasteTransformData(transform, CopyPasteType.Scale));
        menu.AddItem(new GUIContent("Paste/All"), false, () => PasteTransformData(transform, CopyPasteType.All));
        menu.AddSeparator("");

        menu.AddItem(new GUIContent("Mirror/Position/X"), false, () => MirrorSelf(transform, MirrorType.PositionX));
        menu.AddItem(new GUIContent("Mirror/Position/Y"), false, () => MirrorSelf(transform, MirrorType.PositionY));
        menu.AddItem(new GUIContent("Mirror/Position/Z"), false, () => MirrorSelf(transform, MirrorType.PositionZ));
        menu.AddItem(new GUIContent("Mirror/Position/All"), false, () => MirrorSelf(transform, MirrorType.PositionAll));
        menu.AddItem(new GUIContent("Mirror/Rotation"), false, () => MirrorSelf(transform, MirrorType.Rotation));
        menu.AddItem(new GUIContent("Mirror/Scale"), false, () => MirrorSelf(transform, MirrorType.Scale));
        menu.AddItem(new GUIContent("Mirror/All"), false, () =>
        {
            MirrorSelf(transform, MirrorType.PositionAll);
            MirrorSelf(transform, MirrorType.Rotation);
            MirrorSelf(transform, MirrorType.Scale);
        });
        menu.AddSeparator("");

        menu.AddItem(new GUIContent("Reset/Position"), false, () => ResetTransformData(transform, CopyPasteType.Position));
        menu.AddItem(new GUIContent("Reset/Rotation"), false, () => ResetTransformData(transform, CopyPasteType.Rotation));
        menu.AddItem(new GUIContent("Reset/Scale"), false, () => ResetTransformData(transform, CopyPasteType.Scale));
        menu.AddItem(new GUIContent("Reset/All"), false, () => ResetTransformData(transform, CopyPasteType.All));

        menu.ShowAsContext();
    }

    private void CopyTransformData(Transform transform, CopyPasteType copyPasteType)
    {
        switch (copyPasteType)
        {
            case CopyPasteType.Position:
                copiedPosition = transform.position;
                break;
            case CopyPasteType.Rotation:
                copiedRotation = transform.eulerAngles;
                break;
            case CopyPasteType.Scale:
                copiedScale = transform.lossyScale;
                break;
            case CopyPasteType.All:
                copiedPosition = transform.position;
                copiedRotation = transform.eulerAngles;
                copiedScale = transform.lossyScale;
                break;
        }
    }

    private void PasteTransformData(Transform transform, CopyPasteType copyPasteType)
    {
        Undo.RecordObject(transform, $"Paste {copyPasteType}");
        switch (copyPasteType)
        {
            case CopyPasteType.Position:
                transform.position = copiedPosition;
                break;
            case CopyPasteType.Rotation:
                transform.rotation = Quaternion.Euler(copiedRotation);
                break;
            case CopyPasteType.Scale:
                if (transform.parent != null)
                {
                    Vector3 parentScale = transform.parent.lossyScale;
                    transform.localScale = DivideScales(copiedScale, parentScale);
                }
                else
                {
                    transform.localScale = copiedScale;
                }
                break;
            case CopyPasteType.All:
                PasteTransformData(transform, CopyPasteType.Position);
                PasteTransformData(transform, CopyPasteType.Rotation);
                PasteTransformData(transform, CopyPasteType.Scale);
                break;
        }
    }

    private void ResetTransformData(Transform transform, CopyPasteType copyPasteType)
    {
        Undo.RecordObject(transform, $"Reset {copyPasteType}");
        switch (copyPasteType)
        {
            case CopyPasteType.Position:
                transform.position = Vector3.zero;
                break;
            case CopyPasteType.Rotation:
                transform.rotation = Quaternion.identity;
                break;
            case CopyPasteType.Scale:
                transform.localScale = Vector3.one;
                break;
            case CopyPasteType.All:
                ResetTransformData(transform, CopyPasteType.Position);
                ResetTransformData(transform, CopyPasteType.Rotation);
                ResetTransformData(transform, CopyPasteType.Scale);
                break;
        }
    }

    private void MirrorSelf(Transform transform, MirrorType mirrorType)
    {
        Undo.RecordObject(transform, $"Mirror {mirrorType}");

        switch (mirrorType)
        {
            case MirrorType.PositionX:
                Vector3 mirroredPositionX = transform.position;
                mirroredPositionX.x = -mirroredPositionX.x;
                transform.position = mirroredPositionX;
                break;
            case MirrorType.PositionY:
                Vector3 mirroredPositionY = transform.position;
                mirroredPositionY.y = -mirroredPositionY.y;
                transform.position = mirroredPositionY;
                break;
            case MirrorType.PositionZ:
                Vector3 mirroredPositionZ = transform.position;
                mirroredPositionZ.z = -mirroredPositionZ.z;
                transform.position = mirroredPositionZ;
                break;
            case MirrorType.PositionAll:
                Vector3 mirroredPosition = transform.position;
                mirroredPosition = -mirroredPosition;
                transform.position = mirroredPosition;
                break;
            case MirrorType.Rotation:
                Vector3 mirroredRotation = transform.eulerAngles;
                mirroredRotation.y = -mirroredRotation.y;
                mirroredRotation.z = -mirroredRotation.z;
                transform.eulerAngles = mirroredRotation;
                break;
            case MirrorType.Scale:
                Vector3 mirroredScale = transform.localScale;
                mirroredScale = -mirroredScale;
                transform.localScale = mirroredScale;
                break;
        }
    }

    private Vector3 SanitizeRotation(Vector3 rotation)
    {
        const float threshold = 1e-4f;

        return new Vector3(
            Mathf.Abs(rotation.x) < threshold ? 0f : rotation.x,
            Mathf.Abs(rotation.y) < threshold ? 0f : rotation.y,
            Mathf.Abs(rotation.z) < threshold ? 0f : rotation.z
        );
    }

    private Vector3 DivideScales(Vector3 scale, Vector3 parentScale)
    {
        return new Vector3(
            parentScale.x != 0 ? scale.x / parentScale.x : 0,
            parentScale.y != 0 ? scale.y / parentScale.y : 0,
            parentScale.z != 0 ? scale.z / parentScale.z : 0
        );
    }
}
#endif