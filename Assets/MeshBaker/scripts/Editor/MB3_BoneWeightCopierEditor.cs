//----------------------------------------------
//            MeshBaker
// Copyright © 2015-2016 Ian Deane
//----------------------------------------------
using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using DigitalOpus.MB.Core;
using UnityEditor;


[CustomEditor(typeof(MB3_BoneWeightCopier))]
[CanEditMultipleObjects]
public class MB3_BoneWeightCopierEditor : Editor {
	SerializedProperty inputGameObjectProp;
    SerializedProperty radiusProp;
    SerializedProperty seamMeshProp;
    SerializedProperty targetMeshesProp;
    SerializedProperty outputFolderProp;

    GUIContent radiusGC = new GUIContent("Radius", "Vertices in each Target Mesh that are within Radius of a vertex in the Seam Mesh will be assigned the same bone weight as the Seam Mesh vertex.");
    GUIContent seamMeshGC = new GUIContent("Seam Mesh", "The seam mesh should contain vertices for the seams with correct bone weights. Seams are the vertices that are shared by two skinned meshes that overlap. For example there should be a seam between an arm skinned mesh and a hand skinned mesh.");

    [MenuItem("GameObject/Create Other/Mesh Baker/Bone Weight Copier")]
		public static GameObject CreateNewMeshBaker(){
			GameObject nmb = new GameObject("BoneWeightCopier");
			nmb.transform.position = Vector3.zero;
            nmb.AddComponent<MB3_BoneWeightCopier>();
			return nmb.gameObject;  
		}
		

    void OnEnable() {
        radiusProp = serializedObject.FindProperty("radius");
        seamMeshProp = serializedObject.FindProperty("seamMesh");
        targetMeshesProp = serializedObject.FindProperty("targetMeshes");
        outputFolderProp = serializedObject.FindProperty("outputFolder");
		inputGameObjectProp = serializedObject.FindProperty("inputGameObject");
    }

    string ConvertAbsolutePathToUnityPath(string absolutePath) {
        if (absolutePath.StartsWith(Application.dataPath)) {
            return "Assets" + absolutePath.Substring(Application.dataPath.Length);
        }
        return null;
    }

    public override void OnInspectorGUI(){
        serializedObject.Update();
        MB3_BoneWeightCopier bwc = (MB3_BoneWeightCopier) target;


        EditorGUILayout.HelpBox("== BETA (please report problems) ==\n\n" +
                                "This tool helps create skinned mesh parts that can be mix and matched to customize characters. " +
                                "It adjusts the boneWeights at the joins to have the same weight so there are no tears." +
                                "\n\n" +
                                "1) Model the skinned meshes and attach them all to the same rig. The rig may have multiple arms, hands, hair etc...\n" +
                                "2) At every seam (eg. between an arm mesh and hand mesh) there must be a set of vertices duplicated in both meshes.\n" +
                                "3) Create one additional mesh that contains another copy of all the seam vertices. This will be called the Seam Mesh\n" +
                                "4) Attach it to the rig and adjust the bone weights. This will be the master set of bone weights that will be copied to all the other skinned meshes.\n" +
                                "5) Import the model into Unity and create an instance of it in the scene\n" +
                                "6) Assign the Seam Mesh to the Seam Mesh field\n" +
                                "7) Assign all the skinned meshes to the Target Meshes field\n" +
                                "8) Adjust the radius\n" +
                                "9) Click 'Copy Bone Weights From Seam Mesh'\n" +
                                "10) Choose an output folder and save the meshes\n" +
                                "11) Optionally create a new prefab and assign the rig with skinned meshes to it\n", MessageType.Info);

        EditorGUILayout.PropertyField(radiusProp, radiusGC);
        EditorGUILayout.PropertyField(seamMeshProp, seamMeshGC);
		EditorGUILayout.PropertyField(inputGameObjectProp);
		if (GUILayout.Button("Get Skinned Meshes From Input Game Object")) {
			GetSkinnedMeshesFromGameObject(bwc);
		}
        EditorGUILayout.PropertyField(targetMeshesProp,true);

        if (GUILayout.Button("Copy Bone Weights From Seam Mesh")) {
            CopyBoneWeightsFromSeamMeshToOtherMeshes(bwc);
        }
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Save Meshes To Project Folder", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Mesh Output Folder", outputFolderProp.stringValue);
        if (GUILayout.Button("Browse For Output Folder")) {
            string path = EditorUtility.OpenFolderPanel("Browse For Output Folder", "", "");
            outputFolderProp.stringValue = path;
        }
        if (GUILayout.Button("Save Meshes To Output Folder")) {
            SaveMeshesToOutputFolderAndUpdateTargetSMRs(bwc);
        }
        serializedObject.ApplyModifiedProperties();
	}

	public void GetSkinnedMeshesFromGameObject(MB3_BoneWeightCopier bwc){
		if (bwc.inputGameObject == null){
			Debug.LogError("Please set the input game object.");
			return;
		}
		SkinnedMeshRenderer[] rs = bwc.inputGameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		bwc.targetMeshes = rs;
	}

    public void CopyBoneWeightsFromSeamMeshToOtherMeshes(MB3_BoneWeightCopier bwc) {
        Mesh[] targs = new Mesh[bwc.targetMeshes.Length];
        for (int i = 0; i < bwc.targetMeshes.Length; i++) {
            if (bwc.targetMeshes[i] == null) {
                Debug.LogError(string.Format("Target Mesh {0} is null", i));
                return;
            }
            if (bwc.targetMeshes[i].sharedMesh == null) {
					Debug.LogError(string.Format("Target Mesh {0} does not have a mesh", i));
                return;
            }
            PrefabType pt = PrefabUtility.GetPrefabType(bwc.targetMeshes[i]);
            if (pt == PrefabType.ModelPrefab) {
						Debug.LogError(string.Format("Target Mesh {0} is an imported model prefab. Can't modify these meshes because changes will be overwritten the next time the model is saved or reimported. Try instantiating the prefab and using skinned meshes from the scene instance.", i));
                return;
            }
            targs[i] = bwc.targetMeshes[i].sharedMesh;
        }

        if (bwc.seamMesh == null) {
            Debug.LogError("The seamMesh cannot be null.");
            return;
        }

		UnityEngine.Object pr = (UnityEngine.Object) PrefabUtility.GetPrefabObject(bwc.seamMesh.gameObject);
		string assetPath = null;
		if (pr != null) {
			assetPath = AssetDatabase.GetAssetPath(pr);
		}
		if (assetPath != null){
			ModelImporter mi = (ModelImporter) AssetImporter.GetAtPath(assetPath);
			if (mi != null){
				if (mi.optimizeMesh){
					Debug.LogError(string.Format("The seam mesh has 'optimized' checked in the asset importer. This will result in no vertices. Uncheck 'optimized'."));
					return;
				}
			}
		}

        MB3_CopyBoneWeights.CopyBoneWeightsFromSeamMeshToOtherMeshes(bwc.radius, bwc.seamMesh.sharedMesh, targs);

    }

    public void SaveMeshesToOutputFolderAndUpdateTargetSMRs(MB3_BoneWeightCopier bwc) {
        //validate meshes
        for (int i = 0; i < bwc.targetMeshes.Length; i++) {
            if (bwc.targetMeshes[i] == null) {
				Debug.LogError(string.Format("Target Mesh {0} is null", i));
                return;
            }
            if (bwc.targetMeshes[i].sharedMesh == null) {
					Debug.LogError(string.Format("Target Mesh {0} does not have a mesh", i));
                return;
            }
            PrefabType pt = PrefabUtility.GetPrefabType(bwc.targetMeshes[i]);
            if (pt == PrefabType.ModelPrefab) {
						Debug.LogError(string.Format("Target Mesh {0} is an imported model prefab. Can't modify these meshes because changes will be overwritten the next time the model is saved or reimported. Try instantiating the prefab and using skinned meshes from the scene instance.", i));
                return;
            }
        }
        //validate output folder
        if (outputFolderProp.stringValue == null) {
            Debug.LogError("Output folder must be set");
            return;
        }
        if (outputFolderProp.stringValue.StartsWith(Application.dataPath)) {
            string relativePath = "Assets" + outputFolderProp.stringValue.Substring(Application.dataPath.Length);
            string gid = AssetDatabase.AssetPathToGUID(relativePath);
            if (gid == null) {
                Debug.LogError("Output folder must be a folder in the Unity project Asset folder");
                return;
            }
        } else {
            Debug.LogError("Output folder must be a folder in the Unity project Asset folder");
            return;
        }
        for (int i = 0; i < bwc.targetMeshes.Length; i++) {
            Mesh m = (Mesh) Instantiate(bwc.targetMeshes[i].sharedMesh);
            string pth = ConvertAbsolutePathToUnityPath(outputFolderProp.stringValue + "/" + bwc.targetMeshes[i].name + ".Asset");
            if (pth == null) {
                Debug.LogError("The output folder must be a folder in the project Assets folder.");
                return;
            }
            AssetDatabase.CreateAsset(m, pth);
            bwc.targetMeshes[i].sharedMesh = m;
            Debug.Log(string.Format("Created mesh at {0}. Updated Skinned Mesh {1} to use created mesh.", pth, bwc.targetMeshes[i].name));
        }
        AssetDatabase.SaveAssets();
    }

}
