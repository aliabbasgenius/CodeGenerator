import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Header } from '../header/header';
import { Sidebar } from '../sidebar/sidebar';
import { Footer } from '../footer/footer';

@Component({
  selector: 'app-code-generator-test',
  standalone: true,
  imports: [CommonModule, Header, Sidebar, Footer],
  template: `
    <div class="page-layout">
      <app-header></app-header>
      
      <div class="content-wrapper">
        <app-sidebar></app-sidebar>
        
        <main class="main-content">
          <div class="test-container">
            <h1>Code Generator Test Page</h1>
            <p>This is a test to verify the component loads correctly.</p>
            <div class="test-status">
              <h2>Test Status</h2>
              <p>✅ Component loaded successfully!</p>
              <p>✅ Layout components working!</p>
              <p>✅ Routing is functional!</p>
            </div>
          </div>
        </main>
      </div>
      
      <app-footer></app-footer>
    </div>
  `,
  styles: [`
    .page-layout {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }

    .content-wrapper {
      display: flex;
      flex: 1;
    }

    .main-content {
      flex: 1;
      padding: 2rem;
      background-color: #f8f9fa;
      margin-left: 250px;
    }

    @media (max-width: 768px) {
      .main-content {
        margin-left: 0;
        padding: 1rem;
      }
    }

    .test-container {
      max-width: 800px;
      margin: 0 auto;
      background: white;
      padding: 2rem;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .test-status {
      margin-top: 2rem;
      padding: 1rem;
      background: #d4edda;
      border: 1px solid #c3e6cb;
      border-radius: 4px;
    }

    h1 {
      color: #2c3e50;
      margin-bottom: 1rem;
    }

    h2 {
      color: #495057;
      margin-bottom: 1rem;
    }

    p {
      margin-bottom: 0.5rem;
    }
  `]
})
export class CodeGeneratorTest {
  constructor() {}
}