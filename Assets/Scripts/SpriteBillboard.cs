using UnityEngine;

public class SpriteBillboard : MonoBehaviour
{
    private Camera _mainCam;

    private void Start()
    {
        _mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (_mainCam != null)
        {
            // 스프라이트가 항상 카메라와 평행하게 마주보도록 회전값을 동기화
            transform.rotation = _mainCam.transform.rotation;
        }
    }
}