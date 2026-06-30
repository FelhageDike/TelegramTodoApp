import { Injectable, inject, signal } from '@angular/core';

import { HttpClient } from '@angular/common/http';

import { OAuthService } from 'angular-oauth2-oidc';

import { firstValueFrom } from 'rxjs';

import { AdminAuthConfig } from '../models/auth.models';



const STORAGE_KEY = 'tgtodo-admin-key';



@Injectable({ providedIn: 'root' })

export class AuthService {

  private readonly http = inject(HttpClient);

  private readonly oauth = inject(OAuthService);



  readonly config = signal<AdminAuthConfig | null>(null);

  readonly initError = signal('');

  private initialized = false;



  async init(): Promise<void> {

    if (this.initialized) return;

    this.initialized = true;



    try {

      const config = await firstValueFrom(

        this.http.get<AdminAuthConfig>('/admin/api/auth/config'),

      );

      this.config.set(config);



      if (config.oidcEnabled && config.issuer && config.clientId) {

        const origin = window.location.origin;

        this.oauth.configure({

          issuer: config.issuer,

          clientId: config.clientId,

          redirectUri: `${origin}/admin/`,

          postLogoutRedirectUri: `${origin}/admin/login`,

          responseType: 'code',

          scope: config.scope,

          showDebugInformation: false,

          strictDiscoveryDocumentValidation: false,

          requireHttps: window.location.protocol === 'https:',

          useSilentRefresh: false,

          sessionChecksEnabled: false,

        });



        await this.oauth.loadDiscoveryDocumentAndTryLogin();

        if (this.oauth.hasValidAccessToken()) {
          window.history.replaceState({}, document.title, `${origin}/admin/`);
        }

      }

    } catch {

      this.initError.set('Не удалось загрузить настройки авторизации.');

    }

  }



  isAuthenticated(): boolean {

    if (this.oauth.hasValidAccessToken()) return true;

    return !!sessionStorage.getItem(STORAGE_KEY);

  }



  getKey(): string | null {

    return sessionStorage.getItem(STORAGE_KEY);

  }



  getAccessToken(): string | null {

    return this.oauth.hasValidAccessToken() ? this.oauth.getAccessToken() : null;

  }



  setKey(key: string): void {

    sessionStorage.setItem(STORAGE_KEY, key);

  }



  async loginWithOidc(): Promise<void> {

    await this.init();

    if (!this.config()?.oidcEnabled) {

      throw new Error('OIDC is not enabled');

    }

    this.oauth.initLoginFlow();

  }



  logout(): void {

    sessionStorage.removeItem(STORAGE_KEY);

    if (this.oauth.hasValidAccessToken()) {

      this.oauth.logOut();

      return;

    }

    window.location.href = '/admin/login';

  }



  secretKeyEnabled(): boolean {

    return this.config()?.secretKeyAuthEnabled ?? true;

  }



  oidcEnabled(): boolean {

    return this.config()?.oidcEnabled ?? false;

  }

}

