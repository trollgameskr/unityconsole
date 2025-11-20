using UnityEngine;

/// <summary>
/// 채널 필터링 테스트용 스크립트
/// Enhanced Console 창을 열고 이 스크립트를 실행하면 다양한 채널 태그가 포함된 로그가 생성됩니다.
/// </summary>
public class ChannelFilterTestScript : MonoBehaviour
{
    void Start()
    {
        TestChannelFiltering();
    }

    [ContextMenu("Test Channel Filtering")]
    public void TestChannelFiltering()
    {
        // 요구사항의 예시 로그들
        Debug.Log("[AI][Pathfinding] 이동 중 (10, 2.0)");
        Debug.Log("[Audio][Sfx] 효과음 재생 \"SFX_Hit\"");
        Debug.Log("[Setup] Target missing!");
        Debug.Log("[Annie-PC] 테스트용 메시지");
        
        // 추가 테스트 케이스
        Debug.Log("[UI][Button] 버튼 클릭 이벤트 처리");
        Debug.Log("[Network][Connection] 서버 연결 성공");
        Debug.Log("[Physics][Collision] 충돌 감지: Player vs Enemy");
        Debug.LogWarning("[AI][Decision] 경로를 찾을 수 없음");
        Debug.LogError("[Network][Error] 연결 시간 초과");
        
        // 채널이 없는 로그
        Debug.Log("채널 태그가 없는 일반 로그");
        
        // 단일 채널
        Debug.Log("[Graphics] 렌더링 파이프라인 초기화");
        
        // 여러 채널
        Debug.Log("[AI][Pathfinding][Debug] A* 알고리즘 실행 완료");
        
        Debug.Log("채널 필터링 테스트 완료! Enhanced Console의 채널 바에서 각 채널을 활성/비활성화하여 필터링을 테스트하세요.");
    }
}
