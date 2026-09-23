import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../Services/Auth.service';
import { RegisterRequest } from '../../Models/Register_request';

// Angular Material Modules
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    // Material modules
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatCheckboxModule
  ],
  templateUrl: './Register.component.html',
  styleUrls: ['./Register.component.scss']
})
export class RegisterComponent {
  registerForm: FormGroup;
  errorMessage = '';
  isLoading = false;
  hidePassword = true;
  hideConfirmPassword = true;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    console.log('RegisterComponent constructor called');
    
    this.registerForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]],
      agreeTerms: [false, [Validators.requiredTrue]]
    }, { validators: this.passwordMatchValidator });

    console.log('Register form created:', this.registerForm);
  }

  // Custom validator to check if passwords match
  passwordMatchValidator(control: AbstractControl): { [key: string]: any } | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');
    
    if (!password || !confirmPassword) {
      return null;
    }

    return password.value === confirmPassword.value ? null : { passwordMismatch: true };
  }

  onSubmit(): void {
    console.log('=== REGISTER FORM SUBMISSION STARTED ===');
    console.log('Form submitted at:', new Date().toISOString());
    console.log('Form valid:', this.registerForm.valid);
    console.log('Form values:', {
      ...this.registerForm.value,
      password: '***HIDDEN***',
      confirmPassword: '***HIDDEN***'
    });
    console.log('Form errors:', this.getFormErrors());

    if (this.registerForm.invalid) {
      console.log('❌ Form is invalid, not submitting');
      console.log('Invalid fields:', this.getInvalidFields());
      this.markFormGroupTouched();
      return;
    }

    console.log('✅ Form is valid, proceeding with registration...');
    this.isLoading = true;
    this.errorMessage = '';

    const registerData: RegisterRequest = {
      firstName: this.registerForm.value.firstName,
      lastName: this.registerForm.value.lastName,
      email: this.registerForm.value.email,
      password: this.registerForm.value.password,
       confirmPassword: this.registerForm.value.password
    };

    console.log('📤 Sending register request with data:', {
      ...registerData,
      password: '***HIDDEN***'
    });

    this.authService.register(registerData).subscribe({
      next: (response) => {
        console.log('✅ Registration successful!');
        console.log('📥 Response received:', response);
        console.log('🔄 Navigating to login page...');
        this.router.navigate(['/login']);
      },
      error: (err) => {
        console.error('❌ Registration failed!');
        console.error('Error details:', err);
        console.error('Error status:', err.status);
        console.error('Error message:', err.error?.message);
        console.error('Full error object:', JSON.stringify(err, null, 2));
        
        this.errorMessage = err.error?.message || 'Registration failed';
        this.isLoading = false;
        
        console.log('Error message set to:', this.errorMessage);
        console.log('Loading state set to:', this.isLoading);
      },
      complete: () => {
        console.log('🏁 Registration request completed');
        this.isLoading = false;
        console.log('Final loading state:', this.isLoading);
      }
    });
  }

  // Helper method to mark all fields as touched for validation display
  private markFormGroupTouched(): void {
    Object.keys(this.registerForm.controls).forEach(key => {
      const control = this.registerForm.get(key);
      control?.markAsTouched();
    });
  }

  // Helper method to get form validation errors
  private getFormErrors(): any {
    const errors: any = {};
    Object.keys(this.registerForm.controls).forEach(key => {
      const control = this.registerForm.get(key);
      if (control && control.errors) {
        errors[key] = control.errors;
      }
    });
    
    // Add form-level errors
    if (this.registerForm.errors) {
      errors['form'] = this.registerForm.errors;
    }
    
    return errors;
  }

  // Helper method to get invalid field names
  private getInvalidFields(): string[] {
    const invalidFields: string[] = [];
    Object.keys(this.registerForm.controls).forEach(key => {
      const control = this.registerForm.get(key);
      if (control && control.invalid) {
        invalidFields.push(key);
      }
    });
    return invalidFields;
  }

  // Helper methods for template
  hasError(fieldName: string, errorType: string): boolean {
    const field = this.registerForm.get(fieldName);
    return !!(field && field.errors?.[errorType] && (field.dirty || field.touched));
  }

  hasPasswordMismatch(): boolean {
    return !!(this.registerForm.errors?.['passwordMismatch'] && 
             this.registerForm.get('confirmPassword')?.touched);
  }

  // Debug method you can call from browser console
  debugForm(): void {
    console.log('=== FORM DEBUG INFO ===');
    console.log('Form valid:', this.registerForm.valid);
    console.log('Form touched:', this.registerForm.touched);
    console.log('Form dirty:', this.registerForm.dirty);
    console.log('Form values:', {
      ...this.registerForm.value,
      password: '***HIDDEN***',
      confirmPassword: '***HIDDEN***'
    });
    console.log('Form errors:', this.getFormErrors());
    console.log('Password mismatch:', this.hasPasswordMismatch());
    console.log('Current error message:', this.errorMessage);
    console.log('Current loading state:', this.isLoading);
  }
}