export function isPendingTaskStatus(status: string | number): boolean {
  if (typeof status === 'number') return status === 0;
  return String(status).toLowerCase() === 'pending';
}

export function taskScopeLabel(scope: string | number): string {
  if (typeof scope === 'number') return scope === 1 ? 'group' : 'personal';
  return String(scope).toLowerCase() === 'group' ? 'group' : 'personal';
}

export function taskStatusLabel(status: string | number): string {
  if (typeof status === 'number') {
    return status === 0 ? 'Pending' : status === 1 ? 'Completed' : 'Archived';
  }
  return String(status);
}

export function taskRecurrenceLabel(recurrence: string | number): string {
  if (typeof recurrence === 'number') {
    const labels = ['None', 'Daily', 'Weekly', 'Monthly', 'EveryNDays'];
    return labels[recurrence] ?? String(recurrence);
  }
  return String(recurrence);
}
