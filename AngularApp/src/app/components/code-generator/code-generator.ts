import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { DatabaseService, DatabaseTable, DatabaseCodeGenerationRequest, GeneratedFile } from '../../services/database.service';
import { Header } from '../header/header';
import { Sidebar } from '../sidebar/sidebar';
import { Footer } from '../footer/footer';

@Component({
  selector: 'app-code-generator',
  standalone: true,
  imports: [CommonModule, FormsModule, Header, Sidebar, Footer],
  templateUrl: './code-generator.html',
  styleUrl: './code-generator.css'
})
export class CodeGenerator implements OnInit, OnDestroy {
  tables: DatabaseTable[] = [];
  isLoading = false;
  connectionStatus: string = '';
  isConnected = false;
  selectedTables: string[] = [];
  isGenerating = false;
  generationResult: string = '';
  
  // Generation options
  generateAngularCode = true;
  generateApiCode = false;
  outputPath = './generated';
  angularPath = '../AngularApp/src/app';

  private subscription: Subscription = new Subscription();

  constructor(private databaseService: DatabaseService) {}

  ngOnInit(): void {
    this.testConnection();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  testConnection(): void {
    this.isLoading = true;
    this.connectionStatus = 'Testing connection...';
    
    this.subscription.add(
      this.databaseService.testConnection().subscribe({
        next: (result) => {
          this.isConnected = result.success;
          this.connectionStatus = result.message;
          this.isLoading = false;
          
          if (result.success) {
            this.loadTables();
          }
        },
        error: (error) => {
          console.error('Connection test failed:', error);
          this.isConnected = false;
          this.connectionStatus = 'Connection failed: ' + (error.error?.message || error.message);
          this.isLoading = false;
        }
      })
    );
  }

  loadTables(): void {
    this.isLoading = true;
    
    this.subscription.add(
      this.databaseService.getTables().subscribe({
        next: (tables) => {
          this.tables = tables.map(table => ({ ...table, isSelected: false }));
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Failed to load tables:', error);
          this.connectionStatus = 'Failed to load tables: ' + (error.error?.message || error.message);
          this.isLoading = false;
        }
      })
    );
  }

  onTableSelectionChange(table: DatabaseTable): void {
    const tableFullName = `${table.schema}.${table.tableName}`;
    
    if (table.isSelected) {
      if (!this.selectedTables.includes(tableFullName)) {
        this.selectedTables.push(tableFullName);
      }
    } else {
      const index = this.selectedTables.indexOf(tableFullName);
      if (index > -1) {
        this.selectedTables.splice(index, 1);
      }
    }
  }

  selectAll(): void {
    this.tables.forEach(table => {
      table.isSelected = true;
      const tableFullName = `${table.schema}.${table.tableName}`;
      if (!this.selectedTables.includes(tableFullName)) {
        this.selectedTables.push(tableFullName);
      }
    });
  }

  deselectAll(): void {
    this.tables.forEach(table => {
      table.isSelected = false;
    });
    this.selectedTables = [];
  }

  generateCode(): void {
    if (this.selectedTables.length === 0) {
      alert('Please select at least one table to generate code.');
      return;
    }

    this.isGenerating = true;
    this.generationResult = 'Starting code generation...';

    const request: DatabaseCodeGenerationRequest = {
      selectedTables: this.selectedTables,
      outputPath: this.outputPath,
      generateAngularCode: this.generateAngularCode,
      generateApiCode: this.generateApiCode,
      angularPath: this.angularPath
    };

    this.subscription.add(
      this.databaseService.generateCode(request).subscribe({
        next: (result) => {
          this.isGenerating = false;
          if (result.success) {
            this.generationResult = `${result.message}\n\nGenerated Files:\n${result.generatedFiles.map(f => `• ${f.fileName} (${f.fileType})`).join('\n')}`;
            
            // Show download options
            this.showGeneratedFiles(result.generatedFiles);
          } else {
            this.generationResult = `Code generation completed with errors:\n${result.errors.join('\n')}`;
          }
        },
        error: (error) => {
          console.error('Code generation failed:', error);
          this.isGenerating = false;
          this.generationResult = 'Code generation failed: ' + (error.error?.message || error.message);
        }
      })
    );
  }

  showGeneratedFiles(files: GeneratedFile[]): void {
    // Create a simple file preview/download interface
    const fileList = files.map(file => ({
      name: file.fileName,
      path: file.filePath,
      type: file.fileType,
      content: file.content
    }));

    // Store files for potential download
    (window as any).generatedFiles = fileList;
    
    // You could implement a modal here to show the files
    const message = `Generated ${files.length} files successfully!\n\nFiles created:\n${files.map(f => `• ${f.fileName}`).join('\n')}\n\nFiles are ready for download or integration into your project.`;
    alert(message);
  }

  getSelectedCount(): number {
    return this.selectedTables.length;
  }

  getTableColumns(table: DatabaseTable): string {
    return table.columns.map(col => col.columnName).join(', ');
  }

  getPrimaryKeyColumns(table: DatabaseTable): string {
    return table.columns.filter(col => col.isPrimaryKey).map(col => col.columnName).join(', ') || 'None';
  }
}