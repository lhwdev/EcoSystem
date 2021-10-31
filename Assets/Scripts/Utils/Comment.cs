#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;

public class CommentAttribute : PropertyAttribute {
	public readonly string tooltip;
	public readonly string comment;
	
	public CommentAttribute(string comment, string tooltip = null) {
		this.tooltip = tooltip;
		this.comment = comment;
	}
}

[CustomPropertyDrawer(typeof(CommentAttribute))]
public class CommentDrawer : PropertyDrawer {
	const int textHeight = 20;
	
	CommentAttribute commentAttribute { get { return (CommentAttribute) attribute; } }
	
	public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) {
		return textHeight;
	}
	
	public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label) {
		EditorGUI.LabelField(position, new GUIContent(commentAttribute.comment, commentAttribute.tooltip));
	}
}

#endif
