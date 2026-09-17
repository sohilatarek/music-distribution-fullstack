import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResponse } from './models';

const TOKEN_STORAGE_KEY = 'md_jwt_token';

/**
 * Minimal auth service for the demo admin account. Token is kept in
 * localStorage for simplicity - see DECISIONS.md for the trade-off
 * (readable by any script on the page / XSS surface) vs an httpOnly
 * cookie, which would be the safer choice for a real product.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  /** Reactive flag the UI can bind to without re-reading localStorage everywhere. */
  readonly isAuthenticated = signal<boolean>(!!this.getToken());

  constructor(private http: HttpClient) {}

  login(username: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.baseUrl}/login`, { username, password })
      .pipe(
        tap((res) => {
          localStorage.setItem(TOKEN_STORAGE_KEY, res.token);
          this.isAuthenticated.set(true);
        })
      );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    this.isAuthenticated.set(false);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_STORAGE_KEY);
  }
}
