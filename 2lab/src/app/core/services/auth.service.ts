import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, ReplaySubject, map, tap } from 'rxjs';

import { API_BASE_URL } from '../config/api';
import { LoginResponse, PublicUser, RecoveryConfirmResult, RecoveryRequestSent } from '../../shared/models';

/** Ключ sessionStorage для временных данных потока восстановления. */
export const RECOVERY_STORAGE_KEY = 'mock.recovery.v1';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${API_BASE_URL}/auth`;

  /** Пользователь текущей сессии (null — гость). */
  readonly user = signal<PublicUser | null>(null);

  private readonly readySubject = new ReplaySubject<void>(1);
  /** Завершается, когда статус сессии известен (используется в guards). */
  readonly ready$ = this.readySubject.asObservable();

  constructor() {
    this.http.get<PublicUser>(`${this.apiUrl}/me`).subscribe({
      next: (user) => {
        this.user.set(user);
        this.readySubject.next();
      },
      error: () => {
        this.user.set(null);
        this.readySubject.next();
      },
    });
  }

  login(login: string, password: string): Observable<PublicUser> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, { login, password }).pipe(
      map((response) => response.user),
      tap((user) => this.user.set(user)),
    );
  }

  logout(): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/logout`, {}).pipe(
      tap(() => this.user.set(null)),
    );
  }

  requestRecoveryCode(contact: string): Observable<RecoveryRequestSent> {
    return this.http.post<RecoveryRequestSent>(`${this.apiUrl}/recovery/request`, { contact });
  }

  confirmRecoveryCode(contact: string, code: string): Observable<RecoveryConfirmResult> {
    return this.http.post<RecoveryConfirmResult>(`${this.apiUrl}/recovery/confirm`, { contact, code });
  }

  resetPassword(resetToken: string, password: string): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/reset-password`, { resetToken, password });
  }

  readonly displayName = () => this.user()?.fullName ?? '';
}
