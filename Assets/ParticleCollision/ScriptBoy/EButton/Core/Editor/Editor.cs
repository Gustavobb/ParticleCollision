using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

[CustomEditor(typeof(MonoBehaviour), true)]
public class EButtonInspector : Editor
{
    public static bool reverse;

    [EButton, EButton.BeginHorizontal]
    public void OnEnable()
    {
        var method = this.GetType().GetMethod("OnEnable");
        var attributes = method.GetCustomAttributes(false);
        reverse = attributes[0].GetType() == typeof(EButton.BeginHorizontal);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawEButtons();
    }

    public void DrawEButtons()
    {
        DrawEButtons(target);
    }

    public static void DrawEButtons(object target)
    {
        var type = target.GetType();
        BindingFlags flags = 
            BindingFlags.InvokeMethod |
            BindingFlags.Public | 
            BindingFlags.NonPublic | 
            BindingFlags.Static | 
            BindingFlags.Instance;

        var methods = type.GetMethods(flags);

        //0 = Horizontal , 1 = Vertical
        List<int> controlGroup = new List<int>();

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            var attributes = method.GetCustomAttributes(false);
            if (attributes.Length > 0)
            {
                for (int ii = 0; ii < attributes.Length; ii++)
                {
                    int Index = reverse? attributes.Length - ii - 1 : ii;

                    var attribute = attributes[Index];
                    var attributeType = attribute.GetType(); ;
                    if (attributeType == typeof(EButton))
                    {
                        EButton eButton = (EButton)attribute;
                        if (GUILayout.Button((eButton.text == null) ? method.Name : eButton.text))
                        {
                            method.Invoke(target, null);
                        }
                    }
                    else if (attributeType == typeof(EButton.BeginHorizontal))
                    {
                        EButton.BeginHorizontal beginHorizontal = (EButton.BeginHorizontal)attribute;
                        if (beginHorizontal.text == null)
                        {
                            GUILayout.BeginHorizontal();
                        }
                        else
                        {
                            GUILayout.BeginHorizontal(beginHorizontal.text, GUI.skin.window, GUILayout.MaxHeight(1));
                        }
                        controlGroup.Add(0);
                    }
                    else if (attributeType == typeof(EButton.EndHorizontal))
                    {
                        int lastIndex = controlGroup.Count - 1;
                        if (controlGroup[lastIndex] == 0)
                        {
                            GUILayout.EndHorizontal();
                            controlGroup.RemoveAt(lastIndex);
                        }
                    }
                    else if (attributeType == typeof(EButton.BeginVertical))
                    {
                        EButton.BeginVertical beginVertical = (EButton.BeginVertical)attribute;
                        if (beginVertical.text == null)
                        {
                            GUILayout.BeginVertical();
                        }
                        else
                        {
                            GUILayout.BeginVertical(beginVertical.text, GUI.skin.window, GUILayout.MaxHeight(1));
                        }
                        controlGroup.Add(1);
                    }
                    else if (attributeType == typeof(EButton.EndVertical))
                    {
                        int lastIndex = controlGroup.Count - 1;
                        if (controlGroup[lastIndex] == 1)
                        {
                            GUILayout.EndVertical();
                            controlGroup.RemoveAt(lastIndex);
                        }
                    }
                }
            }
        }

        for (int i = controlGroup.Count - 1 ; i >= 0; i--)
        {
            if (controlGroup[i] == 0)
            {
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.EndVertical();
            }
        }
    }
}

public static class MonoBehaviourEditorExtensions
{
    public static void DrawEButtons(this Editor e)
    {
        EButtonInspector.DrawEButtons(e.target);
    }
}