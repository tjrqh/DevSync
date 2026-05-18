# DevSync

실시간 채팅과 지라 스타일 칸반 보드를 결합한 학원 팀 프로젝트용 업무 관리 시스템입니다. 비공개 워크스페이스 초대, 멘션 알림, 작업 상태 변경 시스템 메시지, Slack/Notion 방향의 협업 UI를 ASP.NET Core와 Blazor WebAssembly로 구현했습니다.

## Tech Stack

| 영역 | 기술 |
| --- | --- |
| Backend | ASP.NET Core Web API, SignalR, EF Core 8 |
| Frontend | Blazor WebAssembly, JavaScript interop |
| Database | MySQL 8.0, EF Core InMemory/SQL Server 전환 가능 |
| Runtime | .NET 8, Docker Compose |

## 주요 기능

- 비공개 룸: 초대 토큰이 일치하고 승인된 멤버만 프로젝트 룸에 입장
- 칸반 업무 관리: `Todo`, `InProgress`, `Done` 상태 기반 작업 생성/수정/드래그 이동
- 실시간 채팅: 같은 룸 멤버끼리 SignalR 기반 메시지 송수신
- 멘션 UX: 내 태그만 강조 표시하고, 헤더 알림에서 해당 메시지로 바로 이동
- 시스템 알림: 작업 완료 시 채팅방에 상태 변경 메시지 자동 전송
- 워크스페이스 관리: 룸 입장, 초대 승인, 멤버 확인, 룸 나가기

## 프로젝트 구조

```text
DevSync/
├── docker-compose.yml
├── src/
│   ├── DevSync.Api/       # ASP.NET Core API, EF Core, SignalR Hub
│   ├── DevSync.Client/    # Blazor WebAssembly UI
│   └── DevSync.Shared/    # DTO, Enum 등 공유 계약
└── README.md
```

## 빠른 실행

Docker Desktop을 실행한 뒤 프로젝트 루트에서 아래 명령을 실행합니다.

```bash
docker compose up --build -d
```

실행 후 접속 주소는 다음과 같습니다.

| 서비스 | URL |
| --- | --- |
| Blazor Client | http://localhost:8080 |
| ASP.NET Core API | http://localhost:5010 |
| Swagger | http://localhost:5010/swagger |
| MySQL | localhost:3306 |

MySQL JDBC URL:

```text
jdbc:mysql://localhost:3306/DevSyncDb?user=root&password=devsync_root_password
```

## 데모 계정

| 사용자 | 이메일 | 비밀번호 |
| --- | --- | --- |
| 홍길동 | `hong@example.com` | `Password123!` |
| 김철수 | `kim@example.com` | `Password123!` |

기본 룸 초대 토큰:

```text
devsync-demo-token
```

## 로컬 개발

.NET 8 SDK가 설치되어 있다면 Docker의 MySQL만 띄우고 API/Client를 개별 실행할 수 있습니다.

```bash
docker compose up -d mysql
dotnet build src/DevSync.Api/DevSync.Api.csproj
dotnet build src/DevSync.Client/DevSync.Client.csproj
```

API 실행:

```bash
dotnet run --project src/DevSync.Api/DevSync.Api.csproj --urls http://localhost:5010
```

Client 실행:

```bash
dotnet run --project src/DevSync.Client/DevSync.Client.csproj --urls http://localhost:5170
```

## 데이터베이스 설정

현재 기본 설정은 MySQL입니다.

```json
{
  "DatabaseProvider": "MySql",
  "ResetDatabaseOnStartup": true
}
```

`ResetDatabaseOnStartup`이 `true`이면 API가 시작될 때 DB를 삭제 후 다시 생성합니다. 수업용 데모 데이터를 매번 초기화하기 위한 설정이므로, 데이터를 유지하려면 `false`로 변경하세요.

지원 provider:

- `MySql`
- `SqlServer`
- `InMemory`

## API 개요

| 영역 | 엔드포인트 예시 |
| --- | --- |
| 인증 | `POST /api/auth/login` |
| 사용자 | `GET /api/users` |
| 룸 | `GET /api/rooms`, `POST /api/rooms`, `POST /api/rooms/join`, `POST /api/rooms/{id}/leave` |
| 작업 | `GET /api/tasks/room/{roomId}`, `POST /api/tasks`, `PATCH /api/tasks/{id}/status` |
| 채팅 | `GET /api/chat/room/{roomId}`, `POST /api/chat` |
| 알림 | `GET /api/notifications/user/{userId}`, `PATCH /api/notifications/{id}/read` |
| SignalR | `/hubs/project` |

## Git 관리 기준

저장소에는 소스 코드, Docker 설정, 문서만 올립니다. `bin/`, `obj/`, `.DS_Store`, IDE 개인 설정, 로컬 환경 변수, 인증서/키 파일, DB 파일은 `.gitignore`로 제외했습니다.
