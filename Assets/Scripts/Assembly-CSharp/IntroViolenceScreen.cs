using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroViolenceScreen : MonoBehaviour
{
    private void Start()
    {
        GoToNextScene();
    }

    private void GoToNextScene()
    {
        // Verifică unde trebuie să trimită jucătorul
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