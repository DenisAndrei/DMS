import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, throwError } from 'rxjs';

import { AuthSessionService } from '../services/auth-session.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private readonly authSession = inject(AuthSessionService);
  private readonly router = inject(Router);

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.authSession.token();

    const authenticatedRequest = token
      ? request.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`
          }
        })
      : request;

    return next.handle(authenticatedRequest).pipe(
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 401) {
          this.authSession.logout();
          void this.router.navigateByUrl('/login');
        }

        return throwError(() => error);
      })
    );
  }
}
