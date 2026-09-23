import { Injectable } from '@angular/core';
import {
  CanActivate,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  Router,
  UrlTree
} from '@angular/router';
import { TokenStorageService } from '../Services/Token_storage.service';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private tokenService: TokenStorageService,
    private router: Router
  ) {}

  canActivate(
    next: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): boolean | UrlTree | Observable<boolean | UrlTree> | Promise<boolean | UrlTree> {
    
    // Skip guard for public routes
    if (this.isPublicRoute(next)) {
      return true;
    }

    if (this.tokenService.isLoggedIn()) {
      return true;
    }

    // Redirect to login with return URL
    return this.router.createUrlTree(['/login'], {
      queryParams: { returnUrl: state.url }
    });
  }

  private isPublicRoute(route: ActivatedRouteSnapshot): boolean {
    const publicRoutes = ['/login', '/register'];
    return publicRoutes.includes(route.url.join('/'));
  }
}