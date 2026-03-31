using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
    public Health enemyHealth;
    public Slider slider;
    public Transform target;
    public Vector3 offset = new Vector3(0, 0, 0);

    void Update()
    {
        if (enemyHealth != null)
        {
            slider.value = enemyHealth.GetHealth();
        }

        // Follow enemy
        if (target != null)
        {
            transform.position = target.position + offset;
        }

        // Face camera
        transform.forward = Camera.main.transform.forward;
    }
}