using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class buttonManager : MonoBehaviour
{
    [SerializeField]Button startbutton;
    [SerializeField]Button quitbutton;

    void Start()
    {
        startbutton.onClick.AddListener(start);
        quitbutton.onClick.AddListener(quit);
    }

    private void start()
    {
        GetComponentInParent<RealBallFadeAlpha>().Toggle();
    }

    private void quit()
    {
         EditorApplication.isPlaying = false;
        //System.Diagnostics.Process.GetCurrentProcess().Kill();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
