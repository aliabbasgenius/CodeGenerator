import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Header } from '../header/header';
import { Sidebar } from '../sidebar/sidebar';
import { Footer } from '../footer/footer';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, Header, Sidebar, Footer],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard {
  stats = [
    { title: 'Projects Generated', value: '42', icon: '📁' },
    { title: 'Templates Available', value: '15', icon: '📄' },
    { title: 'Active Users', value: '8', icon: '👥' },
    { title: 'Success Rate', value: '98%', icon: '✅' }
  ];

  recentActivities = [
    { action: 'Generated C# Web API project', time: '2 minutes ago' },
    { action: 'Created new template: React Component', time: '15 minutes ago' },
    { action: 'Updated project settings', time: '1 hour ago' },
    { action: 'Generated Python Flask app', time: '2 hours ago' }
  ];

  get currentDate(): string {
    return new Date().toLocaleDateString();
  }
}
