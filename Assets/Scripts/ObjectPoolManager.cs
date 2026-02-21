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

    public void Initialize()
    {
        foreach (var config in configs)
        {
            if (!pools.ContainsKey(config.type))
            {
                // 로직 분리를 위해 지역 변수 캡처
                var targetPrefab = config.prefab;

                pools[config.type] = new ObjectPool<GameObject>(
                    createFunc: () => Instantiate(targetPrefab, this.transform), // 생성
                    actionOnGet: (obj) => obj.SetActive(true),                   // 대여 시
                    actionOnRelease: (obj) => obj.SetActive(false),               // 반환 시
                    actionOnDestroy: (obj) => Destroy(obj),                      // 삭제 시
                    collectionCheck: true,                                       // 중복 반환 검사
                    defaultCapacity: config.defaultCapacity,
                    maxSize: config.maxSize
                );

                // 초기 할당 (Pre-warm)
                List<GameObject> temp = new List<GameObject>();
                for(int i = 0; i < config.defaultCapacity; i++) 
                    temp.Add(pools[config.type].Get());
                foreach(var obj in temp) 
                    pools[config.type].Release(obj);
            }
        }
    }

    public GameObject GetFromPool(int type, Transform parent)
    {
        if (!pools.ContainsKey(type)) return null;

        GameObject obj = pools[type].Get();
        obj.transform.SetParent(parent);
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