using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;

[CustomPropertyDrawer(typeof(Genes))]
[CanEditMultipleObjects]
public class GenesDrawer : PropertyDrawer {
	private ReorderableList list;
	private bool isFirst = true;

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		if (isFirst) {
			var serialized = property.serializedObject;
			var traits = property.FindPropertyRelative("lightTraits");
			list = new ReorderableList(serialized, traits, draggable: false, displayHeader: true, displayAddButton: false, displayRemoveButton: false) {

				drawHeaderCallback = rect => {
					EditorGUI.LabelField(rect, new GUIContent(property.displayName));
				},
				drawElementCallback = (rect, index, isActive, isFocused) => {
					rect.y += 2;
					var trait = traits.GetArrayElementAtIndex(index);

					var name = trait.FindPropertyRelative("name");
					var type = trait.FindPropertyRelative("type");
					var value1 = trait.FindPropertyRelative("value1");
					var value1min = trait.FindPropertyRelative("value1min");
					var value1max = trait.FindPropertyRelative("value1max");

					var w = rect.width / 10;
					var h = EditorGUIUtility.singleLineHeight;

					EditorGUI.LabelField(
						new Rect(rect.x, rect.y, w * 3, h),
						new GUIContent(name.stringValue),
						EditorStyles.label
					);

					EditorGUI.LabelField(
						new Rect(rect.x + w * 3, rect.y, w * 2, h),
						new GUIContent(type.enumDisplayNames[type.enumValueIndex]),
						EditorStyles.label
					);
					var r = new Rect(rect.x + w * 5, rect.y, w * 3, h);
					EditorGUI.BeginProperty(r, GUIContent.none, value1);
					EditorGUI.BeginChangeCheck();
					var newValue = GUI.HorizontalSlider(
						r,
						value: value1.floatValue,
						leftValue: value1min.floatValue,
						rightValue: value1max.floatValue
					);
					var changed = EditorGUI.EndChangeCheck();
					if(changed) {
						value1.floatValue = newValue;
					}

					EditorGUI.LabelField(
						new Rect(rect.x + w * 8, rect.y, w * 2, h),
						label: newValue.ToString(),
						style: EditorStyles.textField
					);

					EditorGUI.EndProperty();

					// EditorGUI.PropertyField(
					// 	position: new Rect(rect.x + 15f, rect.y, rect.width - 15f, rect.height),
					// 	property: trait,
					// 	includeChildren: true,
					// 	label: new GUIContent(name.stringValue)
					// );

				},
				onChangedCallback = (list) => {
					property.FindPropertyRelative("lightTraitsChanged").boolValue = true;
				},
				onAddDropdownCallback = (Rect buttonRect, ReorderableList l) => {
					// var menu = new GenericMenu();
					// foreach (string enumName in enumNames) {
					// 	if (activeEnumNames.Contains(enumName) == false) {
					// 		menu.AddItem(new GUIContent(enumNameToDisplayName[enumName]),
					// 			false, data => {
					// 				if (enumNameToValue[(string)data] == 0) {
					// 					activeEnumNames.Clear();
					// 				}
					// 				activeEnumNames.Add((string)data);
					// 				SaveActiveValues();
					// 				ParseActiveEnumNames();
					// 			},
					// 			enumName);
					// 	}
					// }
					// menu.ShowAsContext();
				},
				onRemoveCallback = l => {
					// ReorderableList.defaultBehaviours.DoRemoveButton(l);
					// SaveActiveValues();
					// ParseActiveEnumNames();
				}
			};
			isFirst = false;
		}
		return list.GetHeight();
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty(position, label, property);

		list.DoList(position);

		EditorGUI.EndProperty();
	}
}
