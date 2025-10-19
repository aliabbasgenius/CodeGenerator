import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

interface MenuItem {
  title: string;
  icon: string;
  route: string;
  isActive?: boolean;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class Sidebar {
  menuItems: MenuItem[] = [
    { title: 'Dashboard', icon: '📊', route: '/dashboard' },
    { title: 'Code Generator', icon: '⚙️', route: '/generator' },
    { title: 'Templates', icon: '📄', route: '/templates' },
    { title: 'Projects', icon: '📁', route: '/projects' },
    { title: 'Settings', icon: '⚙️', route: '/settings' },
    { title: 'Help & Support', icon: '❓', route: '/help' }
  ];

  constructor() {}

  setActiveItem(clickedItem: MenuItem): void {
    this.menuItems.forEach(item => {
      item.isActive = item === clickedItem;
    });
  }
}
