using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class TestEditor : EditorWindow
{
    //ObjectField objectField;
    ObjectField image_base_select;
    [MenuItem("Window/UI Toolkit/TestEditor")]
    public static void ShowExample()
    {
        TestEditor wnd = GetWindow<TestEditor>();
        wnd.titleContent = new GUIContent("TestEditor");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement label = new Label("Hello World! From C#");
        root.Add(label);

        //objectField = new ObjectField();
        //objectField.objectType = typeof(Texture2D);
        //objectField.label = "Select an object:";
        //root.Add(objectField);

        image_base_select = new ObjectField("Base Image");
        image_base_select.objectType = typeof(Texture2D);
        root.Add(image_base_select);
        //image_base_select.RegisterValueChangedCallback<UnityEngine.Object>(ImageSelected);
    }
}
