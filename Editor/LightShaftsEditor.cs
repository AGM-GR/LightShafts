using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LightShafts)), CanEditMultipleObjects]
class LightShaftsEditor : Editor {
    SerializedProperty cameras;
    SerializedProperty shadowmapMode;
    SerializedProperty size;
    SerializedProperty near;
    SerializedProperty far;
    SerializedProperty cullingMask;
    SerializedProperty colorFilterMask;
    SerializedProperty brightness;
    SerializedProperty brightnessColored;
    SerializedProperty extinction;
    SerializedProperty minDistFromCamera;
    SerializedProperty shadowmapRes;
    SerializedProperty colored;
    SerializedProperty colorBalance;
    SerializedProperty epipolarLines;
    SerializedProperty epipolarSamples;
    SerializedProperty depthThreshold;
    SerializedProperty interpolationStep;
    SerializedProperty showSamples;
    SerializedProperty showInterpolatedSamples;
    SerializedProperty backgroundFade;
    SerializedProperty attenuationCurveOn;
    SerializedProperty attenuationCurve;
    GUIContent[] sizesStr = new GUIContent[] { new GUIContent("64"), new GUIContent("128"), new GUIContent("256"), new GUIContent("512"), new GUIContent("1024"), new GUIContent("2048") };
    int[] sizes = new int[] { 64, 128, 256, 512, 1024, 2048 };
    GUIContent[] interpolationStepValuesStr = new GUIContent[] { new GUIContent("32"), new GUIContent("16"), new GUIContent("8") };
    int[] interpolationStepValues = new int[] { 32, 16, 8 };

    void OnEnable() {
        cameras = serializedObject.FindProperty("m_Cameras");
        shadowmapMode = serializedObject.FindProperty("m_ShadowmapMode");
        size = serializedObject.FindProperty("m_Size");
        near = serializedObject.FindProperty("m_SpotNear");
        far = serializedObject.FindProperty("m_SpotFar");
        cullingMask = serializedObject.FindProperty("m_CullingMask");
        colorFilterMask = serializedObject.FindProperty("m_ColorFilterMask");
        brightness = serializedObject.FindProperty("m_Brightness");
        brightnessColored = serializedObject.FindProperty("m_BrightnessColored");
        extinction = serializedObject.FindProperty("m_Extinction");
        minDistFromCamera = serializedObject.FindProperty("m_MinDistFromCamera");
        shadowmapRes = serializedObject.FindProperty("m_ShadowmapRes");
        colored = serializedObject.FindProperty("m_Colored");
        colorBalance = serializedObject.FindProperty("m_ColorBalance");
        epipolarLines = serializedObject.FindProperty("m_EpipolarLines");
        epipolarSamples = serializedObject.FindProperty("m_EpipolarSamples");
        depthThreshold = serializedObject.FindProperty("m_DepthThreshold");
        interpolationStep = serializedObject.FindProperty("m_InterpolationStep");
        showSamples = serializedObject.FindProperty("m_ShowSamples");
        showInterpolatedSamples = serializedObject.FindProperty("m_ShowInterpolatedSamples");
        backgroundFade = serializedObject.FindProperty("m_ShowSamplesBackgroundFade");

        attenuationCurveOn = serializedObject.FindProperty("m_AttenuationCurveOn");
        attenuationCurve = serializedObject.FindProperty("m_AttenuationCurve");
        if (attenuationCurve.animationCurveValue.length == 0) {
            attenuationCurve.animationCurveValue = new AnimationCurve(new Keyframe(0, 1.0f), new Keyframe(1, 0.0f));
            serializedObject.ApplyModifiedProperties();
            ((LightShafts)serializedObject.targetObject).gameObject.SendMessage("UpdateLUTs");
        }
    }

    void Label(string text) {
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    }

    void CheckParamBounds() {
        depthThreshold.floatValue = Mathf.Max(0, depthThreshold.floatValue);
        brightness.floatValue = Mathf.Max(0, brightness.floatValue);
        brightnessColored.floatValue = Mathf.Max(0, brightnessColored.floatValue);
        minDistFromCamera.floatValue = Mathf.Max(0, minDistFromCamera.floatValue);

        float minNear = 0.0001f;
        far.floatValue = Mathf.Clamp(far.floatValue, minNear, 1.0f);
        near.floatValue = Mathf.Clamp(near.floatValue, minNear, far.floatValue);
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        LightShafts effect = target as LightShafts;

        if (!effect.CheckMinRequirements()) {
            EditorGUILayout.HelpBox("Render texture support (including RFloat and RGFloat) and shader model 3.0 required.", MessageType.Error, true);
            return;
        }

        effect.UpdateLightType();
        if (!effect.directional && !effect.spot) {
            EditorGUILayout.HelpBox("Directional or spot lights only.", MessageType.Warning, true);
            return;
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(cameras, true);
        var updateCameraDepthMode = EditorGUI.EndChangeCheck();

        Label("Volumetric shadow");

        if (effect.directional) {
            EditorGUILayout.PropertyField(size, new GUIContent("Volume size"));
        } else {
            EditorGUILayout.PropertyField(near, new GUIContent("Volume start"));
            EditorGUILayout.PropertyField(far, new GUIContent("Volume end"));
        }

        EditorGUILayout.PropertyField(cullingMask, new GUIContent("Culling mask"));

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(shadowmapMode, new GUIContent("Shadowmap mode"));
        if ((LightShaftsShadowmapMode)shadowmapMode.enumValueIndex == LightShaftsShadowmapMode.Static) {
            if (GUILayout.Button("Update shadowmap") || EditorGUI.EndChangeCheck()) {
                effect.SetShadowmapDirty();
                EditorUtility.SetDirty(target);
            }
        }

        Label("Brightness");

        EditorGUILayout.PropertyField(colored, new GUIContent("Colored"));
        if (colored.boolValue) {
            EditorGUILayout.PropertyField(brightnessColored, new GUIContent("Brightness (colored)"));
            EditorGUILayout.PropertyField(colorFilterMask, new GUIContent("Color filters mask"));
            EditorGUILayout.Slider(colorBalance, 0.1f, 1, new GUIContent("Color balance"));
        } else {
            EditorGUILayout.PropertyField(brightness, new GUIContent("Brightness"));
        }
        // Creates a sphere around the camera, in which the light is not accumulated in. Sometimes useful.
        // EditorGUILayout.PropertyField (minDistFromCamera, new GUIContent("Min dist from cam"));

        Label("Attenuation");

        EditorGUILayout.PropertyField(attenuationCurveOn, new GUIContent("Attenuation curve"));
        var updateLUTs = false;
        if (attenuationCurveOn.boolValue) {
            EditorGUI.BeginChangeCheck();
            attenuationCurve.animationCurveValue = EditorGUILayout.CurveField(new GUIContent("Attenuation curve"), attenuationCurve.animationCurveValue, Color.white, new Rect(0.0f, 0.0f, 1.0f, 1.0f));
            updateLUTs = EditorGUI.EndChangeCheck();
        } else {
            if (effect.directional)
                EditorGUILayout.PropertyField(extinction, new GUIContent("Attenuation"));
            else
                EditorGUILayout.LabelField("Default");
        }

        Label("Quality");

        EditorGUILayout.IntPopup(shadowmapRes, sizesStr, sizes, new GUIContent("Shadowmap resolution"));
        EditorGUILayout.IntPopup(epipolarSamples, sizesStr, sizes, new GUIContent("Samples along rays"));
        EditorGUILayout.IntPopup(epipolarLines, sizesStr, sizes, new GUIContent("Samples across rays"));
        EditorGUILayout.PropertyField(depthThreshold, new GUIContent("Depth threshold"));
        EditorGUILayout.IntPopup(interpolationStep, interpolationStepValuesStr, interpolationStepValues, new GUIContent("Force sample every"));

        Label("Show samples");

        EditorGUILayout.PropertyField(showSamples, new GUIContent("Raymarched (slow) samples"));
        if (showSamples.boolValue) {
            if (SystemInfo.graphicsShaderLevel >= 50) {
                // Maybe not that important to show
                //EditorGUILayout.PropertyField (showInterpolatedSamples, new GUIContent("Interpolated samples"));
                EditorGUILayout.Slider(backgroundFade, 0, 1, new GUIContent("Background fade"));
            } else
                EditorGUILayout.HelpBox("Sample visualisation works only in DX11", MessageType.Warning, true);
        }

        CheckParamBounds();
        serializedObject.ApplyModifiedProperties();
        if (updateLUTs)
            effect.UpdateLUTs();
        if (updateCameraDepthMode)
            effect.UpdateCameraDepthMode();
    }
}
