# Unity Console Enhanced

Unity 기본 콘솔에 Time.frameCount를 추가한 확장 콘솔 패키지입니다.

## 기능

- Unity 기본 콘솔과 유사한 UI
- **로그 레벨 아이콘**: 로그 타입(Log, Warning, Error)별로 직관적인 아이콘 표시
- 홀수/짝수 행 배경색 구분으로 가독성 향상 (회색/진한회색)
- Time.frameCount 컬럼 표시/숨김 설정
- Time.fixedTime 표시/숨김 설정
- DateTime (타임스탬프) 표시/숨김 설정
- 로그 타입별 필터링 (Log, Warning, Error)
- **검색 필터**: 특정 단어가 포함된 로그만 표시
- **채널 시스템**: 로그 메시지의 [채널명] 태그를 자동으로 파싱하여 채널별 필터링 지원
  - 로그에서 자동으로 채널 감지 및 등록
  - 채널별 활성/비활성 토글
  - 전체선택/전체해제 기능
  - 활성화된 채널의 로그만 표시 (여러 채널 중 하나라도 활성화되어 있으면 표시)
  - EditorPrefs를 사용한 채널 상태 저장
- **태그 기능**: 태그를 추가/삭제하고, 태그를 클릭하여 해당 태그가 포함된 로그만 표시
- Collapse 모드
- Clear on Play
- Error Pause
- 로그 더블 클릭으로 IDE에서 해당 스크립트 파일 열기
- 스택 트레이스에서 UnityEngine.Debug:Log 라인 자동 숨김

## 설치 방법

1. Unity Package Manager를 엽니다
2. "Add package from git URL"을 선택합니다
3. 이 저장소의 URL을 입력합니다

또는 이 저장소를 Unity 프로젝트의 `Packages` 폴더에 직접 복사합니다.

## 사용 방법

1. Unity Editor에서 `Window > Enhanced Console` 메뉴를 선택합니다
2. Enhanced Console 창이 열립니다
3. 툴바에서 다양한 옵션을 설정할 수 있습니다:
   - **검색**: 특정 단어가 포함된 로그만 필터링하여 표시
   - **채널**: 로그 메시지의 [채널명] 태그를 자동으로 감지하여 채널별 필터링
     - 예: `Debug.Log("[AI][Pathfinding] 이동 중");`
     - 발견된 채널은 자동으로 등록되고 기본적으로 활성화됨
     - 채널 버튼을 클릭하여 활성/비활성 토글
     - 전체선택/전체해제 버튼으로 모든 채널 일괄 제어
     - 채널 X 버튼으로 채널 삭제
   - **태그**: 태그를 추가/삭제하고 태그 버튼을 클릭하여 해당 태그가 포함된 로그만 필터링
   - **Show Frame Count**: Time.frameCount 표시/숨김
   - **Show Fixed Time**: Time.fixedTime 표시/숨김
   - **Show Timestamp**: DateTime.Now 타임스탬프 표시/숨김
4. 로그를 더블 클릭하면 IDE에서 해당 스크립트 파일이 열립니다

### 채널 시스템 사용 예시

```csharp
// 단일 채널
Debug.Log("[AI] AI 시스템 초기화 완료");

// 다중 채널
Debug.Log("[AI][Pathfinding] 경로 탐색 시작");
Debug.Log("[Audio][Sfx] 효과음 재생");

// 채널이 없는 로그
Debug.Log("일반 로그 메시지");
```

채널 필터링 우선순위:
1. **채널 필터** (활성화된 채널이 있을 경우)
2. 검색 필터
3. 태그 필터

## 요구 사항

- Unity 6000.0 이상

## 라이선스

MIT License