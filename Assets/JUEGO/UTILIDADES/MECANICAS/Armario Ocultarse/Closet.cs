using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Closet : MonoBehaviour
{
    public VHSFilter vhsFilter; // arrástralo en el inspector
    public float hideDuration = 2f; // tiempo que dura la entrada
    [Header("Configuración")]
    public Transform hidePoint;
    public Transform cameraExitPoint;
    public float doorOpenAngle = 90f;
    public float doorSpeed = 3f;
    public float cooldownTime = 1f;

    [Header("Puertas del armario (opcional)")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip hideSound;

    [Header("UI")]
    public Text interactionText;

    private bool isPlayerNearby = false;
    private bool isPlayerHidden = false;
    private bool onCooldown = false;
    private GameObject player;
    private Quaternion leftDoorClosedRot, rightDoorClosedRot;

    void Start()
    {
        if (leftDoor) leftDoorClosedRot = leftDoor.localRotation;
        if (rightDoor) rightDoorClosedRot = rightDoor.localRotation;
        if (interactionText) interactionText.enabled = false;
    }

    void Update()
    {
        if (isPlayerNearby && !onCooldown && Input.GetKeyDown(KeyCode.E))
        {
            if (!isPlayerHidden)
                HidePlayer();
            else
                ExitCloset();
        }

        // Animación simple de puertas (rotación)
        if (leftDoor && rightDoor)
        {
            Quaternion targetLeft = isPlayerHidden
                ? Quaternion.Euler(0, -doorOpenAngle, 0)
                : leftDoorClosedRot;
            Quaternion targetRight = isPlayerHidden
                ? Quaternion.Euler(0, doorOpenAngle, 0)
                : rightDoorClosedRot;

            leftDoor.localRotation = Quaternion.Lerp(leftDoor.localRotation, targetLeft, Time.deltaTime * doorSpeed);
            rightDoor.localRotation = Quaternion.Lerp(rightDoor.localRotation, targetRight, Time.deltaTime * doorSpeed);
        }
    }

    void HidePlayer()
    {
        if (!player) return;

        isPlayerHidden = true;

        // Bloquear movimiento del jugador
        player.GetComponent<FPSController>().SetHidden(true);



        // Rotar al jugador 90° hacia la puerta al entrar
        player.transform.rotation = Quaternion.Euler(
            player.transform.eulerAngles.x, // mantiene la rotación X (mirada vertical)
            player.transform.eulerAngles.y + 180f, // gira 90° en Y
            player.transform.eulerAngles.z  // mantiene la rotación Z
        );

        
        StartCoroutine(MaximizeVHS(vhsFilter, hideDuration));
        

        // Desactivar colisión del armario para evitar bloqueos
        GetComponent<Collider>().enabled = false;

        // Mover al jugador entero al HidePoint
        StartCoroutine(MovePlayerTo(player.transform, hidePoint.position, 0.5f));

        // Sonidos
        if (audioSource && hideSound) audioSource.PlayOneShot(hideSound);
        if (audioSource && closeSound) audioSource.PlayOneShot(closeSound);

        // UI
        if (interactionText)
        {
            interactionText.enabled = true;
            interactionText.text = "Presiona E para salir";
        }

        StartCoroutine(Cooldown());
    }

    void ExitCloset()
    {
        if (!player) return;

        isPlayerHidden = false;

        // Reactivar movimiento del jugador
        player.GetComponent<FPSController>().SetHidden(false);

        // Reactivar colisión del armario
        GetComponent<Collider>().enabled = true;

        // Mover al jugador entero al CameraExitPoint
        StartCoroutine(MovePlayerTo(player.transform, cameraExitPoint.position, 0.5f));

        // Sonido de abrir puerta
        if (audioSource && openSound) audioSource.PlayOneShot(openSound);

        // UI
        if (interactionText)
        {
            interactionText.enabled = true;
            interactionText.text = "Presiona E para esconderte";
        }

        StartCoroutine(Cooldown());
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            player = other.gameObject;
            if (interactionText) interactionText.enabled = true;
            interactionText.text = "Presiona E para esconderte";
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !isPlayerHidden)
        {
            isPlayerNearby = false;
            if (interactionText) interactionText.enabled = false;
        }
    }

    private System.Collections.IEnumerator Cooldown()
    {
        onCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        onCooldown = false;
    }
    IEnumerator MovePlayerTo(Transform obj, Vector3 targetPos, float duration)
    {
        Vector3 start = obj.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            obj.position = Vector3.Lerp(start, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        obj.position = targetPos;
    }
    IEnumerator MaximizeVHS(VHSFilter filter, float duration)
    {
        // Guardar valores originales
        float originalDistortion = filter.distortion;
        float originalNoise = filter.noiseIntensity;

        // Aumentar al máximo
        filter.distortion = 0.2f;        // máximo permitido
        filter.noiseIntensity = 1;      // ruido máximo

        // Esperar duración de entrada
        yield return new WaitForSeconds(duration);

        // Restaurar valores originales
        filter.distortion = originalDistortion;
        filter.noiseIntensity = originalNoise;
    }




}

