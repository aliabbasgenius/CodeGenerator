import { Component, OnInit } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Auth } from './services/auth';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  title = 'CodeGenerator App';

  constructor(
    private authService: Auth,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Check authentication status on app initialization
    if (!this.authService.isLoggedIn) {
      this.router.navigate(['/login']);
    }
  }
}
