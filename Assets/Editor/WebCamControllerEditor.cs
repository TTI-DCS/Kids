using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WebCamController))]
public class WebCamControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WebCamController controller = (WebCamController)target;
        
        // デフォルトインスペクターを描画
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        // カメラ選択セクション
        EditorGUILayout.LabelField("Camera Selection", EditorStyles.boldLabel);
        
        // 利用可能なカメラを表示
        if (controller.availableCameraNames != null && controller.availableCameraNames.Length > 0)
        {
            EditorGUILayout.LabelField("Available Cameras:", EditorStyles.helpBox);
            for (int i = 0; i < controller.availableCameraNames.Length; i++)
            {
                string style = (i == controller.selectedCameraIndex) ? " ← SELECTED" : "";
                EditorGUILayout.LabelField($"  {controller.availableCameraNames[i]}{style}");
            }
            
            EditorGUILayout.Space();
            
            // カメラ選択ドロップダウン
            int newSelectedIndex = EditorGUILayout.Popup("Select Camera:", controller.selectedCameraIndex, controller.availableCameraNames);
            if (newSelectedIndex != controller.selectedCameraIndex)
            {
                controller.selectedCameraIndex = newSelectedIndex;
                EditorUtility.SetDirty(controller);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No cameras detected. Press 'Refresh Camera List' to scan for cameras.", MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        
        // ボタンセクション
        EditorGUILayout.LabelField("Camera Controls", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Refresh Camera List"))
        {
            if (Application.isPlaying)
            {
                controller.SendMessage("RefreshCameraList");
            }
            else
            {
                // エディターモードでの簡易カメラリスト更新
                WebCamDevice[] devices = WebCamTexture.devices;
                controller.availableCameraNames = new string[devices.Length];
                for (int i = 0; i < devices.Length; i++)
                {
                    string frontBack = devices[i].isFrontFacing ? " (Front)" : " (Back)";
                    controller.availableCameraNames[i] = $"{i}: {devices[i].name}{frontBack}";
                }
                EditorUtility.SetDirty(controller);
            }
        }
        
        if (Application.isPlaying && GUILayout.Button("Restart Camera"))
        {
            controller.SendMessage("RestartCamera");
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 情報表示
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Current Camera Index: {controller.selectedCameraIndex}");
            EditorGUILayout.LabelField($"Auto Refresh: {(controller.autoRefreshCameras ? "Enabled" : "Disabled")}");
        }
        
        EditorGUILayout.Space();
        
        // ヘルプボックス
        EditorGUILayout.HelpBox(
            "Camera Selection:\n" +
            "• Select camera from dropdown\n" +
            "• Changes apply automatically in Play mode\n" +
            "• Use 'Refresh Camera List' if cameras are not detected\n" +
            "• Front/Back camera info is shown in parentheses",
            MessageType.Info
        );
    }
}