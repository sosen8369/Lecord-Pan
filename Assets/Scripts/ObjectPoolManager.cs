using UnityEngine;
using UnityEngine.Pool; // 공식 풀링 네임스페이스
using System.Collections.Generic;

public class ObjectPoolManager : MonoBehaviour
{
    [System.Serializable]
    public struct PoolConfig
    {
        public int type;
        public GameObject prefab;
        public int defaultCapacity;
        public int maxSize;
    }

    [SerializeField] private List<PoolConfig> configs;
    
    // 타입별로 공식 ObjectPool을 저장하는 딕셔너리
    private Dictionary<int, IObjectPool<GameObject>> pools = new Dictionary<int, IObjectPool<GameObject>>();

    // ObjectPoolManager.cs 내부
public void Initialize()
{
    if (configs == null || configs.Count == 0)
    {
        Debug.LogWarning("PoolConfig가 비어있습니다. 인스펙터를 확인하세요.");
        return;
    }

    foreach (var config in configs)
    {
        if (!pools.ContainsKey(config.type))
        {
            var targetPrefab = config.prefab;
            if (targetPrefab == null)
            {
                Debug.LogError($"타입 {config.type}의 프리팹이 할당되지 않았습니다.");
                continue;
            }

            pools[config.type] = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(targetPrefab, this.transform),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: true,
                defaultCapacity: config.defaultCapacity,
                maxSize: config.maxSize
            );

            Debug.Log($"[Pool] 타입 {config.type} 초기화 완료 (기본 용량: {config.defaultCapacity})");
        }
    }
}

    public GameObject GetFromPool(int type, Transform parent)
    {
        if (pools == null || !pools.ContainsKey(type)) 
        {
            Debug.LogError($"타입 {type}에 해당하는 풀이 초기화되지 않았습니다.");
            return null;
        }

        GameObject obj = pools[type].Get();
        if (obj != null)
        {
            obj.transform.SetParent(parent);
        }
        return obj;
    }

    public void ReturnToPool(int type, GameObject obj)
    {
        if (!pools.ContainsKey(type))
        {
            Destroy(obj); // 풀이 없으면 파괴
            return;
        }
        pools[type].Release(obj);
    }
}