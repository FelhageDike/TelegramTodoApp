import { Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
import { AdminTask, AdminUserOverview, TaskFilter } from '../../models/admin.models';
import {
  isPendingTaskStatus,
  taskRecurrenceLabel,
  taskScopeLabel,
  taskStatusLabel,
} from '../../models/task-enums';
import { AdminApiService } from '../../services/admin-api.service';

@Component({
  selector: 'app-tasks-dialog',
  standalone: true,
  templateUrl: './tasks-dialog.component.html',
  styleUrl: './tasks-dialog.component.css',
})
export class TasksDialogComponent implements OnInit {
  private readonly api = inject(AdminApiService);

  readonly user = input.required<AdminUserOverview>();
  readonly initialTab = input<TaskFilter>('active');
  readonly closed = output<void>();

  readonly allTasks = signal<AdminTask[]>([]);
  readonly tab = signal<TaskFilter>('active');
  readonly loading = signal(true);
  readonly error = signal('');

  readonly activeTasks = computed(() =>
    this.allTasks().filter((t) => isPendingTaskStatus(t.status)),
  );

  readonly completedTasks = computed(() =>
    this.allTasks().filter((t) => !isPendingTaskStatus(t.status)),
  );

  readonly visibleTasks = computed(() =>
    this.tab() === 'active' ? this.activeTasks() : this.completedTasks(),
  );

  ngOnInit(): void {
    this.tab.set(this.initialTab());
    this.api.getUserTasks(this.user().userId).subscribe({
      next: (items) => {
        this.allTasks.set(items);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.error ?? 'Не удалось загрузить задачи');
      },
    });
  }

  setTab(value: TaskFilter): void {
    this.tab.set(value);
  }

  dismiss(): void {
    this.closed.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.dismiss();
    }
  }

  scopeLabel(scope: string | number): string {
    return taskScopeLabel(scope);
  }

  statusLabel(status: string | number): string {
    return taskStatusLabel(status);
  }

  recurrenceLabel(recurrence: string | number): string {
    return taskRecurrenceLabel(recurrence);
  }

  statusClass(status: string | number): string {
    if (isPendingTaskStatus(status)) return 'badge-pending';
    const label = taskStatusLabel(status).toLowerCase();
    if (label === 'completed') return 'badge-completed';
    return 'badge-muted';
  }
}
