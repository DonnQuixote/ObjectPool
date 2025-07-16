using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolManager : MonoBehaviour
{
    //volatile 确保线程修改时对其他线程的访问是可见的;不保证原子性，适用于计时器等场景
    [SerializeField] private volatile bool _addToDontDestroyOnLoad = false;
    private GameObject _emptyHolder;

    private static GameObject _particleSystemsEmpty;
    private static GameObject _gameObjectEmpty;
    private static GameObject _soundFXEmpty;

    private static Dictionary<GameObject, ObjectPool<GameObject>> _objectPools;
    private static Dictionary<GameObject, GameObject> _cloneToPrefabMap;
    public struct ObjectClips
    {
        public AudioClip generateClip;
        public AudioClip deleteClip;
        public ObjectClips(AudioClip _generateClip, AudioClip _deleteClip)
        {
            generateClip = _generateClip;
            deleteClip = _deleteClip;
        }
    }
    private static Dictionary<GameObject, ObjectClips> _objectToClip;
    private AudioClip tempDeleteClip;
    public enum PoolType
    {
        ParticleSystem,
        GameObject,
        SoundFX
    }

    private void Awake()
    {
        _objectPools = new Dictionary<GameObject, ObjectPool<GameObject>>();
        _cloneToPrefabMap = new Dictionary<GameObject, GameObject>();
        _objectToClip = new Dictionary<GameObject, ObjectClips>();
        SetupParentLevels();
    }

    private void SetupParentLevels()
    {
        _emptyHolder = new GameObject("Object Pool");

        _particleSystemsEmpty = new GameObject("Particle Effects");
        _particleSystemsEmpty.transform.SetParent(_emptyHolder.transform);
        _gameObjectEmpty = new GameObject("GameObject");
        _gameObjectEmpty.transform.SetParent(_emptyHolder.transform);
        _soundFXEmpty = new GameObject("Sound FX");
        _soundFXEmpty.transform.SetParent(_emptyHolder.transform);

        if (_addToDontDestroyOnLoad)
            DontDestroyOnLoad(_particleSystemsEmpty);
    }

    static void CreatePool(GameObject prefab, Vector3 pos, Quaternion rot, PoolType poolType = PoolType.GameObject)
    {
        ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () => CreateGameObject(prefab, pos, rot, poolType),
            actionOnGet: OnGetObject,
            actionOnRelease: OnReleaseObject,
            actionOnDestroy: OnDestroyObject
        );
        _objectPools.Add(prefab, pool);
    }

    static GameObject CreateGameObject(GameObject prefab, Vector3 pos, Quaternion rot,
        PoolType poolType = PoolType.GameObject)
    {
        prefab.SetActive(false);
        GameObject obj = Instantiate(prefab, pos, rot);
        prefab.SetActive(true);
        GameObject parentObject = SetParentObject(poolType);
        obj.transform.SetParent(parentObject.transform);
        return obj;
    }

    static void OnGetObject(GameObject obj)
    {
        //OptionalAction
        Debug.Log("GetObject");
    }

    static void OnReleaseObject(GameObject obj)
    {
        Debug.Log("ReleaseObject");
        obj.SetActive(false);
        // AudioSource.PlayClipAtPoint(_objectToClip[obj].deleteClip, obj.transform.position, 1f);
        SoundClipManager.Instance.PlayPooledSound(_objectToClip[obj].deleteClip, obj.transform.position, 1f);
    }

    static void OnDestroyObject(GameObject obj)
    {
        if (_cloneToPrefabMap.ContainsKey(obj))
        {
            Debug.Log("DestroyObject");
            _cloneToPrefabMap.Remove(obj);
        }
    }

    static GameObject SetParentObject(PoolType poolType)
    {
        switch (poolType)
        {
            case PoolType.GameObject:
                return _gameObjectEmpty;
            case PoolType.ParticleSystem:
                return _particleSystemsEmpty;
            case PoolType.SoundFX:
                return _soundFXEmpty;
            default:
               return null;
        }
    }

    private static T SpawnObject<T>(GameObject objectToSpawn, Vector3 spawnPos, Quaternion spawnRot, PoolType poolType = PoolType.GameObject,
        AudioClip generateClip = null,AudioClip deleteClip = null) where T : UnityEngine.Object
    {
        if (!_objectPools.ContainsKey(objectToSpawn))
        {
            CreatePool(objectToSpawn, spawnPos, spawnRot, poolType);
        }

        GameObject obj = _objectPools[objectToSpawn].Get();

        if (obj != null)
        {
            if (!_cloneToPrefabMap.ContainsKey(obj))
            {
                _cloneToPrefabMap.Add(obj,objectToSpawn);
            }
            
            if (generateClip || deleteClip)
            {
                if (!_objectToClip.ContainsKey(obj))
                {
                    ObjectClips oc = new ObjectClips(generateClip, deleteClip);
                    _objectToClip.Add(obj,oc);
                }
            }

            Rigidbody rigidBody = obj.GetComponent<Rigidbody>();
            if (rigidBody != null)
            {
                rigidBody.velocity = Vector3.zero;
            }
            
            obj.transform.position = spawnPos;
            obj.transform.rotation = spawnRot;
            obj.SetActive(true);
            // AudioSource.PlayClipAtPoint(generateClip,spawnPos,0.6f);
            SoundClipManager.Instance.PlayPooledSound(generateClip, spawnPos, 0.6f);
            Debug.Log("SpawnSuccess");

            if (typeof(T) == typeof(GameObject))
            {
                return obj as T;
            }

            T component = obj.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"Object {objectToSpawn.name} doesn't have component of type {typeof(T)}");
                return null;
            }

            return component;
        }
        else
        {
            Debug.Log("Getting a null object from the pool! ");
        }

        return null;
    }
    public static T SpawnObject<T>(T objectToSpawn, Vector3 spawnPos, Quaternion spawnRot, PoolType poolType = PoolType.GameObject,
        AudioClip generateClip = null,AudioClip deleteClip = null) where T : Component
    {
        return SpawnObject<T>(objectToSpawn.gameObject,  spawnPos,  spawnRot, poolType,generateClip,deleteClip);
    }

    public static GameObject SpawnObject(GameObject objectToSpawn, Vector3 spawnPos, Quaternion spawnRot, PoolType poolType = PoolType.GameObject,
        AudioClip generateClip = null,AudioClip deleteClip = null)
    {
        return SpawnObject<GameObject>(objectToSpawn.gameObject,  spawnPos,  spawnRot, poolType,generateClip,deleteClip);
    }

    public static void ReturnObjectToPool(GameObject obj, PoolType poolType = PoolType.GameObject,bool useDeleteClip = false)
    {
        if (_cloneToPrefabMap.TryGetValue(obj, out GameObject prefab))
        {
            GameObject parentObject = SetParentObject(poolType);
            if (obj.transform.parent != parentObject.transform)
            {
                obj.transform.SetParent(parentObject.transform);
            }

            if (_objectPools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
            {
                pool.Release(obj);
            }
        }
        else
        {
            Debug.LogWarning("Trying to return an object that is not pooled" + obj.name);
        }
    }
}
