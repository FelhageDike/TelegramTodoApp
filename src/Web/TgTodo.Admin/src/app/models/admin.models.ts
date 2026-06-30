export interface AdminUserOverview {
  userId: string;
  displayName: string;
  telegramId: number;
  balance: number;
  activeTasks: number;
  completedTasks: number;
}

export interface AdminTask {
  id: string;
  title: string;
  scope: string | number;
  status: string | number;
  recurrence: string | number;
  userCompletionCount: number;
}

export type TaskFilter = 'active' | 'completed';
