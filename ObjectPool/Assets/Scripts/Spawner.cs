
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private GameObject planePrafab;
    [SerializeField] private AudioClip generateClip = null;
    [SerializeField] private AudioClip deleteClip = null;
    private int layer =0;
    private LayerMask layerMask;
    public float timeInterval = 0.04f;
    private float dt = 0;
    private void Awake()
    {
        layer = planePrafab.layer;
        layerMask = 1<<layer;
    }
    private void Update()
    {
        dt += Time.deltaTime;
        if (Input.GetMouseButton(0) && dt >= timeInterval)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 10, Color.red, 2f);  // 调试绘制射线
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit,20,layerMask))
            {
                // Debug.Log(hit.point);
                if (generateClip || deleteClip)
                {
                    ObjectPoolManager.Instance.SpawnObject(prefab, hit.point, prefab.transform.rotation,ObjectPoolManager.PoolType.SoundFX,generateClip,deleteClip);
                }
                else
                {
                    ObjectPoolManager.Instance.SpawnObject(prefab, hit.point, prefab.transform.rotation);
                }
            }else
            {
                Debug.LogWarning("Raycast did not hit anything with layerMask: " + layerMask);
            }
            dt = 0;
        }
    }
}
