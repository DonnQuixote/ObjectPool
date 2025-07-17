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
        yield return new WaitForSeconds(1.5f);
        ObjectPoolManager.Instance.ReturnObjectToPool(gameObject,ObjectPoolManager.PoolType.GameObject,true);
    }
}
