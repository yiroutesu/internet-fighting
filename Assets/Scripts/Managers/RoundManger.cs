using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManger : MonoBehaviour
{
    private WaveManager waveManager;
    private EquippedItemsManager equippedItemsManager;
    [SerializeField] private GameObject ShopAndBag;
    [SerializeField] private  HealthBarSmooth healthBarSmooth;

   
    private void Start() {
        waveManager=WaveManager.Instance;
        equippedItemsManager=EquippedItemsManager.Instance;
        waveManager.OnWaveCompleted += RoundEnd;
        RoundStart();
    }

    public void RoundEnd(int waveNumber)
    {
        equippedItemsManager.PropAttrRemove();
        OrthoCameraIntro.Instance?.EndRoundTransition(()=>{
            ShopAndBag.SetActive(true);
        })
        ;
        
    }
    public void RoundStart()
    {
        Debug.Log("Round Start");
        waveManager.RoundStart();
        equippedItemsManager.ReequipAll();
        equippedItemsManager.PropAttrCalculate();
        equippedItemsManager.SetXPpickRange();
        
        ShopAndBag.SetActive(false);
        waveManager.StartNextWave();
    }
}
