using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class IntroText : MonoBehaviour
{
    private AudioSource aud;

    // Referințe păstrate pentru a nu genera erori în Inspector
    public GameObject[] calibrationWindows;
    public GameObject[] activateOnEnd;
    public GameObject[] deactivateOnEnd;
    public GameObject[] activateOnTextTrigger;

    private void Start()
    {
        aud = GetComponent<AudioSource>();
        // Dezactivăm cursorul din start
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        InstantSkip();
    }

    private void InstantSkip()
    {
        // Activăm toate declanșatoarele care în mod normal apăreau în timpul textului (@)
        foreach (GameObject obj in activateOnTextTrigger)
        {
            if (obj != null) obj.SetActive(true);
        }

        // Marcare finalizare în controlerul părinte (dacă există)
        var controller = GetComponentInParent<IntroTextController>();
        if (controller != null) controller.introOver = true;
        
        // Salvare progres automată
        GameProgressSaver.SetIntro(beat: true);

        // Executăm finalizarea (schimbarea scenelor/obiectelor de start)
        Over();
    }

    private void Over()
    {
        // Activăm obiectele finale ale jocului
        foreach (GameObject obj in activateOnEnd)
        {
            if (obj != null) obj.SetActive(true);
        }

        // Dezactivăm obiectele de intro
        foreach (GameObject obj in deactivateOnEnd)
        {
            if (obj != null) obj.SetActive(false);
        }
    }
}