namespace DevSync.Shared.Enums;

/// <summary>
/// 지라 스타일 칸반 보드에서 하나의 업무가 가질 수 있는 상태입니다.
/// C# 기본 TaskStatus와 이름 충돌을 피하기 위해 ProjectTaskStatus로 명명했습니다.
/// </summary>
public enum ProjectTaskStatus
{
    /// <summary>아직 시작하지 않은 업무입니다.</summary>
    Todo = 0,

    /// <summary>현재 진행 중인 업무입니다.</summary>
    InProgress = 1,

    /// <summary>완료된 업무입니다.</summary>
    Done = 2
}
