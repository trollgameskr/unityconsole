# 아이콘 테스트 샘플

이 샘플은 Enhanced Console의 로그 레벨 아이콘 기능을 테스트하기 위한 스크립트입니다.

## 사용 방법

1. Unity Editor에서 `Window > Enhanced Console` 메뉴를 선택하여 Enhanced Console 창을 엽니다.
2. `IconTestScript.cs`를 씬의 GameObject에 추가합니다.
3. 플레이 모드로 들어가면 자동으로 다양한 로그가 출력됩니다.
4. Enhanced Console에서 다음을 확인할 수 있습니다:
   - 각 로그 메시지 앞에 로그 타입에 맞는 아이콘이 표시됩니다
   - 툴바의 로그/경고/에러 필터 버튼에 아이콘과 개수가 함께 표시됩니다

## 테스트 항목

- ✅ 일반 로그 (Log): 정보 아이콘
- ✅ 경고 (Warning): 경고 아이콘  
- ✅ 에러 (Error): 에러 아이콘
- ✅ 툴바 버튼의 아이콘 표시
- ✅ 로그 개수 카운팅

또는 Inspector에서 `IconTestScript` 컴포넌트의 컨텍스트 메뉴(⋮)를 클릭하고 "Test Log Icons"를 선택하여 플레이 모드 없이도 테스트할 수 있습니다.
