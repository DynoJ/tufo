import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private base = '/api/auth';

  constructor(private http: HttpClient) {}

  login(dto: { email: string; password: string }) {
    return this.http.post<{ token: string }>(`${this.base}/login`, dto).pipe(
      tap(res => localStorage.setItem('tufo.jwt', res.token))
    );
  }

  register(dto: { username: string; email: string; password: string; confirmPassword: string }) {
    return this.http.post<{ token: string }>(`${this.base}/register`, dto).pipe(
      tap(res => localStorage.setItem('tufo.jwt', res.token))
    );
  }

  logout() {
    localStorage.removeItem('tufo.jwt');
  }

  getToken() {
    return localStorage.getItem('tufo.jwt');
  }
}