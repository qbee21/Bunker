using UnityEngine;
using UnityEngine.UI;

public class LocalText : MonoBehaviour
{
    public static bool english;
    public Text view;
    [TextArea] public string ru;
    [TextArea] public string en;

    void OnEnable() { Refresh(); }
    public void Refresh() { view.text = Pick(ru, en); }
    public static string Pick(string ru, string en) { return english ? en : ru; }
}
