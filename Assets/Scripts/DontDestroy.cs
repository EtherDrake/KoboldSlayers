using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DontDestroy : MonoBehaviour
{
    void Awake()
    {
        var objs = GameObject.FindGameObjectsWithTag("Music");
        if(objs.Length > 1)
        {
            var otherObjs = objs.Where(x => x != this.gameObject);
            foreach(var obj in otherObjs)
            {
                Destroy(obj);
            }
        }

        var audioSource = GetComponent<AudioSource>();
        audioSource.volume = PlayerPrefs.GetFloat("MusicVol", 1f);

        DontDestroyOnLoad(this.gameObject);
    }
}
