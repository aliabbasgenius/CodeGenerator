import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class Auth {
  private readonly loggedInSubject = new BehaviorSubject<boolean>(false);
  private readonly currentUserSubject = new BehaviorSubject<string>('');

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    // Check if user is already logged in from localStorage (only in browser)
    if (isPlatformBrowser(this.platformId)) {
      const isLoggedIn = localStorage.getItem('isLoggedIn') === 'true';
      const currentUser = localStorage.getItem('currentUser') || '';
      
      if (isLoggedIn && currentUser) {
        this.loggedInSubject.next(true);
        this.currentUserSubject.next(currentUser);
      }
    }
  }

  // Observable for components to subscribe to login status
  get isLoggedIn$(): Observable<boolean> {
    return this.loggedInSubject.asObservable();
  }

  // Observable for current user
  get currentUser$(): Observable<string> {
    return this.currentUserSubject.asObservable();
  }

  // Get current login status
  get isLoggedIn(): boolean {
    return this.loggedInSubject.value;
  }

  // Get current user
  get currentUser(): string {
    return this.currentUserSubject.value;
  }

  // Login method with hardcoded admin credentials
  login(username: string, password: string): boolean {
    if (username === 'admin' && password === 'admin') {
      if (isPlatformBrowser(this.platformId)) {
        localStorage.setItem('isLoggedIn', 'true');
        localStorage.setItem('currentUser', username);
      }
      this.loggedInSubject.next(true);
      this.currentUserSubject.next(username);
      return true;
    }
    return false;
  }

  // Logout method
  logout(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem('isLoggedIn');
      localStorage.removeItem('currentUser');
    }
    this.loggedInSubject.next(false);
    this.currentUserSubject.next('');
  }
}
