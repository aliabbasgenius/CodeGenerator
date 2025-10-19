import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DatabaseTable {
  tableName: string;
  schema: string;
  columns: DatabaseColumn[];
  isSelected: boolean;
}

export interface DatabaseColumn {
  columnName: string;
  dataType: string;
  isNullable: boolean;
  isPrimaryKey: boolean;
  isIdentity: boolean;
  maxLength?: number;
  isForeignKey: boolean;
  referencedTable?: string;
  referencedColumn?: string;
  cSharpType: string;
  typeScriptType: string;
}

export interface DatabaseCodeGenerationRequest {
  selectedTables: string[];
  outputPath: string;
  generateAngularCode: boolean;
  generateApiCode: boolean;
  angularPath: string;
}

export interface DatabaseCodeGenerationResult {
  success: boolean;
  message: string;
  generatedFiles: GeneratedFile[];
  errors: string[];
}

export interface GeneratedFile {
  fileName: string;
  filePath: string;
  fileType: string;
  content: string;
}

@Injectable({
  providedIn: 'root'
})
export class DatabaseService {
  private apiUrl = 'http://localhost:5173/api/database';

  constructor(private http: HttpClient) {}

  testConnection(): Observable<{ success: boolean; message: string }> {
    return this.http.get<{ success: boolean; message: string }>(`${this.apiUrl}/test-connection`);
  }

  getTables(): Observable<DatabaseTable[]> {
    return this.http.get<DatabaseTable[]>(`${this.apiUrl}/tables`);
  }

  getTableSchema(tableName: string, schema: string = 'dbo'): Observable<DatabaseTable> {
    return this.http.get<DatabaseTable>(`${this.apiUrl}/tables/${tableName}/schema?schema=${schema}`);
  }

  generateCode(request: DatabaseCodeGenerationRequest): Observable<DatabaseCodeGenerationResult> {
    return this.http.post<DatabaseCodeGenerationResult>(`${this.apiUrl}/generate-code`, request);
  }
}