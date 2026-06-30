import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AdminUserOverview, TaskFilter } from '../../models/admin.models';
import { AdminApiService } from '../../services/admin-api.service';
import { AuthService } from '../../services/auth.service';
import { TasksDialogComponent } from '../../components/tasks-dialog/tasks-dialog.component';

type SortKey = 'name' | 'balance' | 'active' | 'completed';
type SortDir = 'asc' | 'desc';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [TasksDialogComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent {
  private readonly api = inject(AdminApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly users = signal<AdminUserOverview[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly search = signal('');
  readonly sortKey = signal<SortKey>('name');
  readonly sortDir = signal<SortDir>('asc');

  readonly selectedUser = signal<AdminUserOverview | null>(null);
  readonly taskFilter = signal<TaskFilter>('active');
  readonly dialogOpen = signal(false);

  readonly filteredUsers = computed(() => {
    const q = this.search().trim().toLowerCase();
    const key = this.sortKey();
    const dir = this.sortDir();

    return [...this.users()]
      .filter((u) => {
        if (!q) return true;
        return (
          u.displayName.toLowerCase().includes(q) ||
          String(u.telegramId).includes(q)
        );
      })
      .sort((a, b) => {
        let av: string | number;
        let bv: string | number;
        switch (key) {
          case 'balance':
            av = a.balance;
            bv = b.balance;
            break;
          case 'active':
            av = a.activeTasks;
            bv = b.activeTasks;
            break;
          case 'completed':
            av = a.completedTasks;
            bv = b.completedTasks;
            break;
          default:
            av = a.displayName.toLowerCase();
            bv = b.displayName.toLowerCase();
        }
        if (av < bv) return dir === 'asc' ? -1 : 1;
        if (av > bv) return dir === 'asc' ? 1 : -1;
        return 0;
      });
  });

  readonly stats = computed(() => {
    const rows = this.users();
    return {
      totalUsers: rows.length,
      totalBalance: rows.reduce((s, u) => s + u.balance, 0),
      totalActive: rows.reduce((s, u) => s + u.activeTasks, 0),
      totalCompleted: rows.reduce((s, u) => s + u.completedTasks, 0),
    };
  });

  constructor() {
    this.loadOverview();
  }

  onSearch(value: string): void {
    this.search.set(value);
  }

  toggleSort(key: SortKey): void {
    if (this.sortKey() === key) {
      this.sortDir.update((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortKey.set(key);
      this.sortDir.set('asc');
    }
  }

  sortIcon(col: SortKey): 'none' | 'asc' | 'desc' {
    if (this.sortKey() !== col) return 'none';
    return this.sortDir();
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }

  openTasks(user: AdminUserOverview, filter: TaskFilter): void {
    this.selectedUser.set(user);
    this.taskFilter.set(filter);
    this.dialogOpen.set(true);
  }

  closeDialog(): void {
    this.dialogOpen.set(false);
    this.selectedUser.set(null);
  }

  formatBalance(value: number): string {
    return value > 0 ? value.toLocaleString('ru-RU') : '—';
  }

  private loadOverview(): void {
    this.loading.set(true);
    this.error.set('');

    this.api.getOverview().subscribe({
      next: (rows) => {
        this.users.set(rows);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 401) {
          this.auth.logout();
          void this.router.navigate(['/login']);
          return;
        }
        this.error.set(err.error?.error ?? 'Не удалось загрузить данные');
      },
    });
  }
}
