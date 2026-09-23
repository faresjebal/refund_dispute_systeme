import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { LoginRequest} from '../Models/Login_request';
import {RegisterRequest} from '../Models/Register_request';
import { ChangePasswordRequest} from '../Models/Change_password_request';
import{ RefreshTokenRequest } from '../Models/Refresh_token_request';
import{ TokenModel } from '../Models/Token_model';
import { TokenStorageService } from '../Services/Token_storage.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = 'api/auth';

  constructor(
    private http: HttpClient,
    private tokenStorage: TokenStorageService
  ) {}

  login(request: LoginRequest): Observable<TokenModel> {
    return this.http.post<TokenModel>(`${this.apiUrl}/login`, request).pipe(
      tap((tokens) => this.tokenStorage.saveTokens(tokens))
    );
  }

  register(request: RegisterRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/register`, request);
  }

  refreshToken(request: RefreshTokenRequest): Observable<TokenModel> {
    return this.http.post<TokenModel>(`${this.apiUrl}/refresh`, request).pipe(
      tap((tokens) => this.tokenStorage.saveTokens(tokens))
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/logout`, {}).pipe(
      tap(() => this.tokenStorage.clear())
    );
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/change-password`, request);
  }

  getCurrentUser(): Observable<any> {
    return this.http.get(`${this.apiUrl}/me`);
  }
}