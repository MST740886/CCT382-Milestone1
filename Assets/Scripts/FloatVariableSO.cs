using UnityEngine;

[CreateAssetMenu(fileName = "NewFloatVariable", menuName = "Game Data/Float Variable")]
public class FloatVariableSO : ScriptableObject
{
    [Tooltip("A shared numeric value, such as current or maximum health.")]
    public float value;
}
