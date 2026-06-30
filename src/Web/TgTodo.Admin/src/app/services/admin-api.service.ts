import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminTask, AdminUserOverview } from '../models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/admin/api';

  login(key: string): Observable<{ ok: boolean }> {
    return this.http.post<{ ok: boolean }>(`${this.baseUrl}/login`, { key });
  }

  getOverview(): Observable<AdminUserOverview[]> {
    return this.http.get<AdminUserOverview[]>(`${this.baseUrl}/overview`);
  }

  getUserTasks(userId: string): Observable<AdminTask[]> {
    return this.http.get<AdminTask[]>(`${this.baseUrl}/users/${userId}/tasks`);
  }
}
