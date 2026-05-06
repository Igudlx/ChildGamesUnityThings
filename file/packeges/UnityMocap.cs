using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

public class UnityMocap : EditorWindow
{
    GameObject sourceRig;
    GameObject targetRig;

    float startDelay = 0f;
    float endTrim = 0f;

    string saveFolder = "Assets";
    string animationName = "NewMocap";

    bool isRecording = false;
    bool isWaitingForStart = false;

    float recordStartTime;
    float actualStartTime;

    class FrameData
    {
        public float time;
        public Dictionary<string, TransformData> transforms = new Dictionary<string, TransformData>();
        public Dictionary<string, bool> activeStates = new Dictionary<string, bool>();
    }

    class TransformData
    {
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 scale;
    }

    List<FrameData> frames = new List<FrameData>();

    [MenuItem("Tools/UnityMocap")]
    public static void ShowWindow()
    {
        GetWindow<UnityMocap>("UnityMocap");
    }

    void OnGUI()
    {
        GUILayout.Label("Unity Mocap Recorder", EditorStyles.boldLabel);

        sourceRig = (GameObject)EditorGUILayout.ObjectField("Player Rig (Source)", sourceRig, typeof(GameObject), true);
        targetRig = (GameObject)EditorGUILayout.ObjectField("Rig (Target)", targetRig, typeof(GameObject), true);

        startDelay = EditorGUILayout.FloatField("Start Delay (sec)", startDelay);
        endTrim = EditorGUILayout.FloatField("End Trim (sec)", endTrim);

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Save Folder", saveFolder);
        if (GUILayout.Button("Select Folder"))
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                saveFolder = "Assets" + path.Substring(Application.dataPath.Length);
            }
            else
            {
                Debug.LogError("Folder must be inside Assets!");
            }
        }
        EditorGUILayout.EndHorizontal();

        animationName = EditorGUILayout.TextField("Animation Name", animationName);

        GUILayout.Space(10);

        GUI.enabled = !isRecording && !isWaitingForStart;
        if (GUILayout.Button("Start Recording"))
        {
            if (sourceRig == null || targetRig == null)
            {
                Debug.LogError("Assign both rigs!");
                return;
            }

            frames.Clear();
            isWaitingForStart = true;
            recordStartTime = (float)EditorApplication.timeSinceStartup + startDelay;

            EditorApplication.update += RecordUpdate;
        }
        GUI.enabled = true;

        GUI.enabled = isRecording;
        if (GUILayout.Button("Stop Recording"))
        {
            StopRecording();
        }
        GUI.enabled = true;

        if (isWaitingForStart)
            GUILayout.Label("Waiting to start...");

        if (isRecording)
            GUILayout.Label("Recording...");
    }

    void RecordUpdate()
    {
        float currentTime = (float)EditorApplication.timeSinceStartup;

        if (isWaitingForStart)
        {
            if (currentTime >= recordStartTime)
            {
                isWaitingForStart = false;
                isRecording = true;
                actualStartTime = currentTime;
            }
            return;
        }

        if (!isRecording)
            return;

        float elapsed = currentTime - actualStartTime;

        FrameData frame = new FrameData();
        frame.time = elapsed;

        Transform[] all = sourceRig.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in all)
        {
            string path = GetPath(sourceRig.transform, t);

            TransformData data = new TransformData();
            data.pos = t.localPosition;
            data.rot = t.localRotation;
            data.scale = t.localScale;

            frame.transforms[path] = data;
            frame.activeStates[path] = t.gameObject.activeSelf;
        }

        frames.Add(frame);
    }

    void StopRecording()
    {
        isRecording = false;
        isWaitingForStart = false;
        EditorApplication.update -= RecordUpdate;

        ApplyEndTrim();
        CreateAnimation();

        Debug.Log("Recording Complete");
    }

    void ApplyEndTrim()
    {
        if (endTrim <= 0f || frames.Count == 0)
            return;

        float maxTime = frames[frames.Count - 1].time;
        float cutoffTime = maxTime - endTrim;

        frames.RemoveAll(f => f.time > cutoffTime);
    }

    void CreateAnimation()
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60f;

        var curves = new Dictionary<string, Dictionary<string, AnimationCurve>>();

        foreach (FrameData frame in frames)
        {
            foreach (var kvp in frame.transforms)
            {
                string path = kvp.Key;
                TransformData t = kvp.Value;

                if (!curves.ContainsKey(path))
                    curves[path] = new Dictionary<string, AnimationCurve>();

                void AddKey(string prop, float value)
                {
                    if (!curves[path].ContainsKey(prop))
                        curves[path][prop] = new AnimationCurve();

                    curves[path][prop].AddKey(frame.time, value);
                }

                AddKey("m_LocalPosition.x", t.pos.x);
                AddKey("m_LocalPosition.y", t.pos.y);
                AddKey("m_LocalPosition.z", t.pos.z);

                AddKey("m_LocalRotation.x", t.rot.x);
                AddKey("m_LocalRotation.y", t.rot.y);
                AddKey("m_LocalRotation.z", t.rot.z);
                AddKey("m_LocalRotation.w", t.rot.w);

                AddKey("m_LocalScale.x", t.scale.x);
                AddKey("m_LocalScale.y", t.scale.y);
                AddKey("m_LocalScale.z", t.scale.z);

                AddKey("m_IsActive", frame.activeStates[path] ? 1 : 0);
            }
        }

        foreach (var path in curves)
        {
            foreach (var prop in path.Value)
            {
                if (prop.Key == "m_IsActive")
                    clip.SetCurve(path.Key, typeof(GameObject), prop.Key, prop.Value);
                else
                    clip.SetCurve(path.Key, typeof(Transform), prop.Key, prop.Value);
            }
        }

        string animPath = Path.Combine(saveFolder, animationName + ".anim");
        AssetDatabase.CreateAsset(clip, animPath);

        string controllerPath = Path.Combine(saveFolder, animationName + " AC.controller");
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        var stateMachine = controller.layers[0].stateMachine;
        var state = stateMachine.AddState(animationName);
        state.motion = clip;
        stateMachine.defaultState = state;

        Animator animator = targetRig.GetComponent<Animator>();
        if (animator == null)
            animator = targetRig.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Saved animation + controller to: " + saveFolder);
    }

    string GetPath(Transform root, Transform target)
    {
        if (target == root) return "";

        string path = target.name;
        Transform current = target.parent;

        while (current != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}

// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.
// © 2026 Child Games. All Rights Reserved. No part of this may be copied, distributed or used without explicit permission.