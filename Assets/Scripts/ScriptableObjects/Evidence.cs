using UnityEngine;

[CreateAssetMenu(menuName = "AceAttorney/Evidence")]
public class Evidence : ScriptableObject
{
    public string evidenceName;
    [TextArea] public string description;
    public Sprite image;
}