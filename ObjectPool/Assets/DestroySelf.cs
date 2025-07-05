using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroySelf : MonoBehaviour
{
    private void OnEnable()
    {
        StartCoroutine(Destroy());
    }
    
    private IEnumerator Destroy()
    {
        yield return new WaitForSeconds(3f);
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
}
