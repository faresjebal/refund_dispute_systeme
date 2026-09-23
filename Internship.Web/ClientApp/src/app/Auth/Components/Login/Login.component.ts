import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../Services/Auth.service';
import { LoginRequest } from '../../Models/Login_request';
import { RouterModule } from '@angular/router';
// Angular Material Modules
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    // Material modules
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
     RouterModule
  ],
  templateUrl: './Login.component.html',
  styleUrls: ['./Login.component.scss']
})
export class LoginComponent {
  loginForm: FormGroup;
  errorMessage = '';
  isLoading = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    console.log('LoginComponent constructor called');
    
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });

    console.log('Login form created:', this.loginForm);
  }

  onSubmit(): void {
    console.log('=== LOGIN FORM SUBMISSION STARTED ===');
    console.log('Form submitted at:', new Date().toISOString());
    console.log('Form valid:', this.loginForm.valid);
    console.log('Form values:', this.loginForm.value);
    console.log('Form errors:', this.getFormErrors());

    if (this.loginForm.invalid) {
      console.log('❌ Form is invalid, not submitting');
      console.log('Invalid fields:', this.getInvalidFields());
      return;
    }

    console.log('✅ Form is valid, proceeding with login...');
    this.isLoading = true;
    this.errorMessage = '';

    const loginData: LoginRequest = {
      email: this.loginForm.value.email,
      password: this.loginForm.value.password
    };

    console.log('📤 Sending login request with data:', {
      email: loginData.email,
      password: '***HIDDEN***' // Don't log actual password
    });

    this.authService.login(loginData).subscribe({
      next: (response) => {
        console.log('✅ Login successful!');
        console.log('📥 Response received:', response);
        console.log('🔄 Navigating to home page...');
        this.router.navigate(['/']);
      },
      error: (err) => {
        console.error('❌ Login failed!');
        console.error('Error details:', err);
        console.error('Error status:', err.status);
        console.error('Error message:', err.error?.message);
        console.error('Full error object:', JSON.stringify(err, null, 2));
        
        this.errorMessage = err.error?.message || 'Login failed';
        this.isLoading = false;
        
        console.log('Error message set to:', this.errorMessage);
        console.log('Loading state set to:', this.isLoading);
      },
      complete: () => {
        console.log('🏁 Login request completed');
        this.isLoading = false;
        console.log('Final loading state:', this.isLoading);
      }
    });
  }

  // Helper method to get form validation errors
  private getFormErrors(): any {
    const errors: any = {};
    Object.keys(this.loginForm.controls).forEach(key => {
      const control = this.loginForm.get(key);
      if (control && control.errors) {
        errors[key] = control.errors;
      }
    });
    return errors;
  }

  // Helper method to get invalid field names
  private getInvalidFields(): string[] {
    const invalidFields: string[] = [];
    Object.keys(this.loginForm.controls).forEach(key => {
      const control = this.loginForm.get(key);
      if (control && control.invalid) {
        invalidFields.push(key);
      }
    });
    return invalidFields;
  }

  // Debug method you can call from browser console
  debugForm(): void {
    console.log('=== FORM DEBUG INFO ===');
    console.log('Form valid:', this.loginForm.valid);
    console.log('Form touched:', this.loginForm.touched);
    console.log('Form dirty:', this.loginForm.dirty);
    console.log('Form values:', this.loginForm.value);
    console.log('Form errors:', this.getFormErrors());
    console.log('Email field:', this.loginForm.get('email'));
    console.log('Password field:', this.loginForm.get('password'));
    console.log('Current error message:', this.errorMessage);
    console.log('Current loading state:', this.isLoading);
  }
}