import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  username = signal('admin');
  password = signal('');
  loginError = signal<string | null>(null);

  constructor(public auth: AuthService) {}

  login(): void {
    this.loginError.set(null);
    this.auth.login(this.username(), this.password()).subscribe({
      next: () => this.password.set(''),
      error: (err) => this.loginError.set(err.error?.message ?? 'Login failed')
    });
  }

  logout(): void {
    this.auth.logout();
  }
}
