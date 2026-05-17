# DevSync

실시간 채팅 연동형 지라 스타일 업무 관리 시스템 예제입니다.

## 실행 방법

```bash
docker compose up --build
```

- Blazor Client: http://localhost:8080
- ASP.NET Core API: http://localhost:5010
- Swagger: http://localhost:5010/swagger
- SQL Server: localhost,1433

## 데모 데이터

- 사용자 1: 홍길동
- 사용자 2: 김철수
- 기본 룸 ID: 1
- 초대 토큰: `devsync-demo-token`

## 구조

- `src/DevSync.Api`: ASP.NET Core Web API, EF Core, SignalR Hub
- `src/DevSync.Client`: Blazor WebAssembly UI
- `src/DevSync.Shared`: 서버와 클라이언트가 공유하는 DTO와 Enum
# DevSync
