using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    public Transform playerTransform;
    public float zMoveRatio = 0.2f;

    private float initialPlayerZ;
    private float initialTargetZ;

    private void Start()
    {
        if (playerTransform != null)
        {
            // 게임 시작 시 플레이어와 타겟의 초기 Z축 위치를 저장
            initialPlayerZ = playerTransform.position.z;
            
            // 타겟의 초기 위치를 플레이어와 일치시키려면 아래 주석을 해제하여 사용
            // transform.position = playerTransform.position;
            
            initialTargetZ = transform.position.z;
        }
    }

    private void LateUpdate()
    {
        if (playerTransform != null)
        {
            float targetX = playerTransform.position.x;
            float targetY = playerTransform.position.y;
            
            // 플레이어가 초기 위치로부터 이동한 상대 거리 계산
            float deltaZ = playerTransform.position.z - initialPlayerZ;
            
            // 이동한 거리에만 비율을 곱하여 최종 Z 위치 적용
            float targetZ = initialTargetZ + (deltaZ * zMoveRatio);

            transform.position = new Vector3(targetX, targetY, targetZ);
        }
    }
}