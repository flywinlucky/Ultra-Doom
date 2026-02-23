using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroViolenceScreen : MonoBehaviour
{
    private Image img;
    private float fadeAmount = 0f;
    private float targetAlpha = 1f;
    private bool isFading = true;

    private void Start()
    {
        img = GetComponent<Image>();
        
        // Dezactivăm cursorul
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Pornim procesul de Fade In
        isFading = true;
    }

    private void Update()
    {
        if (isFading)
        {
            // Calculăm tranziția între 0 (invizibil) și 1 (vizibil)
            fadeAmount = Mathf.MoveTowards(fadeAmount, targetAlpha, Time.deltaTime);
            
            Color color = img.color;
            color.a = fadeAmount;
            img.color = color;

            // Dacă am ajuns la pragul dorit
            if (fadeAmount == targetAlpha)
            {
                if (targetAlpha == 1f)
                {
                    // Textul e vizibil, așteptăm 3 secunde și pornim Fade Out
                    isFading = false;
                    targetAlpha = 0f;
                    Invoke("StartFade", 3f); 
                }
                else
                {
                    // Fade Out s-a terminat, schimbăm scena
                    GoToNextScene();
                }
            }
        }
    }

    private void StartFade() 
    {
        isFading = true;
    }

    private void GoToNextScene()
    {
        // Logica de redirecționare bazată pe progres
        if (!GameProgressSaver.GetIntro() || !GameProgressSaver.GetTutorial())
        {
            SceneManager.LoadScene("Tutorial");
        }
        else
        {
            SceneManager.LoadScene("Main Menu");
        }
    }
}