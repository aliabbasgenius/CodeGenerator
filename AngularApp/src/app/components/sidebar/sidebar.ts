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
    { title: 'Products', icon: '📦', route: '/products' },
    { title: 'Settings', icon: '⚙️', route: '/settings' }
  ];

  constructor() {}

  setActiveItem(clickedItem: MenuItem): void {
    for (const item of this.menuItems) {
      item.isActive = item === clickedItem;
    }
  }
}
