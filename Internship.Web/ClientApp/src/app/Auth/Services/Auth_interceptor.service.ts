import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpErrorResponse,
  HttpInterceptor
} from '@angular/common/http';
import { Observable, catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../Services/Auth.service';
import { TokenStorageService } from '../Services/Token_storage.service';
import { Router } from '@angular/router';
import { TokenModel } from '../Models/Token_model';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private readonly publicEndpoints = [
    'auth/login',
    'auth/register',
    'auth/refresh'
  ];

  constructor(
    private tokenService: TokenStorageService,
    private authService: AuthService,
    private router: Router
  ) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // Skip interception for public endpoints
    if (this.isPublicRequest(request)) {
      return next.handle(request);
    }

    const accessToken = this.tokenService.getAccessToken();
    
    if (accessToken) {
      request = this.addTokenHeader(request, accessToken);
    }

    return next.handle(request).pipe(
      catchError((error) => {
        if (
          error instanceof HttpErrorResponse &&
          error.status === 401 &&
          !request.url.includes('auth/refresh')
        ) {
          return this.handle401Error(request, next);
        }
        return throwError(() => error);
      })
    );
  }

  private isPublicRequest(request: HttpRequest<any>): boolean {
    return this.publicEndpoints.some(endpoint => 
      request.url.includes(endpoint)
    );
  }

  private addTokenHeader(request: HttpRequest<any>, token: string): HttpRequest<any> {
    return request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  private handle401Error(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (!this.isRefreshing) {
      this.isRefreshing = true;
      const refreshToken = this.tokenService.getRefreshToken();

      if (refreshToken) {
        return this.authService.refreshToken({
          accessToken: this.tokenService.getAccessToken()!,
          refreshToken: refreshToken
        }).pipe(
          switchMap((tokens: TokenModel) => {
            this.isRefreshing = false;
            this.tokenService.saveTokens(tokens);
            return next.handle(this.addTokenHeader(request, tokens.accessToken));
          }),
          catchError((err) => {
            this.isRefreshing = false;
            this.handleLogout();
            return throwError(() => err);
          })
        );
      }
    }

    this.handleLogout();
    return throwError(() => new Error('Token refresh failed'));
  }

  private handleLogout(): void {
    this.tokenService.clear();
    // Only redirect if not already on a public page
    if (!this.isPublicRoute(this.router.url)) {
      this.router.navigate(['/login']);
    }
  }

  private isPublicRoute(url: string): boolean {
    const publicRoutes = ['/login', '/register'];
    return publicRoutes.some(route => url.startsWith(route));
  }
}