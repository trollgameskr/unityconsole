using UnityEngine;

/// <summary>
/// 콘솔 아이콘 테스트용 스크립트
/// Enhanced Console 창을 열고 이 스크립트를 실행하면 다양한 로그 타입의 메시지가 아이콘과 함께 표시됩니다.
/// </summary>
public class IconTestScript : MonoBehaviour
{
    void Start()
    {
        TestLogIcons();
    }

    [ContextMenu("Test Log Icons")]
    public void TestLogIcons()
    {
        Debug.Log("일반 로그 메시지입니다. 정보 아이콘이 표시되어야 합니다.");
        Debug.LogWarning("경고 메시지입니다. 경고 아이콘이 표시되어야 합니다.");
        Debug.LogError("에러 메시지입니다. 에러 아이콘이 표시되어야 합니다.");
        
        Debug.Log("여러 개의 일반 로그 1");
        Debug.Log("여러 개의 일반 로그 2");
        Debug.Log("여러 개의 일반 로그 3");
        
        Debug.LogWarning("여러 개의 경고 1");
        Debug.LogWarning("여러 개의 경고 2");
        
        Debug.LogError("여러 개의 에러 1");
        Debug.LogError("여러 개의 에러 2");
        
        Debug.Log("테스트 완료! Enhanced Console에서 각 메시지 앞에 아이콘이 표시되는지, 툴바 버튼에도 아이콘이 표시되는지 확인하세요.");
    }
}
