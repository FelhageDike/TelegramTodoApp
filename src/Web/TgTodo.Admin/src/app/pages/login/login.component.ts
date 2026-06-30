import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminApiService } from '../../services/admin-api.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  key = '';
  readonly error = signal('');
  readonly loading = signal(false);
  readonly showKey = signal(false);
  readonly shaking = signal(false);
  readonly oidcLoading = signal(false);

  ngOnInit(): void {
    void this.auth.init().then(() => {
      if (this.auth.isAuthenticated()) {
        void this.router.navigate(['/']);
      }
    });
  }

  toggleShow(): void {
    this.showKey.update((v) => !v);
  }

  submit(): void {
    this.error.set('');
    this.loading.set(true);

    this.api.login(this.key.trim()).subscribe({
      next: () => {
        this.auth.setKey(this.key.trim());
        void this.router.navigate(['/']);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.error ?? 'Неверный ключ');
        this.shaking.set(true);
        setTimeout(() => this.shaking.set(false), 500);
      },
      complete: () => this.loading.set(false),
    });
  }

  async loginWithSso(): Promise<void> {
    this.error.set('');
    this.oidcLoading.set(true);
    try {
      await this.auth.loginWithOidc();
    } catch {
      this.error.set('Не удалось начать вход через SSO.');
      this.oidcLoading.set(false);
    }
  }
}
