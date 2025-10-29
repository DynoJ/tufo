import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h2>Register</h2>
    <form (ngSubmit)="register()">
      <input [(ngModel)]="username" name="username" placeholder="Username" required />
      <input [(ngModel)]="email" name="email" placeholder="Email" required />
      <input [(ngModel)]="password" name="password" placeholder="Password" type="password" required />
      <input [(ngModel)]="confirmPassword" name="confirmPassword" placeholder="Confirm Password" type="password" required />
      <button type="submit">Create Account</button>

      <p *ngIf="error" style="color: red;">{{ error }}</p>
    </form>
  `
})
export class RegisterComponent {
  username = '';
  email = '';
  password = '';
  confirmPassword = '';
  error: string | null = null;

  constructor(private auth: AuthService, private router: Router) {}

  register() {
    if (this.password !== this.confirmPassword) {
      this.error = 'Passwords do not match.';
      return;
    }

    this.auth.register({
      username: this.username,
      email: this.email,
      password: this.password,
      confirmPassword: this.confirmPassword
    }).subscribe({
      next: () => this.router.navigate(['/climbs']),
      error: (err) => this.error = err.error || 'Registration failed.'
    });
  }
}