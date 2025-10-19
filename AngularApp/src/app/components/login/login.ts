import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Auth } from '../../services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  username: string = '';
  password: string = '';
  errorMessage: string = '';
  isLoading: boolean = false;

  constructor(
    private authService: Auth,
    private router: Router
  ) {}

  onSubmit(): void {
    if (!this.username || !this.password) {
      this.errorMessage = 'Please enter both username and password';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    // Simulate a small delay for better UX
    setTimeout(() => {
      if (this.authService.login(this.username, this.password)) {
        this.router.navigate(['/dashboard']);
      } else {
        this.errorMessage = 'Invalid username or password. Try admin/admin';
      }
      this.isLoading = false;
    }, 500);
  }

  clearError(): void {
    this.errorMessage = '';
  }
}
