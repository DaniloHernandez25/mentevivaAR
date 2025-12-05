using UnityEngine;
using TMPro;

public class TogglePassword : MonoBehaviour
{
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Sprite iconMostrar;
    [SerializeField] private Sprite iconOcultar;
    [SerializeField] private UnityEngine.UI.Image icono;
    private bool isPasswordHidden = true;

    public void TogglePasswordVisibility()
    {
        isPasswordHidden = !isPasswordHidden;

        if (isPasswordHidden)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            icono.sprite = iconMostrar;
        }
        else
        {
            passwordInput.contentType = TMP_InputField.ContentType.Standard;
            icono.sprite = iconOcultar;
        }

        passwordInput.ForceLabelUpdate();
    }
}
