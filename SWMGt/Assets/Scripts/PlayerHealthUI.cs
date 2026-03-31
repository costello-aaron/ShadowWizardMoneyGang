using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    public Health playerHealth;
    public Slider slider;

    void Update()
    {
        slider.value = playerHealth.GetHealth();
    }
}