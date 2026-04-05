import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { ProblemDetails } from '../../core/models/problem-details.model';
import { AuthApiService } from '../../core/services/auth-api.service';
import { AuthSessionService } from '../../core/services/auth-session.service';

@Component({
  selector: 'app-login-page',
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.css',
  standalone: false
})
export class LoginPageComponent {
  private readonly authApi = inject(AuthApiService);
  private readonly authSession = inject(AuthSessionService);
  private readonly formBuilder = inject(FormBuilder);

  protected isSubmitting = false;
  protected errorMessage = '';
  protected successMessage = '';
  protected hasSubmitted = false;

  protected readonly loginForm = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]]
  });

  protected async submitLoginAsync(): Promise<void> {
    this.hasSubmitted = true;
    this.errorMessage = '';
    this.successMessage = '';

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;

    try {
      const response = await firstValueFrom(
        this.authApi.login({
          email: this.loginForm.controls.email.value.trim(),
          password: this.loginForm.controls.password.value
        })
      );

      this.authSession.persistSession(response);
      this.successMessage = 'Signed in successfully. Your session is now stored in the browser.';
      this.loginForm.controls.password.reset('');
    } catch (error) {
      this.errorMessage = this.toProblemMessage(error, 'Unable to sign in right now.');
    } finally {
      this.isSubmitting = false;
    }
  }

  protected fieldError(controlName: 'email' | 'password'): string {
    const control = this.loginForm.controls[controlName];
    if (!(control.invalid && (control.touched || this.hasSubmitted))) {
      return '';
    }

    if (control.hasError('required')) {
      return 'This field is required.';
    }

    if (control.hasError('email')) {
      return 'Enter a valid email address.';
    }

    if (control.hasError('minlength')) {
      return 'Password must be at least 8 characters.';
    }

    return 'Please review this field.';
  }

  private toProblemMessage(error: unknown, fallbackMessage: string): string {
    if (error instanceof HttpErrorResponse) {
      const problem = error.error as ProblemDetails | null;

      if (problem?.errors) {
        const messages = Object.values(problem.errors).flat();
        if (messages.length > 0) {
          return messages.join(' ');
        }
      }

      if (problem?.detail) {
        return problem.detail;
      }
    }

    return fallbackMessage;
  }
}
