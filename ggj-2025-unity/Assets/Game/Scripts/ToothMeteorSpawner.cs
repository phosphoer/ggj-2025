using System.Collections.Generic;
using UnityEngine;

public class ToothMeteorSpawner : MonoBehaviour
{
    
    public GameObject MeteorPrefab;
    public float SpawnDistance = 9f;
    public float TimerMax = 2f;
    public float TimerMin = .5f;
    public bool StartEnabled = false;

    bool stormEnabled = false;
    float timer = 0f;
    float gapTime = 0f;
    List<ToothMeteor> stormTeeth = new List<ToothMeteor>(); 

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(StartEnabled) EnableStorm();
        SetNewTimeGap();
    }

    // Update is called once per frame
    void Update()
    {
        if(stormEnabled)
        {
            timer+=Time.deltaTime;
            if(timer>=gapTime) SpawnNewTooth();
        }
    }

    public void SpawnNewTooth()
    {
        Vector3 spawnLoc = transform.position;
        spawnLoc += Vector3.right*Random.Range(SpawnDistance*-1f,SpawnDistance);

        GameObject newTooth = Instantiate(MeteorPrefab,spawnLoc,MeteorPrefab.transform.rotation, transform) as GameObject;
        stormTeeth.Add(newTooth.GetComponent<ToothMeteor>());
        newTooth.SendMessage("SetStormSpawner",this,SendMessageOptions.DontRequireReceiver);
        timer = 0f;
    }

    public void EnableStorm(bool setEnabled = true)
    {
        stormEnabled = setEnabled;
    }

    public void SetNewTimeGap()
    {
        gapTime = Random.Range(TimerMin, TimerMax);
    }

    public void CleanList()
    {
        for(int i=0; i<stormTeeth.Count; i++)
        {
            if(stormTeeth[i] == null)
            {
                stormTeeth.RemoveAt(i);
                i--;
            }
        }
    }
}
