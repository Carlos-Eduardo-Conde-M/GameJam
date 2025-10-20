using UnityEngine;

public class ChestFlashlight : MonoBehaviour
{
    public Light flashlight;   // referencia a la luz
    public KeyCode toggleKey = KeyCode.F;
    private bool isOn = true;  // encendida al inicio (puedes cambiarlo)

    void Start()
    {
        if (flashlight == null)
        {
            flashlight = GetComponentInChildren<Light>();
        }

        // Aseguramos que tenga el estado inicial correcto
        flashlight.enabled = isOn;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isOn = !isOn;
            flashlight.enabled = isOn;
        }
    }
}
