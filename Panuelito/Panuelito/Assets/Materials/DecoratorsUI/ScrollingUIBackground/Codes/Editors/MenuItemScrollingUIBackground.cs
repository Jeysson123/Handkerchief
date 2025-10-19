using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace ShigeDev.ScrollingUIBackground {

    public class MenuItemScrollingUIBackground : MonoBehaviour
    {
        [MenuItem("GameObject/ShigeDev/ScrollingUIBackground", false, 10)]
            private static void CreateScrollingUIBackground()
            {
                var gameObject = Resources.Load("ScrollingUIBackground") as GameObject;
                var instantiatedObject = Instantiate(gameObject, Vector3.zero, Quaternion.identity);
                instantiatedObject.name = "ScrollingUIBackground";
            }
    }
}

#endif