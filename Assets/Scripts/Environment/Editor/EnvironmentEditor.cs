using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Environment))]
public class EnvironmentEditor : Editor {
	public override void OnInspectorGUI() {
		Environment environment = target as Environment;
		if (GUILayout.Button("Regenerate")) {
			environment.ClearPopulations();
      environment.SpawnInitialPopulations();
		}

		DrawDefaultInspector();
	}
}
