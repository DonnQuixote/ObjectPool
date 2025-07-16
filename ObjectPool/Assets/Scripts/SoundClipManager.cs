using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

public  class SoundClipManager:MonoBehaviour
{
    private static SoundClipManager instance;
    private static readonly object _lock = new object();
    public static SoundClipManager Instance {
        get {
            if (instance == null)
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = FindObjectOfType<SoundClipManager>();
                    }
                }
            }
            return instance;
        }
    }
    private static Queue<GameObject> audioPool;
    private void Awake()
    {
        audioPool = new Queue<GameObject>();
    }
    public  void PlayPooledSound(AudioClip clip, Vector3 position, float volume = 1.0f)
    {
        GameObject obj = null;
        if (audioPool.Count > 0)
        {
            lock (_lock)
            {
                if (audioPool.Count > 0)
                {
                    obj = audioPool.Dequeue();
                    obj.SetActive(true);
                }
            }
        }
        if(obj == null)
        {
            obj = new GameObject("One shot Audio");
            obj.AddComponent<AudioSource>();
        }
        
        obj.transform.position = position;
        AudioSource audioSource = obj.GetComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.spatialBlend = 1f;
        audioSource.volume = volume;
        audioSource.Play();

        float destroyDelay = clip.length * ((double)Time.timeScale < 0.009999999776482582 ? 0.01f : Time.timeScale);
        StartCoroutine(ReturnToPool(obj, destroyDelay));
    }
    
    private IEnumerator ReturnToPool(GameObject obj,float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
        audioPool.Enqueue(obj);
    }
}
